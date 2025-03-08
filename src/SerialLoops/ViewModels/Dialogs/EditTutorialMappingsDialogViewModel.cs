using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Util;
using LiteDB;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Items.Shims;
using SerialLoops.ViewModels.Panels;
using SerialLoops.Views.Dialogs;
using IO = SerialLoops.Lib.IO;

namespace SerialLoops.ViewModels.Dialogs;

public class EditTutorialMappingsDialogViewModel : ViewModelBase
{
    public Project Project { get; }
    public ObservableCollection<TutorialMapping> Tutorials { get; }

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    public EditTutorialMappingsDialogViewModel(Project project, EditorTabsPanelViewModel tabs, ILogger log)
    {
        Project = project;
        Tutorials = new(project.TutorialFile.Tutorials.Where(t => t.AssociatedScript > 0)
            .Select(t => new TutorialMapping(t, project, tabs)));

        SaveCommand = ReactiveCommand.Create<EditTutorialMappingsDialog>(dialog =>
        {
            foreach (TutorialMapping tutorial in Tutorials)
            {
                project.TutorialFile.Tutorials.First(t => t.Id == tutorial.Flag)
                    .AssociatedScript = (short)tutorial.Script.Event.Index;
            }
            IO.WriteStringFile(Path.Combine("assets", "events", $"{project.TutorialFile.Index:X3}.s"),
                project.TutorialFile.GetSource([]), project, log);
            dialog.Close();
        });
        CancelCommand = ReactiveCommand.Create<EditTutorialMappingsDialog>(dialog => dialog.Close());
    }
}

public class TutorialMapping : ReactiveObject
{
    public EditorTabsPanelViewModel Tabs { get; }

    public ObservableCollection<ScriptItemShim> Scripts { get; }

    private ScriptItemShim _scriptShim;
    public ScriptItemShim ScriptShim
    {
        get => _scriptShim;
        set
        {
            this.RaiseAndSetIfChanged(ref _scriptShim, value);
            using LiteDatabase db = new(_project.DbFile);
            var itemsCol = db.GetCollection<ItemDescription>(Project.ItemsCollectionName);
            Script = (ScriptItem)_scriptShim.GetItem(itemsCol);
        }
    }

    [Reactive]
    public int Flag { get; set; }

    public string TutorialName => $"Tutorial {Flag}";

    [Reactive]
    public ScriptItem Script { get; set; }

    private Project _project;

    public TutorialMapping(Tutorial tutorial, Project project, EditorTabsPanelViewModel tabs)
    {
        using LiteDatabase db = new(project.DbFile);
        var itemsCol = db.GetCollection<ItemDescription>(Project.ItemsCollectionName);
        var scriptsCol = db.GetCollection<ScriptItemShim>(nameof(ScriptItem));
        _project = project;

        Tabs = tabs;
        Scripts = new(scriptsCol.FindAll());
        _scriptShim = scriptsCol.FindOne(s => s.EventIndex == tutorial.AssociatedScript);
        Flag = tutorial.Id;
        Script = (ScriptItem)_scriptShim.GetItem(itemsCol);
    }
}
