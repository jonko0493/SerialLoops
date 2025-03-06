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
using SerialLoops.ViewModels.Panels;

namespace SerialLoops.ViewModels.Editors.ScriptCommandEditors;

public class SndPlayScriptCommandEditorViewModel : ScriptCommandEditorViewModel
{
    public EditorTabsPanelViewModel Tabs { get; }

    [Reactive]
    public ObservableCollection<SfxItemShim> SfxChoices { get; set; }
    private SfxItemShim _selectedSfxShim;
    public SfxItemShim SelectedSfxShim
    {
        get => _selectedSfxShim;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedSfxShim, value);
            ((SfxScriptParameter)Command.Parameters[0]).Sfx = _selectedSfxShim;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[0] = _selectedSfxShim.Index;
            ScriptEditor.Description.UnsavedChanges = true;

            using LiteDatabase db = new(ScriptEditor.Window.OpenProject.DbFile);
            var itemsCol = db.GetCollection<ItemDescription>(Project.ItemsCollectionName);
            SelectedSfx = (SfxItem)_selectedSfxShim.GetItem(itemsCol);
        }
    }
    [Reactive]
    public SfxItem SelectedSfx { get; set; }

    public ObservableCollection<SfxModeLocalized> SfxPlayModes { get; } =
        new(Enum.GetValues<SfxModeScriptParameter.SfxMode>().Select(m => new SfxModeLocalized(m)));
    private SfxModeLocalized _sfxMode;
    public SfxModeLocalized SfxMode
    {
        get => _sfxMode;
        set
        {
            this.RaiseAndSetIfChanged(ref _sfxMode, value);
            ((SfxModeScriptParameter)Command.Parameters[1]).Mode = _sfxMode.Mode;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[1] = (short)_sfxMode.Mode;
            ScriptEditor.Description.UnsavedChanges = true;
        }
    }

    private short _volume;
    public short Volume
    {
        get => _volume;
        set
        {
            this.RaiseAndSetIfChanged(ref _volume, value);
            ((ShortScriptParameter)Command.Parameters[2]).Value = _volume;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[2] = _volume;
            ScriptEditor.Description.UnsavedChanges = true;
        }
    }

    private bool _loadSound;
    public bool LoadSound
    {
        get => _loadSound;
        set
        {
            this.RaiseAndSetIfChanged(ref _loadSound, value);
            if (_loadSound)
            {
                ((BoolScriptParameter)Command.Parameters[3]).Value = true;
                Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                    .Objects[Command.Index].Parameters[3] = ((BoolScriptParameter)Command.Parameters[3]).TrueValue;
                CrossfadeMin = -1;
                CrossfadeMax = -1;
                CrossfadeTime = -1;
            }
            else
            {
                CrossfadeMin = 0;
                CrossfadeMax = short.MaxValue;
                CrossfadeTime = 0;
            }
            ScriptEditor.Description.UnsavedChanges = true;
        }
    }

    [Reactive]
    public short CrossfadeMin { get; set; } = 0;

    [Reactive]
    public short CrossfadeMax { get; set; } = short.MaxValue;

    private short _crossfadeTime;
    public short CrossfadeTime
    {
        get => _crossfadeTime;
        set
        {
            this.RaiseAndSetIfChanged(ref _crossfadeTime, value);
            ((ShortScriptParameter)Command.Parameters[4]).Value = _crossfadeTime;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[4] = _crossfadeTime;
            ScriptEditor.Description.UnsavedChanges = true;
        }
    }

    public SndPlayScriptCommandEditorViewModel(ScriptItemCommand command, ScriptEditorViewModel scriptEditor, ILogger log, MainWindowViewModel window) :
        base(command, scriptEditor, log)
    {
        using LiteDatabase db = new(window.OpenProject.DbFile);
        var itemsCol = db.GetCollection<ItemDescription>(Project.ItemsCollectionName);
        var sfxCol = db.GetCollection<SfxItemShim>(nameof(SfxItem));

        Tabs = window.EditorTabs;
        SfxChoices = new(sfxCol.FindAll());
        _selectedSfxShim = ((SfxScriptParameter)Command.Parameters[0]).Sfx;
        SelectedSfx = (SfxItem)_selectedSfxShim.GetItem(itemsCol);
        _sfxMode = new(((SfxModeScriptParameter)Command.Parameters[1]).Mode);
        _volume = ((ShortScriptParameter)Command.Parameters[2]).Value;
        _crossfadeTime = ((ShortScriptParameter)Command.Parameters[4]).Value;
        _loadSound = ((BoolScriptParameter)Command.Parameters[3]).Value && _crossfadeTime < 0;
    }
}

public readonly struct SfxModeLocalized(SfxModeScriptParameter.SfxMode mode)
{
    public SfxModeScriptParameter.SfxMode Mode { get; } = mode;
    public string DisplayText { get; } = Strings.ResourceManager.GetString(mode.ToString());
}
