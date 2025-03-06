using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using HaruhiChokuretsuLib.Util;
using MsBox.Avalonia.Enums;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SerialLoops.Assets;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Items.Shims;
using SerialLoops.Utility;
using SerialLoops.ViewModels.Editors;

namespace SerialLoops.ViewModels.Panels;

public class EditorTabsPanelViewModel : ViewModelBase
{
    private readonly Project _project;
    private readonly ILogger _log;

    public MainWindowViewModel MainWindow { get; private set; }

    [Reactive]
    public EditorViewModel SelectedTab { get; set; }
    [Reactive]
    public bool ShowTabsPanel { get; set; }

    public ICommand TabSwitchedCommand { get; set; }
    public ObservableCollection<EditorViewModel> Tabs { get; set; } = [];

    public EditorTabsPanelViewModel(MainWindowViewModel mainWindow, Project project, ILogger log)
    {
        TabSwitchedCommand = ReactiveCommand.Create(OnTabSwitched);
        MainWindow = mainWindow;
        _project = project;
        _log = log;
    }

    public void OpenTab(ReactiveItemDescription item)
    {
        foreach (EditorViewModel tab in Tabs)
        {
            if (tab.Description.Item.Name.Equals(item.Item.Name))
            {
                SelectedTab = tab;
                ShowTabsPanel = Tabs.Any();
                return;
            }
        }

        EditorViewModel newTab = CreateTab(item);
        if (newTab is not null)
        {
            Tabs.Add(newTab);
            SelectedTab = newTab;
        }
        ShowTabsPanel = Tabs.Any();
    }

    private EditorViewModel CreateTab(ReactiveItemDescription item)
    {
        switch (item.Item.Type)
        {
            case ItemDescription.ItemType.Background:
                return new BackgroundEditorViewModel(item, MainWindow, _project, _log);
            case ItemDescription.ItemType.BGM:
                return new BackgroundMusicEditorViewModel(item, MainWindow, _project, _log);
            case ItemDescription.ItemType.Character:
                return new CharacterEditorViewModel(item, MainWindow, _log);
            case ItemDescription.ItemType.Character_Sprite:
                return new CharacterSpriteEditorViewModel(item, MainWindow, _log);
            case ItemDescription.ItemType.Chess_Puzzle:
                return new ChessPuzzleEditorViewModel(item, MainWindow, _log);
            case ItemDescription.ItemType.Group_Selection:
                return new GroupSelectionEditorViewModel(item, MainWindow, _log);
            case ItemDescription.ItemType.Item:
                return new ItemEditorViewModel(item, MainWindow, _log);
            case ItemDescription.ItemType.Layout:
                return new LayoutEditorViewModel(item, MainWindow, _log);
            case ItemDescription.ItemType.Map:
                return new MapEditorViewModel(item, MainWindow, _log);
            case ItemDescription.ItemType.Place:
                return new PlaceEditorViewModel(item, MainWindow, _log);
            case ItemDescription.ItemType.Puzzle:
                return new PuzzleEditorViewModel(item, MainWindow, _log);
            case ItemDescription.ItemType.Scenario:
                return new ScenarioEditorViewModel(item, MainWindow, _log);
            case ItemDescription.ItemType.Script:
                return new ScriptEditorViewModel(item, MainWindow, _log);
            case ItemDescription.ItemType.SFX:
                return new SfxEditorViewModel(item, MainWindow, _log);
            case ItemDescription.ItemType.System_Texture:
                return new SystemTextureEditorViewModel(item, MainWindow, _project, _log);
            case ItemDescription.ItemType.Topic:
                return new TopicEditorViewModel(item, MainWindow, _log);
            case ItemDescription.ItemType.Voice:
                return new VoicedLineEditorViewModel(item, MainWindow, _log);
            case ItemDescription.ItemType.Save:
                return new SaveEditorViewModel(item, MainWindow, _log);
            default:
                _log.LogError(Strings.Invalid_item_type_);
                return null;
        }
    }

    private void OnTabSwitched()
    {

    }

    public async Task OnTabClosed(EditorViewModel closedEditor)
    {
        switch (closedEditor.Description.Item.Type)
        {
            case ItemDescription.ItemType.BGM:
                ((BackgroundMusicEditorViewModel)closedEditor).BgmPlayer.Stop();
                break;
            case ItemDescription.ItemType.Character_Sprite:
                ((CharacterSpriteEditorViewModel)closedEditor).AnimatedImage.Stop();
                break;
            case ItemDescription.ItemType.SFX:
                ((SfxEditorViewModel)closedEditor).SfxPlayerPanel.Stop();
                break;
        }
        ShowTabsPanel = Tabs.Any();

        if (closedEditor.Description.UnsavedChanges)
        {
            ButtonResult result = await MainWindow.Window.ShowMessageBoxAsync(Strings.SaveClosedItemTitle,
                Strings.SaveClosedItemMessage,
                ButtonEnum.YesNoCancel, Icon.Question, _log);
            switch (result)
            {
                case ButtonResult.Yes:
                    MainWindow.SaveItems([closedEditor.Description]);
                    break;
                case ButtonResult.No:
                    break;
                default:
                case ButtonResult.Cancel:
                    EditorViewModel editor = CreateTab(closedEditor.Description);
                    editor.Description.UnsavedChanges = true;
                    Tabs.Add(editor);
                    return;
            }
        }
        else if (closedEditor.Description.Renamed)
        {
            // if we haven't saved but we have been renamed, we need to commit that rename anyway
            MainWindow.SaveItems([closedEditor.Description]);
        }

        _project.ItemShims.First(s => s.Name == closedEditor.Description.Item.Name).Item = null;
    }
}
