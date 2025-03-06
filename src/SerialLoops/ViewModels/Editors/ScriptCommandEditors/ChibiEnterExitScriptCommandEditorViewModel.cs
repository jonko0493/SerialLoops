using System;
using System.Collections.ObjectModel;
using System.Linq;
using HaruhiChokuretsuLib.Util;
using LiteDB;
using ReactiveUI;
using SerialLoops.Assets;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Script;
using SerialLoops.Lib.Script.Parameters;

namespace SerialLoops.ViewModels.Editors.ScriptCommandEditors;

public class ChibiEnterExitScriptCommandEditorViewModel : ScriptCommandEditorViewModel
{
    public ObservableCollection<ChibiItem> Chibis { get; }
    public ChibiItem Chibi
    {
        get => ((ChibiScriptParameter)Command.Parameters[0]).Chibi;
        set
        {
            ((ChibiScriptParameter)Command.Parameters[0]).Chibi = value;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[0] = (short)value.TopScreenIndex;
            this.RaisePropertyChanged();
            ScriptEditor.UpdatePreview();
            ScriptEditor.Description.UnsavedChanges = true;
        }
    }

    public ObservableCollection<LocalizedChibiEnterExitType> EnterExitModes { get; } = new(Enum.GetValues<ChibiEnterExitScriptParameter.ChibiEnterExitType>()
        .Select(t => new LocalizedChibiEnterExitType(t)));
    public LocalizedChibiEnterExitType EnterExitMode
    {
        get => new(((ChibiEnterExitScriptParameter)Command.Parameters[1]).Mode);
        set
        {
            ((ChibiEnterExitScriptParameter)Command.Parameters[1]).Mode = value.Mode;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[1] = (short)value.Mode;
            this.RaisePropertyChanged();
            ScriptEditor.UpdatePreview();
            ScriptEditor.Description.UnsavedChanges = true;
        }
    }

    public short Delay
    {
        get => ((ShortScriptParameter)Command.Parameters[2]).Value;
        set
        {
            ((ShortScriptParameter)Command.Parameters[2]).Value = value;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[2] = value;
            this.RaisePropertyChanged();
            ScriptEditor.Description.UnsavedChanges = true;
        }
    }

    public ChibiEnterExitScriptCommandEditorViewModel(ScriptItemCommand command, ScriptEditorViewModel scriptEditor, ILogger log)
        : base(command, scriptEditor, log)
    {
        using LiteDatabase db = new(scriptEditor.Window.OpenProject.DbFile);
        var itemsCol = db.GetCollection<ItemDescription>(Project.ItemsCollectionName);

        Chibis = new(itemsCol.Find(i => i.Type == ItemDescription.ItemType.Chibi).Cast<ChibiItem>());
    }
}

public readonly struct LocalizedChibiEnterExitType(ChibiEnterExitScriptParameter.ChibiEnterExitType type)
{
    public ChibiEnterExitScriptParameter.ChibiEnterExitType Mode { get; } = type;
    public string DisplayName { get; } = Strings.ResourceManager.GetString(type.ToString());
}
