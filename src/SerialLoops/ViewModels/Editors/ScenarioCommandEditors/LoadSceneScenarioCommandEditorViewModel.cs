using System.Collections.ObjectModel;
using System.Linq;
using LiteDB;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Items.Shims;
using SerialLoops.Models;
using SerialLoops.ViewModels.Panels;

namespace SerialLoops.ViewModels.Editors.ScenarioCommandEditors;

public class LoadSceneScenarioCommandEditorViewModel : ScenarioCommandEditorViewModel
{
    private Project _project;

    public ObservableCollection<ScriptItemShim> ScriptShims { get; set; }

    private ScriptItemShim _scriptShim;
    public ScriptItemShim ScriptShim
    {
        get => _scriptShim;
        set
        {
            this.RaiseAndSetIfChanged(ref _scriptShim, value);
            _parameter = _scriptShim.EventIndex;
            SelectedScenarioCommand.Parameter = _scriptShim.DisplayName;
            SelectedScenarioCommand.Scenario.Scenario.Commands[SelectedScenarioCommand.CommandIndex].Parameter = _parameter;
            ScenarioEditor.Description.UnsavedChanges = true;

            using LiteDatabase db = new(_project.DbFile);
            var itemsCol = db.GetCollection<ItemDescription>(Project.ItemsCollectionName);
            Script = (ScriptItem)_scriptShim.GetItem(itemsCol);
        }
    }
    [Reactive]
    public ScriptItem Script { get; set; }

    public LoadSceneScenarioCommandEditorViewModel(ScenarioEditorViewModel scenarioEditor, PrettyScenarioCommand command, Project project, EditorTabsPanelViewModel tabs)
        : base(scenarioEditor, command, tabs)
    {
        _project = project;
        using LiteDatabase db = new(project.DbFile);
        var itemsCol = db.GetCollection<ItemDescription>(Project.ItemsCollectionName);
        var scriptsCol = db.GetCollection<ScriptItemShim>(nameof(ScriptItem));

        ScriptShims = new(scriptsCol.FindAll());
        _scriptShim = ScriptShims.FirstOrDefault(s => s.DisplayName == command.Parameter);
        _parameter = _scriptShim?.EventIndex ?? 0;
        Script = (ScriptItem)_scriptShim?.GetItem(itemsCol);
    }
}
