using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
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

public class TopicGetScriptCommandEditorViewModel : ScriptCommandEditorViewModel
{
    public EditorTabsPanelViewModel Tabs { get; }

    [Reactive]
    public short TopicId { get; set; }

    public ObservableCollection<TopicItemShim> Topics { get; }
    private TopicItemShim _selectedTopic;
    public TopicItemShim SelectedTopic
    {
        get => _selectedTopic;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedTopic, value);
            ((TopicScriptParameter)Command.Parameters[0]).TopicId = _selectedTopic.TopicEntry.Id;
            TopicId = _selectedTopic.TopicEntry.Id;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[0] = _selectedTopic.TopicEntry.Id;
            ScriptEditor.Description.UnsavedChanges = true;
        }
    }
    [Reactive]
    public TopicItem RealTopic { get; set; }

    public ICommand SelectTopicCommand { get; }

    public TopicGetScriptCommandEditorViewModel(ScriptItemCommand command, ScriptEditorViewModel scriptEditor, ILogger log, MainWindowViewModel window)
        : base(command, scriptEditor, log)
    {
        using LiteDatabase db = new(window.OpenProject.DbFile);
        var itemsCol = db.GetCollection<ItemDescription>(Project.ItemsCollectionName);
        var topicsCol = db.GetCollection<TopicItemShim>(nameof(TopicItemShim));

        Tabs = window.EditorTabs;
        Topics = new(topicsCol.FindAll());
        TopicId = ((TopicScriptParameter)Command.Parameters[0]).TopicId;
        _selectedTopic = topicsCol.FindOne(t => t.TopicEntry.Id == TopicId);
        RealTopic = (TopicItem)_selectedTopic.GetItem(itemsCol);
        SelectTopicCommand = ReactiveCommand.Create(() => SelectedTopic = Topics.FirstOrDefault());
    }
}
