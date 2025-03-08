using System.Collections.Generic;
using System.Linq;
using HaruhiChokuretsuLib.Archive.Event;
using LiteDB;
using SerialLoops.Lib.Items.Shims;

namespace SerialLoops.Lib.Items;

public partial class ItemDescription
{
    [BsonId]
    public string Name { get; set; }
    public bool CanRename { get; set; }
    public string DisplayName { get; set; }
    public ItemType Type { get; set; }

    public ItemDescription()
    {
    }

    public ItemDescription(string name, ItemType type, string displayName)
    {
        Name = name;
        Type = type;
        CanRename = true;
        if (!string.IsNullOrEmpty(displayName))
        {
            DisplayName = displayName;
        }
        else
        {
            DisplayName = Name;
        }
    }

    public virtual void InitializeAfterDbLoad(Project project)
    {
    }

    public override bool Equals(object obj)
    {
        return Name.Equals(((ItemDescription)obj)?.Name);
    }

    public override int GetHashCode()
    {
        return Name!.GetHashCode();
    }

    // Enum with values for each type of item
    public enum ItemType
    {
        Background,
        BGM,
        Character,
        Character_Sprite,
        Chess_Puzzle,
        Chibi,
        Group_Selection,
        Item,
        Layout,
        Map,
        Place,
        Puzzle,
        Scenario,
        Script,
        SFX,
        System_Texture,
        Topic,
        Transition,
        Voice,
        Save, // Not a real item so I'm not adhering to alpha order here; should never show up in a project
    }

