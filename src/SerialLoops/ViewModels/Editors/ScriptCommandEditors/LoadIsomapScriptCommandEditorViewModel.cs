using System.Collections.ObjectModel;
using System.Linq;
using HaruhiChokuretsuLib.Util;
using LiteDB;
using ReactiveUI;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Script;
using SerialLoops.Lib.Script.Parameters;

namespace SerialLoops.ViewModels.Editors.ScriptCommandEditors;

public class LoadIsomapScriptCommandEditorViewModel : ScriptCommandEditorViewModel
{
    public ObservableCollection<MapItem> Maps { get; }
    private MapItem _selectedMap;
    public MapItem SelectedMap
    {
        get => _selectedMap;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedMap, value);
            ((MapScriptParameter)Command.Parameters[0]).Map = _selectedMap;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[0] = (short)_selectedMap.Map.Index;
            Script.UnsavedChanges = true;
        }
    }

    public LoadIsomapScriptCommandEditorViewModel(ScriptItemCommand command, ScriptEditorViewModel scriptEditor, ILogger log)
        : base(command, scriptEditor, log)
    {
        using LiteDatabase db = new(scriptEditor.Window.OpenProject.DbFile);
        var itemsCol = db.GetCollection<ItemDescription>(Project.ItemsTableName);

        Maps  = new(itemsCol.Find(i => i.Type == ItemDescription.ItemType.Map).Cast<MapItem>());
        _selectedMap = ((MapScriptParameter)command.Parameters[0]).Map;
    }
}
