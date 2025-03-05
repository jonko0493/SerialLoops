using System.Collections.Generic;
using System.IO;
using System.Linq;
using HaruhiChokuretsuLib.Archive.Event;
using LiteDB;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Util;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace SerialLoops.Lib;

public class Flags
{
    public const int NUM_FLAGS = 5120;
    public static readonly Dictionary<int, string> _names = JsonSerializer.Deserialize<Dictionary<int, string>>(File.ReadAllText(Extensions.GetLocalizedFilePath(Path.Combine("Defaults", "DefaultFlags"), "json")));

    public static string GetFlagNickname(int flag, Project project)
    {
        using LiteDatabase db = new(project.DbFile);
        var itemsCol = db.GetCollection<ItemDescription>(Project.ItemsTableName);

        if (_names.TryGetValue(flag, out string value))
        {
            return $"{value} (F{flag:D2})";
        }

        TopicItem topic = (TopicItem)itemsCol.FindOne(i => i.Type == ItemDescription.ItemType.Topic && ((TopicItem)i).TopicEntry.Id == flag);
        if (topic is not null)
        {
            return string.Format(project.Localize("{0} Obtained (F{1:D2})"), topic.DisplayName, flag);
        }

        IEnumerable<TopicItem> topics = itemsCol.Find(i => i.Type == ItemDescription.ItemType.Topic).Cast<TopicItem>();
            TopicItem readTopic = topics.FirstOrDefault(t => (t.TopicEntry.Type == TopicType.Main && t.HiddenMainTopic is not null && t.TopicEntry.Id + 3463 == flag) ||
                                                                (t.TopicEntry.Type != TopicType.Main && t.TopicEntry.Id + 3451 == flag));
        if (readTopic is not null)
        {
            return string.Format(project.Localize("{0} Watched in Extras (F{1:D2})"), readTopic.DisplayName, flag);
        }

        BackgroundMusicItem bgm = (BackgroundMusicItem)itemsCol.FindOne(i => i.Type == ItemDescription.ItemType.BGM && ((BackgroundMusicItem)i).Flag == flag);
        if (bgm is not null)
        {
            return string.Format(project.Localize("Listened to {0} (F{1:D2})"), bgm.DisplayName, flag);
        }

        BackgroundItem bg = (BackgroundItem)itemsCol.FindOne(i => i.Type == ItemDescription.ItemType.Background && ((BackgroundItem)i).Flag == flag);
        if (bg is not null && flag > 0)
        {
            return string.Format(project.Localize("{0} ({1}) Seen (F{2:D2})"), bg.CgName, bg.DisplayName, flag);
        }

        IEnumerable<GroupSelectionItem> groupSelections = itemsCol.Find(i => i.Type == ItemDescription.ItemType.Group_Selection).Cast<GroupSelectionItem>();
        GroupSelectionItem groupSelection = groupSelections.FirstOrDefault(g => g.Selection.Activities.Any(a => a?.Routes.Any(r => r?.Flag == flag) ?? false));
        if (groupSelection is not null)
        {
            ScenarioRoute route = groupSelection.Selection.Activities.First(a => a?.Routes.Any(r => r.Flag == flag) ?? false).Routes.First(r => r.Flag == flag);
            return string.Format(project.Localize("Route \"{0}\" Completed (F{1:D2})"), route.Title.GetSubstitutedString(project), flag);
        }

        ScriptItem script = (ScriptItem)itemsCol.FindOne(i => i.Type == ItemDescription.ItemType.Script && ((ScriptItem)i).StartReadFlag > 0 &&
                                                              flag >= ((ScriptItem)i).StartReadFlag && flag < ((ScriptItem)i).StartReadFlag + ((ScriptItem)i).Event.ScriptSections.Count);
        if (script is not null)
        {
            return string.Format(project.Localize("Script {0} Section {1} Completed (F{2:D2})"), script.DisplayName, script.Event.ScriptSections[flag - script.StartReadFlag].Name, flag);
        }

        Tutorial tutorial = project.TutorialFile.Tutorials.FirstOrDefault(t => t.Id != 0 && t.Id == flag);
        if (tutorial is not null)
        {
            return string.Format(project.Localize("Tutorial {0} Completed (F{1:D2})"),
                ((ScriptItem)itemsCol.FindOne(i =>
                    i.Type == ItemDescription.ItemType.Script &&
                    ((ScriptItem)i).Event.Index == tutorial.AssociatedScript)).DisplayName, flag);
        }

        return $"F{flag:D2}";
    }
}