    public List<ItemShim> GetReferencesTo(Project project, LiteDatabase db)
    {
        var itemsCol = db.GetCollection<ItemDescription>(Project.ItemsCollectionName);
        var shimsCol = db.GetCollection<ItemShim>(Project.ShimsCollectionName);
        var scriptsCol = db.GetCollection<ScriptItemShim>(nameof(ScriptItem));
        var puzzlesCol = db.GetCollection<PuzzleItemShim>(nameof(PuzzleItem));

        List<ItemShim> references = [];
        ScenarioItem scenario = (ScenarioItem)itemsCol.FindById("Scenario");
        switch (Type)
        {
            case ItemType.Background:
                BackgroundItem bg = (BackgroundItem)this;
                string[] bgCommands =
                [
                    EventFile.CommandVerb.KBG_DISP.ToString(),
                    EventFile.CommandVerb.BG_DISP.ToString(),
                    EventFile.CommandVerb.BG_DISP2.ToString(),
                    EventFile.CommandVerb.BG_DISPCG.ToString(),
                    EventFile.CommandVerb.BG_FADE.ToString(),
                ];
                (string ScriptName, ScriptCommandInvocation Command)[] bgScriptUses = project.Evt.Files.AsParallel().SelectMany(e =>
                        e.ScriptSections.SelectMany(sec =>
                            sec.Objects.Where(c => bgCommands.Contains(c.Command.Mnemonic)).Select(c => (e.Name[..^1], c))))
                    .Where(t => t.c.Parameters[0] == bg.Id || t.c.Command.Mnemonic == EventFile.CommandVerb.BG_FADE.ToString() && t.c.Parameters[1] == bg.Id).ToArray();
                foreach ((string scriptName, ScriptCommandInvocation _) in bgScriptUses)
                {
                    references.Add(scriptsCol.FindById(scriptName));
                }
                return references.Where(r => r is not null).DistinctBy(r => r.Name).ToList();

            case ItemType.BGM:
                BackgroundMusicItem bgm = (BackgroundMusicItem)this;
                (string ScriptName, ScriptCommandInvocation comamnd)[] bgmScriptUses = project.Evt.Files.AsParallel().SelectMany(e =>
                        e.ScriptSections.SelectMany(sec =>
                            sec.Objects.Where(c => c.Command.Mnemonic == EventFile.CommandVerb.BGM_PLAY.ToString()).Select(c => (e.Name[..^1], c))))
                    .Where(t => t.c.Parameters[0] == bgm.Index).ToArray();
                foreach ((string scriptName, ScriptCommandInvocation _) in bgmScriptUses)
                {
                    references.Add(scriptsCol.FindById(scriptName));
                }
                return references.Where(r => r is not null).DistinctBy(r => r.Name).ToList();

            case ItemType.Character:
                CharacterItem character = (CharacterItem)this;
                references.AddRange(scriptsCol.Find(s =>  s.Speakers.Select(sp => (int)sp).Any(sp => sp == (int)character.MessageInfo.Character)));
                return references.Where(r => r is not null).DistinctBy(r => r.Name).ToList();

            case ItemType.Character_Sprite:
                CharacterSpriteItem sprite = (CharacterSpriteItem)this;
                (string ScriptName, ScriptCommandInvocation command)[] spriteScriptUses = project.Evt.Files.AsParallel().SelectMany(e =>
                        e.ScriptSections.SelectMany(sec =>
                            sec.Objects.Where(c => c.Command.Mnemonic == EventFile.CommandVerb.DIALOGUE.ToString()).Select(c => (e.Name[..^1], c))))
                    .Where(t => t.c.Parameters[1] == sprite.Index).ToArray();

                foreach ((string scriptName, ScriptCommandInvocation _) in spriteScriptUses)
                {
                    references.Add(scriptsCol.FindById(scriptName));
                }
                return references.Where(r => r is not null).DistinctBy(r => r.Name).ToList();

            case ItemType.Chess_Puzzle:
                ChessPuzzleItem chessPuzzle = (ChessPuzzleItem)this;
                (string ScriptName, ScriptCommandInvocation command)[] chessPuzzleScriptUses = project.Evt.Files.AsParallel().SelectMany(e =>
                    e.ScriptSections.SelectMany((sec =>
                            sec.Objects.Where(c => c.Command.Mnemonic == EventFile.CommandVerb.CHESS_LOAD.ToString()).Select(c => (e.Name[..^1], c)))))
                    .Where(t => t.c.Parameters[0] == chessPuzzle.ChessPuzzle.Index).ToArray();
                foreach ((string scriptName, ScriptCommandInvocation _) in chessPuzzleScriptUses)
                {
                    references.Add(scriptsCol.FindById(scriptName));
                }
                return references.Where(r => r is not null).DistinctBy(r => r.Name).ToList();

            case ItemType.Chibi:
                ChibiItem chibi = (ChibiItem)this;
                IEnumerable<int> evtIndices = project.Evt.Files.Where(e =>
                    (e.MapCharactersSection?.Objects?.Any(t => t.CharacterIndex == chibi.ChibiIndex) ?? false)
                        || (e.StartingChibisSection?.Objects?.Any(t => t.ChibiIndex == chibi.TopScreenIndex) ?? false))
                    .Select(e => e.Index);
                references.AddRange(scriptsCol.Find(s => evtIndices.Contains(s.EventIndex)));
                (string ScriptName, ScriptCommandInvocation command)[] chibiScriptUses = project.Evt.Files.AsParallel().SelectMany(e =>
                        e.ScriptSections.SelectMany(sec =>
                            sec.Objects.Where(c => c.Command.Mnemonic == EventFile.CommandVerb.CHIBI_ENTEREXIT.ToString()).Select(c => (e.Name[..^1], c))))
                    .Where(t => t.c.Parameters[0] == chibi.TopScreenIndex).ToArray();
                foreach ((string scriptName, ScriptCommandInvocation _) in chibiScriptUses)
                {
                    references.Add(scriptsCol.FindById(scriptName));
                }
                return references.Where(r => r is not null).DistinctBy(r => r.Name).ToList();

            case ItemType.Group_Selection:
                GroupSelectionItem groupSelection = (GroupSelectionItem)this;
                if (scenario.Scenario.Commands.Any(c => c.Verb == ScenarioCommand.ScenarioVerb.ROUTE_SELECT && c.Parameter == groupSelection.Index))
                {
                    references.Add(shimsCol.FindById(scenario.Name));
                }
                return references;

            case ItemType.Map:
                MapItem map = (MapItem)this;
                (string ScriptName, ScriptCommandInvocation command)[] mapScriptUses = project.Evt.Files.AsParallel().SelectMany(e =>
                        e.ScriptSections.SelectMany(sec =>
                            sec.Objects.Where(c => c.Command.Mnemonic == EventFile.CommandVerb.LOAD_ISOMAP.ToString()).Select(c => (e.Name[..^1], c))))
                    .Where(t => t.c.Parameters[0] == map.Map.Index).ToArray();
                foreach ((string scriptName, ScriptCommandInvocation _) in mapScriptUses)
                {
                    references.Add(scriptsCol.FindById(scriptName));
                }
                references.AddRange(puzzlesCol.Find(p => p.MapId == map.QmapIndex));
                return references.Where(r => r is not null).DistinctBy(r => r.Name).ToList();

            case ItemType.Place:
                PlaceItem place = (PlaceItem)this;
                (string ScriptName, ScriptCommandInvocation command)[] placeScriptUses = project.Evt.Files.AsParallel().SelectMany(e =>
                        e.ScriptSections.SelectMany(sec =>
                            sec.Objects.Where(c => c.Command.Mnemonic == EventFile.CommandVerb.SET_PLACE.ToString()).Select(c => (e.Name[..^1], c))))
                    .Where(t => t.c.Parameters[1] == place.Index).ToArray();
                foreach ((string scriptName, ScriptCommandInvocation _) in placeScriptUses)
                {
                    references.Add(scriptsCol.FindById(scriptName));
                }
                return references.Where(r => r is not null).DistinctBy(r => r.Name).ToList();

            case ItemType.Puzzle:
                PuzzleItem puzzle = (PuzzleItem)this;
                if (scenario.Scenario.Commands.Any(c => c.Verb == ScenarioCommand.ScenarioVerb.PUZZLE_PHASE && c.Parameter == puzzle.Puzzle.Index))
                {
                    references.Add(shimsCol.FindById(scenario.Name));
                }
                return references.Where(r => r is not null).DistinctBy(r => r.Name).ToList();

            case ItemType.Script:
                ScriptItem script = (ScriptItem)this;
                if (scenario.Scenario.Commands.Any(c => c.Verb == ScenarioCommand.ScenarioVerb.LOAD_SCENE && c.Parameter == script.Event.Index))
                {
                    references.Add(shimsCol.FindById(scenario.Name));
                }

                var gsCol = db.GetCollection<GroupSelectionItemShim>(nameof(GroupSelectionItem));
                references.AddRange(gsCol.Find(g => g.ScriptIndices.Contains((short)script.Event.Index)));

                var topicsCol = db.GetCollection<TopicItemShim>(nameof(TopicItem));
                references.AddRange(topicsCol.Find(t => t.TopicEntry.CardType != TopicCardType.Main && t.TopicEntry.EventIndex == script.Event.Index ||
                                                        (t.HiddenMainTopic != null && t.HiddenMainTopic.EventIndex == script.Event.Index)));

                references.AddRange(scriptsCol.Find(s => s.Conditionals.Contains(Name)));
                return references.Where(r => r is not null).DistinctBy(r => r.Name).ToList();

            case ItemType.SFX:
                SfxItem sfx = (SfxItem)this;
                (string ScriptName, ScriptCommandInvocation command)[] sfxScriptUses = project.Evt.Files.AsParallel().SelectMany(e =>
                        e.ScriptSections.SelectMany(sec =>
                            sec.Objects.Where(c => c.Command.Mnemonic == EventFile.CommandVerb.SND_PLAY.ToString()).Select(c => (e.Name[..^1], c))))
                    .Where(t => t.c.Parameters[0] == sfx.Index).ToArray();
                foreach ((string scriptName, ScriptCommandInvocation _) in sfxScriptUses)
                {
                    references.Add(scriptsCol.FindById(scriptName));
                }

                var charCol = db.GetCollection<CharacterItemShim>(nameof(CharacterItem));
                references.AddRange(charCol.Find(c => c.VoiceFont == sfx.Index));
                return references.Where(r => r is not null).DistinctBy(r => r.Name).ToList();

            case ItemType.Topic:
                TopicItem topic = (TopicItem)this;
                (string ScriptName, ScriptCommandInvocation command)[] topicScriptUses = project.Evt.Files.AsParallel().SelectMany(e =>
                        e.ScriptSections.SelectMany(sec =>
                            sec.Objects.Where(c => c.Command.Mnemonic == EventFile.CommandVerb.TOPIC_GET.ToString()).Select(c => (e.Name[..^1], c))))
                    .Where(t => t.c.Parameters[0] == topic.TopicEntry.Id).ToArray();
                foreach ((string scriptName, ScriptCommandInvocation _) in topicScriptUses)
                {
                    references.Add(scriptsCol.FindById(scriptName));
                }
                return references.Where(r => r is not null).DistinctBy(r => r.Name).ToList();

            case ItemType.Voice:
                VoicedLineItem voicedLine = (VoicedLineItem)this;
                (string ScriptName, ScriptCommandInvocation command)[] vceScriptUses = project.Evt.Files.AsParallel().SelectMany(e =>
                        e.ScriptSections.SelectMany(sec =>
                            sec.Objects.Where(c => c.Command.Mnemonic == EventFile.CommandVerb.DIALOGUE.ToString()).Select(c => (e.Name[..^1], c))))
                    .Where(t => t.c.Parameters[5] == voicedLine.Index)
                    .Concat(project.Evt.Files.AsParallel().SelectMany(e =>
                            e.ScriptSections.SelectMany(sec =>
                                sec.Objects.Where(c => c.Command.Mnemonic == EventFile.CommandVerb.VCE_PLAY.ToString()).Select(c => (e.Name[..^1], c))))
                        .Where(t => t.c.Parameters[0] == voicedLine.Index))
                    .ToArray();
                foreach ((string scriptName, ScriptCommandInvocation _) in vceScriptUses)
                {
                    references.Add(scriptsCol.FindById(scriptName));
                }
                return references.Where(r => r is not null).DistinctBy(r => r.Name).ToList();

            default:
                return references;
        }
    }
}
