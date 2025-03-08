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

public class VcePlayScriptCommandEditorViewModel : ScriptCommandEditorViewModel
{
    public EditorTabsPanelViewModel Tabs { get; }

    public ObservableCollection<VoicedLineItemShim> Vces { get; }
    private VoicedLineItemShim _vceShim;
    public VoicedLineItemShim VceShim
    {
        get => _vceShim;
        set
        {
            this.RaiseAndSetIfChanged(ref _vceShim, value);
            ((VoicedLineScriptParameter)Command.Parameters[0]).VoiceLine = _vceShim;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[0] = (short)_vceShim.Index;
            ScriptEditor.Description.UnsavedChanges = true;

            using LiteDatabase db = new(ScriptEditor.Window.OpenProject.DbFile);
            var itemsCol = db.GetCollection<ItemDescription>(Project.ItemsCollectionName);
            Vce = (VoicedLineItem)_vceShim.GetItem(itemsCol);
        }
    }
    [Reactive]
    public VoicedLineItem Vce { get; set; }

    public VcePlayScriptCommandEditorViewModel(ScriptItemCommand command, ScriptEditorViewModel scriptEditor, ILogger log, MainWindowViewModel window) :
        base(command, scriptEditor, log)
    {
        using LiteDatabase db = new(window.OpenProject.DbFile);
        var itemsCol = db.GetCollection<ItemDescription>(Project.ItemsCollectionName);
        var vceCol = db.GetCollection<VoicedLineItemShim>(nameof(VoicedLineItem));

        Tabs = window.EditorTabs;
        Vces = new(vceCol.FindAll());
        _vceShim = ((VoicedLineScriptParameter)Command.Parameters[0]).VoiceLine;
        Vce = (VoicedLineItem)_vceShim.GetItem(itemsCol);
    }
}
