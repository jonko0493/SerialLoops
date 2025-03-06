using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib;
using SerialLoops.Lib.Items.Shims;
using SerialLoops.ViewModels.Panels;

namespace SerialLoops.ViewModels.Editors;

public class EditorViewModel(ReactiveItemDescription item, MainWindowViewModel window, ILogger log, Project project = null, EditorTabsPanelViewModel tabs = null, ItemExplorerPanelViewModel explorer = null) : ViewModelBase
{
    public MainWindowViewModel Window { get; protected set; } = window;
    protected ILogger _log = log;
    protected Project _project = project;
    protected EditorTabsPanelViewModel _tabs = tabs;
    protected ItemExplorerPanelViewModel _explorer = explorer;

    public ReactiveItemDescription Description { get; set; } = item;

    public string IconSource => $"avares://SerialLoops/Assets/Icons/{Description.Item.Type.ToString().Replace(' ', '_')}.png";
}
