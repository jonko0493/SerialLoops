using System.Collections.ObjectModel;
using System.Linq;
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

public class SceneGotoScriptCommandEditorViewModel : ScriptCommandEditorViewModel
{
    public EditorTabsPanelViewModel Tabs { get; }

    public ObservableCollection<ScriptItemShim> Scripts { get; }
    private ScriptItemShim _selectedScript;

    public ScriptItemShim SelectedScript
    {
        get => _selectedScript;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedScript, value);
            ((ConditionalScriptParameter)Command.Parameters[0]).Conditional = _selectedScript.Name;
            if (Script.Event.ConditionalsSection.Objects.Contains(_selectedScript.Name))
            {
                Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                        .Objects[Command.Index].Parameters[0] =
                    (short)Script.Event.ConditionalsSection.Objects.IndexOf(_selectedScript.Name);
            }
            else
            {
                Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                        .Objects[Command.Index].Parameters[0] =
                    (short)(Script.Event.ConditionalsSection.Objects.Count - 1);
                Script.Event.ConditionalsSection.Objects.Insert(Script.Event.ConditionalsSection.Objects.Count - 1, _selectedScript.Name);
            }
            ScriptEditor.Description.UnsavedChanges = true;
            
            using LiteDatabase db = new(ScriptEditor.Window.OpenProject.DbFile);
            var itemsCol = db.GetCollection<ItemDescription>(Project.ItemsCollectionName);
            ActualScript = (ScriptItem)_selectedScript.GetItem(itemsCol);
        }
    }
    [Reactive]
    public ScriptItem ActualScript { get; set; }

    public SceneGotoScriptCommandEditorViewModel(ScriptItemCommand command, ScriptEditorViewModel scriptEditor, ILogger log, MainWindowViewModel window) :
        base(command, scriptEditor, log)
    {
        using LiteDatabase db = new(scriptEditor.Window.OpenProject.DbFile);
        var itemsCol = db.GetCollection<ItemDescription>(Project.ItemsCollectionName);
        var scriptsCol = db.GetCollection<ScriptItemShim>(nameof(ScriptItem));

        Tabs = window.EditorTabs;
        Scripts = new(scriptsCol.FindAll());
        try
        {
            _selectedScript = scriptsCol.FindById(((ConditionalScriptParameter)Command.Parameters[0]).Conditional);
        }
        catch
        {
            _selectedScript = scriptsCol.FindOne(s => true);
        }
        ActualScript = (ScriptItem)_selectedScript.GetItem(itemsCol);
    }
}
