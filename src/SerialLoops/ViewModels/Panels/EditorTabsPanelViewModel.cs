using System.Collections.ObjectModel;
using System.IO;
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
using SerialLoops.Lib.SaveFile;
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
                return new BackgroundEditorViewModel((BackgroundItem)item.Item, MainWindow, _project, _log);
            case ItemDescription.ItemType.BGM:
                return new BackgroundMusicEditorViewModel((BackgroundMusicItem)item.Item, MainWindow, _project, _log);
            case ItemDescription.ItemType.Character:
                return new CharacterEditorViewModel((CharacterItem)item.Item, MainWindow, _log);
            case ItemDescription.ItemType.Character_Sprite:
                return new CharacterSpriteEditorViewModel((CharacterSpriteItem)item.Item, MainWindow, _log);
            case ItemDescription.ItemType.Chess_Puzzle:
                return new ChessPuzzleEditorViewModel((ChessPuzzleItem)item.Item, MainWindow, _log);
            case ItemDescription.ItemType.Group_Selection:
                return new GroupSelectionEditorViewModel((GroupSelectionItem)item.Item, MainWindow, _log);
            case ItemDescription.ItemType.Item:
                return new ItemEditorViewModel((ItemItem)item.Item, MainWindow, _log);
            case ItemDescription.ItemType.Layout:
                return new LayoutEditorViewModel((LayoutItem)item.Item, MainWindow, _log);
            case ItemDescription.ItemType.Map:
                return new MapEditorViewModel((MapItem)item.Item, MainWindow, _log);
            case ItemDescription.ItemType.Place:
                return new PlaceEditorViewModel((PlaceItem)item.Item, MainWindow, _log);
            case ItemDescription.ItemType.Puzzle:
                return new PuzzleEditorViewModel((PuzzleItem)item.Item, MainWindow, _log);
            case ItemDescription.ItemType.Scenario:
                return new ScenarioEditorViewModel((ScenarioItem)item.Item, MainWindow, _log);
            case ItemDescription.ItemType.Script:
                return new ScriptEditorViewModel((ScriptItem)item.Item, MainWindow, _log);
            case ItemDescription.ItemType.SFX:
                return new SfxEditorViewModel((SfxItem)item.Item, MainWindow, _log);
            case ItemDescription.ItemType.System_Texture:
                return new SystemTextureEditorViewModel((SystemTextureItem)item.Item, MainWindow, _project, _log);
            case ItemDescription.ItemType.Topic:
                return new TopicEditorViewModel((TopicItem)item.Item, MainWindow, _log);
            case ItemDescription.ItemType.Voice:
                return new VoicedLineEditorViewModel((VoicedLineItem)item.Item, MainWindow, _log);
            case ItemDescription.ItemType.Save:
                return new SaveEditorViewModel((SaveItem)item.Item, MainWindow, _log);
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
                    OpenTab(closedEditor.Description);
                    break;
            }
        }

        if (closedEditor.Description.Item.Type == ItemDescription.ItemType.BGM)
        {
            ((BackgroundMusicEditorViewModel)closedEditor).BgmPlayer.Stop();
        }
        else if (closedEditor.Description.Item.Type == ItemDescription.ItemType.Character_Sprite)
        {
            ((CharacterSpriteEditorViewModel)closedEditor).AnimatedImage.Stop();
        }
        else if (closedEditor.Description.Item.Type == ItemDescription.ItemType.SFX)
        {
            ((SfxEditorViewModel)closedEditor).SfxPlayerPanel.Stop();
        }
        ShowTabsPanel = Tabs.Any();
    }

    public void OnTabMiddleClicked()
    {
        Tabs.Remove(SelectedTab);
        ShowTabsPanel = Tabs.Any();
    }
}
