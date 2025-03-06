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

public class RouteSelectScenarioCommandEditorViewModel : ScenarioCommandEditorViewModel
{
    private Project _project;
    public ObservableCollection<GroupSelectionItemShim> GroupSelections { get; set; }

    private GroupSelectionItemShim _groupSelectionShim;
    public GroupSelectionItemShim GroupSelectionShim
    {
        get => _groupSelectionShim;
        set
        {
            this.RaiseAndSetIfChanged(ref _groupSelectionShim, value);
            _parameter = GroupSelections.IndexOf(_groupSelectionShim);
            SelectedScenarioCommand.Parameter = _groupSelectionShim.DisplayName;
            SelectedScenarioCommand.Scenario.Scenario.Commands[SelectedScenarioCommand.CommandIndex].Parameter = _parameter;
            ScenarioEditor.Description.UnsavedChanges = true;

            using LiteDatabase db = new(_project.DbFile);
            var itemsCol = db.GetCollection<ItemDescription>(Project.ItemsCollectionName);
            GroupSelection = (GroupSelectionItem)_groupSelectionShim?.GetItem(itemsCol);
        }
    }
    [Reactive]
    public GroupSelectionItem GroupSelection { get; set; }

    public RouteSelectScenarioCommandEditorViewModel(ScenarioEditorViewModel scenarioEditor, PrettyScenarioCommand command, Project project, EditorTabsPanelViewModel tabs)
        : base(scenarioEditor, command, tabs)
    {
        _project = project;
        using LiteDatabase db = new(project.DbFile);
        var itemsCol = db.GetCollection<ItemDescription>(Project.ItemsCollectionName);
        var gsCol = db.GetCollection<GroupSelectionItemShim>(nameof(GroupSelectionItem));

        GroupSelections = new(gsCol.FindAll());
        _groupSelectionShim = GroupSelections.FirstOrDefault(g => g.DisplayName == command.Parameter);
        _parameter = _groupSelectionShim?.Index ?? -1;
        GroupSelection = (GroupSelectionItem)_groupSelectionShim?.GetItem(itemsCol);
    }
}
