using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Util;
using LiteDB;
using MsBox.Avalonia.Enums;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SerialLoops.Assets;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Items.Shims;
using SerialLoops.Models;
using SerialLoops.Utility;
using SerialLoops.ViewModels.Dialogs;
using SerialLoops.ViewModels.Editors.ScenarioCommandEditors;
using SerialLoops.Views.Dialogs;
using static HaruhiChokuretsuLib.Archive.Event.ScenarioCommand;

namespace SerialLoops.ViewModels.Editors;

public class ScenarioEditorViewModel : EditorViewModel
{
    private ScenarioItem _scenario;
    private PrettyScenarioCommand _selectedCommand;

    public ObservableCollection<PrettyScenarioCommand> Commands { get; set; }

    public PrettyScenarioCommand SelectedCommand
    {
        get => _selectedCommand;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedCommand, value);
            if (_selectedCommand is not null)
            {
                CurrentCommandViewModel = GetScenarioCommandEditor(SelectedCommand);
            }
            else
            {
                CurrentCommandViewModel = null;
            }
        }
    }
    [Reactive]
    public ScenarioCommandEditorViewModel CurrentCommandViewModel { get; set; }

    public ICommand AddCommand { get; set; }
    public ICommand DeleteCommand { get; set; }
    public ICommand ClearCommand { get; set; }
    public ICommand UpCommand { get; set; }
    public ICommand DownCommand { get; set; }

    public ScenarioEditorViewModel(ScenarioItem scenario, MainWindowViewModel window, ILogger log) : base(new(scenario), window, log)
    {
        _scenario = scenario;
        Commands = new(scenario.ScenarioCommands.Select((s, i) => new PrettyScenarioCommand(s, i, scenario)));
        AddCommand = ReactiveCommand.Create(Add);
        DeleteCommand = ReactiveCommand.Create(Delete);
        ClearCommand = ReactiveCommand.CreateFromTask(Clear);
        UpCommand = ReactiveCommand.Create(Up);
        DownCommand = ReactiveCommand.Create(Down);
    }

    private async void Add()
    {
        int selectedIndex = Math.Min(_scenario.Scenario.Commands.Count - 1, Commands.IndexOf(SelectedCommand));
        ScenarioVerb? newVerb = await new AddScenarioCommandDialog { DataContext = new AddScenarioCommandDialogViewModel() }.ShowDialog<ScenarioVerb?>(Window.Window);
        if (newVerb is null)
        {
            return;
        }

        using LiteDatabase db = new(Window.OpenProject.DbFile);
        var itemsCol = db.GetCollection<ItemDescription>(Project.ItemsCollectionName);
        var puzzleCol = db.GetCollection<PuzzleItemShim>(nameof(PuzzleItem));
        var scriptCol = db.GetCollection<ScriptItemShim>(nameof(ScriptItem));

        int param = newVerb switch
        {
            ScenarioVerb.NEW_GAME => 1,
            ScenarioVerb.PUZZLE_PHASE => ((PuzzleItem)puzzleCol.FindOne(p => true).GetItem(itemsCol)).Puzzle.Index,
            ScenarioVerb.LOAD_SCENE => ((ScriptItem)scriptCol.FindOne(s => true).GetItem(itemsCol)).Event.Index,
            _ => 0,
        };
        ScenarioCommand newCommand = new((ScenarioVerb)newVerb, param);
        _scenario.Scenario.Commands.Insert(selectedIndex + 1, newCommand);
        Commands.Insert(selectedIndex + 1, new(_scenario.GetCommandMacro(newCommand), selectedIndex + 1, _scenario));
        if (SelectedCommand is not null)
        {
            SelectedCommand.CommandIndex = Commands.IndexOf(SelectedCommand);
        }
        Description.UnsavedChanges = true;
    }
    private void Delete()
    {
        int selectedIndex = Commands.IndexOf(SelectedCommand);
        if (selectedIndex < 0 || selectedIndex >= _scenario.Scenario.Commands.Count)
        {
            return;
        }

        Commands.RemoveAt(selectedIndex);
        _scenario.Scenario.Commands.RemoveAt(selectedIndex);

        Description.UnsavedChanges = true;
    }
    private async Task Clear()
    {
        if (await Window.Window.ShowMessageBoxAsync(Strings.Clear_Scenario, Strings.Clear_all_commands_from_the_game_scenario__nThis_action_is_irreversible_,
                ButtonEnum.YesNo, Icon.Warning, _log) == ButtonResult.Yes)
        {
            Commands.Clear();
            _scenario.Scenario.Commands.Clear();

            Description.UnsavedChanges = true;
        }
    }
    private void Up()
    {
        int selectedIndex = Commands.IndexOf(SelectedCommand);
        if (selectedIndex <= 0 || selectedIndex >= _scenario.Scenario.Commands.Count)
        {
            return;
        }

        Commands.Swap(selectedIndex, selectedIndex - 1);
        _scenario.Scenario.Commands.Swap(selectedIndex, selectedIndex - 1);
        SelectedCommand = Commands.ElementAt(selectedIndex - 1);

        Description.UnsavedChanges = true;
    }
    private void Down()
    {
        int selectedIndex = Commands.IndexOf(SelectedCommand);
        if (selectedIndex < 0 || selectedIndex >= _scenario.Scenario.Commands.Count - 1)
        {
            return;
        }

        Commands.Swap(selectedIndex, selectedIndex + 1);
        _scenario.Scenario.Commands.Swap(selectedIndex, selectedIndex + 1);
        SelectedCommand = Commands.ElementAt(selectedIndex + 1);

        Description.UnsavedChanges = true;
    }
    public ScenarioCommandEditorViewModel GetScenarioCommandEditor(PrettyScenarioCommand command)
    {
        return command.Verb switch
        {
            ScenarioVerb.LOAD_SCENE => new LoadSceneScenarioCommandEditorViewModel(this, command, Window.OpenProject, Window.EditorTabs),
            ScenarioVerb.PUZZLE_PHASE => new PuzzlePhaseScenarioCommandEditorViewModel(this, command, Window.OpenProject, Window.EditorTabs),
            ScenarioVerb.ROUTE_SELECT => new RouteSelectScenarioCommandEditorViewModel(this, command, Window.OpenProject, Window.EditorTabs),
            _ => new(this, command, Window.EditorTabs),
        };
    }
}
