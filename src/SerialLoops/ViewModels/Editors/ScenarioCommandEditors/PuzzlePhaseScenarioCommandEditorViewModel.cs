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

public class PuzzlePhaseScenarioCommandEditorViewModel : ScenarioCommandEditorViewModel
{
    private Project _project;
    public ObservableCollection<PuzzleItemShim> Puzzles { get; set; }

    private PuzzleItemShim _puzzleShim;
    public PuzzleItemShim PuzzleShim
    {
        get => _puzzleShim;
        set
        {
            this.RaiseAndSetIfChanged(ref _puzzleShim, value);
            _parameter = _puzzleShim.PuzzleIndex;
            SelectedScenarioCommand.Parameter = _puzzleShim.DisplayName;
            SelectedScenarioCommand.Scenario.Scenario.Commands[SelectedScenarioCommand.CommandIndex].Parameter = _parameter;
            ScenarioEditor.Description.UnsavedChanges = true;

            using LiteDatabase db = new(_project.DbFile);
            var itemsCol = db.GetCollection<ItemDescription>(Project.ItemsCollectionName);
            Puzzle = (PuzzleItem)_puzzleShim.GetItem(itemsCol);
        }
    }
    [Reactive]
    public PuzzleItem Puzzle { get; set; }

    public PuzzlePhaseScenarioCommandEditorViewModel(ScenarioEditorViewModel scenarioEditor, PrettyScenarioCommand command, Project project, EditorTabsPanelViewModel tabs)
        : base(scenarioEditor, command, tabs)
    {
        _project = project;
        using LiteDatabase db = new(project.DbFile);
        var itemsCol = db.GetCollection<ItemDescription>(Project.ItemsCollectionName);
        var puzzlesCol = db.GetCollection<PuzzleItemShim>(nameof(PuzzleItem));

        Puzzles = new(puzzlesCol.FindAll());
        _puzzleShim = Puzzles.FirstOrDefault(s => s.DisplayName == command.Parameter);
        _parameter = _puzzleShim?.PuzzleIndex ?? 0;
        Puzzle = (PuzzleItem)_puzzleShim?.GetItem(itemsCol);
    }
}
