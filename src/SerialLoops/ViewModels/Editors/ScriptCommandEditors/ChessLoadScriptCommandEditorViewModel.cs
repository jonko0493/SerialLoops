using System.Collections.ObjectModel;
using HaruhiChokuretsuLib.Util;
using LiteDB;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Items.Shims;
using SerialLoops.Lib.Script;
using SerialLoops.Lib.Script.Parameters;
using SerialLoops.ViewModels.Panels;

namespace SerialLoops.ViewModels.Editors.ScriptCommandEditors;

public class ChessLoadScriptCommandEditorViewModel : ScriptCommandEditorViewModel
{
    public EditorTabsPanelViewModel Tabs { get; }

    public ObservableCollection<ChessPuzzleItemShim> ChessPuzzles { get; }
    private ChessPuzzleItemShim _chessPuzzleShim;
    public ChessPuzzleItemShim ChessPuzzleShim
    {
        get => _chessPuzzleShim;
        set
        {
            this.RaiseAndSetIfChanged(ref _chessPuzzleShim, value);
            ((ChessPuzzleScriptParameter)Command.Parameters[0]).ChessPuzzle = _chessPuzzleShim;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[0] = (short)_chessPuzzleShim.Index;
            ScriptEditor.UpdatePreview();
            ScriptEditor.Description.UnsavedChanges = true;

            using LiteDatabase db = new(ScriptEditor.Window.OpenProject.DbFile);
            var itemsCol = db.GetCollection<ItemDescription>(Project.ItemsCollectionName);
            ChessPuzzle = (ChessPuzzleItem)_chessPuzzleShim.GetItem(itemsCol);
        }
    }
    [Reactive]
    public ChessPuzzleItem ChessPuzzle { get; set; }

    public ChessLoadScriptCommandEditorViewModel(ScriptItemCommand command, ScriptEditorViewModel scriptEditor, MainWindowViewModel window, ILogger log)
        : base(command, scriptEditor, log)
    {
        using LiteDatabase db = new(window.OpenProject.DbFile);
        var itemsCol = db.GetCollection<ItemDescription>(Project.ItemsCollectionName);
        var chessCol = db.GetCollection<ChessPuzzleItemShim>(nameof(ChessPuzzleItem));

        ChessPuzzles = new(chessCol.FindAll());
        _chessPuzzleShim = ((ChessPuzzleScriptParameter)command.Parameters[0]).ChessPuzzle;
        ChessPuzzle = (ChessPuzzleItem)_chessPuzzleShim.GetItem(itemsCol);
        Tabs = window.EditorTabs;
    }
}
