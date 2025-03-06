using System.Collections.Generic;
using System.Linq;
using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Util;
using LiteDB;
using SerialLoops.Lib.Items.Shims;

namespace SerialLoops.Lib.Items;

public class ScenarioItem : Item
{
    public ScenarioStruct Scenario { get; set; }
    public List<ScenarioCommandHolder> ScenarioCommands { get; set; } = [];

    private Project _project;

    public ScenarioItem()
    {
    }
    public ScenarioItem(ScenarioStruct scenario, Project project, ILogger log) : base("Scenario", ItemType.Scenario)
    {
        Scenario = scenario;
        CanRename = false;
        _project = project;
    }

    public override void Refresh(Project project, ILogger log)
    {
        ScenarioCommands = Scenario.Commands.Select(c => GetCommandMacro(c)).ToList();
    }

    public void RefreshWithDb(LiteDatabase db)
    {
        ScenarioCommands = Scenario.Commands.Select(c => GetCommandMacro(c, db)).ToList();
    }

    public ScenarioCommandHolder GetCommandMacro(ScenarioCommand command, LiteDatabase db = null)
    {
        bool dbWasNull = db is null;
        db ??= new(_project.DbFile);

        switch (command.Verb)
        {
            case ScenarioCommand.ScenarioVerb.LOAD_SCENE:
                var scriptCol = db.GetCollection<ScriptItemShim>(nameof(ScriptItem));
                ScriptItemShim script = scriptCol.FindOne(s => s.EventIndex == command.Parameter);
                if (dbWasNull)
                {
                    db.Dispose();
                }
                return new(command.Verb, script.DisplayName);

            case ScenarioCommand.ScenarioVerb.PUZZLE_PHASE:
                var puzzleCol = db.GetCollection<PuzzleItemShim>(nameof(PuzzleItem));
                PuzzleItemShim puzzle = puzzleCol.FindOne(p => p.PuzzleIndex == command.Parameter);
                if (dbWasNull)
                {
                    db.Dispose();
                }
                return new(command.Verb, puzzle.DisplayName);

            case ScenarioCommand.ScenarioVerb.ROUTE_SELECT:
                var gsCol = db.GetCollection<GroupSelectionItemShim>(nameof(GroupSelectionItem));
                GroupSelectionItemShim groupSelection = gsCol.FindOne(g => g.Index == command.Parameter);
                if (dbWasNull)
                {
                    db.Dispose();
                }
                return new(command.Verb, groupSelection.DisplayName);

            default:
                if (dbWasNull)
                {
                    db.Dispose();
                }
                return new(command.Verb, command.Parameter.ToString());
        }
    }
}

public class ScenarioCommandHolder
{
    public ScenarioCommand.ScenarioVerb Verb { get; set; }
    public string Parameter { get; set; }

    public ScenarioCommandHolder()
    {
    }

    public ScenarioCommandHolder(ScenarioCommand.ScenarioVerb verb, string parameter)
    {
        Verb = verb;
        Parameter = parameter;
    }
}
