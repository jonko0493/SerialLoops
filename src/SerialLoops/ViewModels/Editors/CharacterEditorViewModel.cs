using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using DynamicData;
using HaruhiChokuretsuLib.Util;
using LiteDB;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Items.Shims;
using SerialLoops.Utility;
using SerialLoops.ViewModels.Panels;
using SkiaSharp;

namespace SerialLoops.ViewModels.Editors;

public class CharacterEditorViewModel : EditorViewModel
{
    private readonly CharacterItem _character;

    public EditorTabsPanelViewModel Tabs { get; }

    public string CharacterName
    {
        get => _character.NameplateProperties.Name;
        set
        {
            _character.NameplateProperties.Name = value;
            this.RaisePropertyChanged();
            UpdateNameplateBitmap();
            Description.UnsavedChanges = true;
        }
    }

    public SKColor TextColor
    {
        get => _character.NameplateProperties.NameColor;
        set
        {
            _character.NameplateProperties.NameColor = value;
            this.RaisePropertyChanged();
            UpdateNameplateBitmap();
            Description.UnsavedChanges = true;
        }
    }

    public SKColor PlateColor
    {
        get => _character.NameplateProperties.PlateColor;
        set
        {
            _character.NameplateProperties.PlateColor = value;
            this.RaisePropertyChanged();
            UpdateNameplateBitmap();
            Description.UnsavedChanges = true;
        }
    }

    public SKColor OutlineColor
    {
        get => _character.NameplateProperties.OutlineColor;
        set
        {
            _character.NameplateProperties.OutlineColor = value;
            this.RaisePropertyChanged();
            UpdateNameplateBitmap();
            Description.UnsavedChanges = true;
        }
    }

    public bool HasOutline
    {
        get => _character.NameplateProperties.HasOutline;
        set
        {
            _character.NameplateProperties.HasOutline = value;
            this.RaisePropertyChanged();
            UpdateNameplateBitmap();
            Description.UnsavedChanges = true;
        }
    }

    public ObservableCollection<SfxItem> Sfxs { get; } = [];
    private SfxItem _voiceFont;
    public SfxItem VoiceFont
    {
        get => _voiceFont;
        set
        {
            this.RaiseAndSetIfChanged(ref _voiceFont, value);
            _character.MessageInfo.VoiceFont = _voiceFont.Index;
            Description.UnsavedChanges = true;
        }
    }

    public short TextTimer
    {
        get => _character.MessageInfo.TextTimer;
        set
        {
            _character.MessageInfo.TextTimer = value;
            this.RaisePropertyChanged();
            Description.UnsavedChanges = true;
        }
    }

    [Reactive]
    public SKBitmap NameplateBitmap { get; set; }

    private SKBitmap _blankNameplateBitmap;
    private SKBitmap _blankNameplateBaseArrowBitmap;

    public CharacterColorPalette ColorPalette { get; } = new();

    public CharacterEditorViewModel(ReactiveItemDescription item, MainWindowViewModel window, ILogger log) : base(item, window, log)
    {
        using LiteDatabase db = new(window.OpenProject.DbFile);
        var itemsCol = db.GetCollection<ItemDescription>(Project.ItemsCollectionName);
        var sfxCol = db.GetCollection<SfxItemShim>(nameof(SfxItem));

        _character = (CharacterItem)item.Item;
        Tabs = window.EditorTabs;

        Sfxs.AddRange(sfxCol.FindAll().Select(s => s.GetItem(itemsCol)).Cast<SfxItem>());
        _voiceFont = Sfxs.FirstOrDefault(s => s.Index == _character.MessageInfo.VoiceFont);

        using Stream blankNameplateStream = AssetLoader.Open(new("avares://SerialLoops/Assets/Graphics/BlankNameplate.png"));
        _blankNameplateBitmap = SKBitmap.Decode(blankNameplateStream);
        using Stream blankNameplateBaseArrowStream = AssetLoader.Open(new("avares://SerialLoops/Assets/Graphics/BlankNameplateBaseArrow.png"));
        _blankNameplateBaseArrowBitmap = SKBitmap.Decode(blankNameplateBaseArrowStream);

        NameplateBitmap = new(64, 16);
        window.OpenProject.NameplateBitmap.ExtractSubset(NameplateBitmap,
            new(0, 16 * ((int)_character.MessageInfo.Character - 1), 64, 16 * (int)_character.MessageInfo.Character));
    }

    private void UpdateNameplateBitmap()
    {
        NameplateBitmap =
            _character.GetNewNameplate(_blankNameplateBitmap, _blankNameplateBaseArrowBitmap, Window.OpenProject);
    }
}

public class CharacterColorPalette : IColorPalette
{
    public Color GetColor(int colorIndex, int shadeIndex) =>
        CharacterItem.BuiltInColors[CharacterItem.BuiltInColors.Keys.ElementAt(colorIndex)].ToAvalonia();

    public int ColorCount => CharacterItem.BuiltInColors.Count;
    public int ShadeCount => 1;
}
