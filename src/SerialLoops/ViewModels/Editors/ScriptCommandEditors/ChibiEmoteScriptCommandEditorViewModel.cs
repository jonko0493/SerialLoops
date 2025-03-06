using System;
using System.Collections.ObjectModel;
using System.Linq;
using HaruhiChokuretsuLib.Util;
using LiteDB;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SerialLoops.Assets;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Items.Shims;
using SerialLoops.Lib.Script;
using SerialLoops.Lib.Script.Parameters;

namespace SerialLoops.ViewModels.Editors.ScriptCommandEditors;

public class ChibiEmoteScriptCommandEditorViewModel : ScriptCommandEditorViewModel
{
    public ObservableCollection<ChibiItemShim> Chibis { get; }
    public ChibiItemShim ChibiShim
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

    public ObservableCollection<LocalizedChibiEmote> ChibiEmotes { get; } = new(Enum
        .GetValues<ChibiEmoteScriptParameter.ChibiEmote>()
        .Select(e => new LocalizedChibiEmote(e)));

    public LocalizedChibiEmote ChibiEmote
    {
        get => new(((ChibiEmoteScriptParameter)Command.Parameters[1]).Emote);
        set
        {
            ((ChibiEmoteScriptParameter)Command.Parameters[1]).Emote = value.Emote;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[1] = (short)value.Emote;
            this.RaisePropertyChanged();
            ScriptEditor.UpdatePreview();
            ScriptEditor.Description.UnsavedChanges = true;
        }
    }

    public ChibiEmoteScriptCommandEditorViewModel(ScriptItemCommand command, ScriptEditorViewModel scriptEditor, ILogger log) : base(command, scriptEditor, log)
    {
        using LiteDatabase db = new(scriptEditor.Window.OpenProject.DbFile);
        var chibiCol = db.GetCollection<ChibiItemShim>(nameof(ChibiItem));

        Chibis = new(chibiCol.FindAll());
    }
}

public struct LocalizedChibiEmote(ChibiEmoteScriptParameter.ChibiEmote chibiEmote)
{
    public ChibiEmoteScriptParameter.ChibiEmote Emote { get; } = chibiEmote;

    public string DisplayName => Strings.ResourceManager.GetString(Emote.ToString());
}
