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

namespace SerialLoops.ViewModels.Editors.ScriptCommandEditors;

public class LoadIsomapScriptCommandEditorViewModel : ScriptCommandEditorViewModel
{
    public ObservableCollection<MapItemShim> Maps { get; }
    private MapItemShim _selectedMap;
    public MapItemShim SelectedMap
    {
        get => _selectedMap;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedMap, value);
            ((MapScriptParameter)Command.Parameters[0]).Map = _selectedMap;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[0] = (short)_selectedMap.MapIndex;
            ScriptEditor.Description.UnsavedChanges = true;

            using LiteDatabase db = new(ScriptEditor.Window.OpenProject.DbFile);
            var itemsCol = db.GetCollection<ItemDescription>(Project.ItemsCollectionName);
            Map = (MapItem)_selectedMap.GetItem(itemsCol);
        }
    }
    [Reactive]
    public MapItem Map { get; set; }

    public LoadIsomapScriptCommandEditorViewModel(ScriptItemCommand command, ScriptEditorViewModel scriptEditor, ILogger log)
        : base(command, scriptEditor, log)
    {
        using LiteDatabase db = new(scriptEditor.Window.OpenProject.DbFile);
        var itemsCol = db.GetCollection<ItemDescription>(Project.ItemsCollectionName);
        var mapsCol = db.GetCollection<MapItemShim>(nameof(MapItem));

        Maps  = new(mapsCol.FindAll());
        _selectedMap = ((MapScriptParameter)command.Parameters[0]).Map;
        Map = (MapItem)_selectedMap.GetItem(itemsCol);
    }
}
