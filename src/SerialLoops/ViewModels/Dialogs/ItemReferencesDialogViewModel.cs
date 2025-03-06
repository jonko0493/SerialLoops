using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using HaruhiChokuretsuLib.Util;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SerialLoops.Assets;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Items.Shims;
using SerialLoops.Models;
using SerialLoops.Utility;
using SerialLoops.ViewModels.Panels;
using SerialLoops.Views.Dialogs;

namespace SerialLoops.ViewModels.Dialogs;

public class ItemReferencesDialogViewModel : ViewModelBase
{
    private readonly ILogger _log;
    private Project _project;
    [Reactive]
    public KeyGesture CloseHotKey { get; private set; }

    [Reactive]
    public string FoundReferencesLabel { get; private set; }
    public EditorTabsPanelViewModel Tabs { get; }
    public ReactiveItemShim Item { get; }

    public string Title { get; }

    private ObservableCollection<ReactiveItemShim> _items;
    public ObservableCollection<ReactiveItemShim> Items
    {
        get => _items;
        set
        {
            this.RaiseAndSetIfChanged(ref _items, value);
            Source = new(GetSections())
            {
                Columns =
                {
                    new HierarchicalExpanderColumn<ITreeItem>(
                        new TemplateColumn<ITreeItem>(null, new FuncDataTemplate<ITreeItem>((val, _) => val?.GetDisplay())),
                        i => i.Children
                    ),
                },
            };
            Source.ExpandAll();
        }
    }

    public ICommand OpenItemCommand { get; }
    public ICommand CloseCommand { get; }

    public ItemReferencesDialogViewModel(ReactiveItemShim item, Project project, EditorTabsPanelViewModel tabs, ILogger log)
    {
        _log = log;
        _project = project;

        Tabs = tabs;
        Item = item;
        Title = string.Format(Strings.References_to__0_, Item.DisplayName);
        Items = new(item.Shim.GetReferencesTo(project));
        FoundReferencesLabel = string.Format(Strings._0__results_found, Items.Count);
        OpenItemCommand = ReactiveCommand.Create<TreeDataGrid>(OpenItem);
        CloseCommand = ReactiveCommand.Create<ItemReferencesDialog>(dialog => dialog.Close());
        CloseHotKey = new(Key.Escape);
    }

    public void OpenItem(TreeDataGrid viewer)
    {
        ItemDescription item = _project.FindItem(((ITreeItem)viewer.RowSelection?.SelectedItem)?.Text);
        if (item is not null)
        {
            Tabs.OpenTab(new(item));
        }
    }

    private ObservableCollection<ITreeItem> GetSections()
    {
        return new(Items.GroupBy(i => i.Type)
            .OrderBy(g => ControlGenerator.LocalizeItemTypes(g.Key))
            .Select(g => new SectionTreeItem(
                ControlGenerator.LocalizeItemTypes(g.Key),
                g.Select(i => new ItemShimTreeItem(i)),
                ControlGenerator.GetVectorIcon(g.Key.ToString(), _log, size: 16)
            )));
    }

    [Reactive]
    public HierarchicalTreeDataGridSource<ITreeItem> Source { get; private set; }
}
