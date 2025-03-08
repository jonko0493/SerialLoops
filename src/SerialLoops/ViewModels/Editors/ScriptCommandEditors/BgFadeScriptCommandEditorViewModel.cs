using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using HaruhiChokuretsuLib.Archive.Data;
using HaruhiChokuretsuLib.Util;
using LiteDB;
using ReactiveUI;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Items.Shims;
using SerialLoops.Lib.Script;
using SerialLoops.Lib.Script.Parameters;
using SerialLoops.ViewModels.Dialogs;
using SerialLoops.ViewModels.Panels;
using SerialLoops.Views.Dialogs;

namespace SerialLoops.ViewModels.Editors.ScriptCommandEditors;

public class BgFadeScriptCommandEditorViewModel : ScriptCommandEditorViewModel
{
    private MainWindowViewModel _window;
    public EditorTabsPanelViewModel Tabs { get; }

    private BackgroundItem _bg;
    public BackgroundItem Bg
    {
        get => _bg;
        set
        {
            this.RaiseAndSetIfChanged(ref _bg, value);
            ((BgScriptParameter)Command.Parameters[0]).Background = new(_bg);
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[0] = (short?)_bg?.Id ?? 0;
            ScriptEditor.UpdatePreview();
            ScriptEditor.Description.UnsavedChanges = true;
        }
    }

    private BackgroundItem _cg;
    public BackgroundItem Cg
    {
        get => _cg;
        set
        {
            this.RaiseAndSetIfChanged(ref _cg, value);
            ((BgScriptParameter)Command.Parameters[1]).Background = new(_cg);
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[1] = (short?)_cg?.Id ?? 0;
            ScriptEditor.UpdatePreview();
            ScriptEditor.Description.UnsavedChanges = true;
        }
    }

    private short _fadeTime;
    public short FadeTime
    {
        get => _fadeTime;
        set
        {
            this.RaiseAndSetIfChanged(ref _fadeTime, value);
            ((ShortScriptParameter)Command.Parameters[2]).Value = _fadeTime;
            ScriptEditor.Description.UnsavedChanges = true;
        }
    }

    public ICommand ReplaceBgCommand { get; }
    public ICommand ReplaceCgCommand { get; }

    public BgFadeScriptCommandEditorViewModel(ScriptItemCommand command, ScriptEditorViewModel scriptEditor, ILogger log, MainWindowViewModel window)
        : base(command, scriptEditor, log)
    {
        _window = window;
        Tabs = _window.EditorTabs;
        using LiteDatabase db = new(scriptEditor.Window.OpenProject.DbFile);
        var itemsCol = db.GetCollection<ItemDescription>(Project.ItemsCollectionName);
        _bg = ((BgScriptParameter)Command.Parameters[0]).GetBackground(itemsCol);
        _cg = ((BgScriptParameter)Command.Parameters[1]).GetBackground(itemsCol);
        _fadeTime = ((ShortScriptParameter)Command.Parameters[2]).Value;
        ReplaceBgCommand = ReactiveCommand.CreateFromTask(ReplaceBg);
        ReplaceCgCommand = ReactiveCommand.CreateFromTask(ReplaceCg);
    }

    private async Task ReplaceBg()
    {
        using LiteDatabase db = new(_window.OpenProject.DbFile);
        var itemsCol = db.GetCollection<ItemDescription>(Project.ItemsCollectionName);
        var bgCol = db.GetCollection<BackgroundItemShim>(nameof(BackgroundItem));

        // Order of the predicate matters here as "NONE" short circuits the NonePreviewableGraphic, preventing it from being cast
        GraphicSelectionDialogViewModel graphicSelectionDialog = new(new List<IPreviewableGraphic> { NonePreviewableGraphic.BACKGROUND }.Concat(bgCol.FindAll().Select(b => b.GetItem(itemsCol)).Cast<IPreviewableGraphic>()),
            Bg, _window.OpenProject, _window.Log, i => i.Name == "NONE" || ((BackgroundItem)i).BackgroundType == BgType.TEX_BG);
        IPreviewableGraphic bg = await new GraphicSelectionDialog { DataContext = graphicSelectionDialog }.ShowDialog<IPreviewableGraphic>(_window.Window);
        if (bg is null)
        {
            return;
        }
        else if (bg.Text == "NONE")
        {
            Bg = null;
        }
        else
        {
            Bg = (BackgroundItem)bg;
            Cg = null;
        }
    }

    private async Task ReplaceCg()
    {
        using LiteDatabase db = new(_window.OpenProject.DbFile);
        var itemsCol = db.GetCollection<ItemDescription>(Project.ItemsCollectionName);
        var bgCol = db.GetCollection<BackgroundItemShim>(nameof(BackgroundItem));

        // Order of the predicate matters here as "NONE" short circuits the NonePreviewableGraphic, preventing it from being cast
        GraphicSelectionDialogViewModel graphicSelectionDialog = new(new List<IPreviewableGraphic> { NonePreviewableGraphic.BACKGROUND }.Concat(bgCol.FindAll().Select(b => b.GetItem(itemsCol)).Cast<IPreviewableGraphic>()),
            Cg, _window.OpenProject, _window.Log, i => i.Name == "NONE" ||
                                                       (((BackgroundItem)i).BackgroundType != BgType.TEX_BG && ((BackgroundItem)i).BackgroundType != BgType.KINETIC_SCREEN));
        IPreviewableGraphic cg = await new GraphicSelectionDialog { DataContext = graphicSelectionDialog }.ShowDialog<IPreviewableGraphic>(_window.Window);
        if (cg is null)
        {
            return;
        }
        else if (cg.Text == "NONE")
        {
            Cg = null;
        }
        else
        {
            Cg = (BackgroundItem)cg;
            Bg = null;
        }
    }
}
