using System.IO;
using Avalonia.Platform;
using HaruhiChokuretsuLib.Util;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Items.Shims;
using SkiaSharp;

namespace SerialLoops.ViewModels.Editors;

public class PlaceEditorViewModel : EditorViewModel
{
    private PlaceItem _place;
    private SKTypeface _msGothicHaruhi;

    [Reactive]
    public SKBitmap Preview { get; set; }

    public string PlaceName
    {
        get => _place.PlaceName;
        set
        {
            _place.PlaceName = value;
            this.RaisePropertyChanged();
            Preview = _place.GetNewPlaceGraphic(_msGothicHaruhi);
            _place.PlaceGraphic.SetImage(Preview);
            Description.UnsavedChanges = true;
        }
    }

    public PlaceEditorViewModel(ReactiveItemDescription item, MainWindowViewModel window, ILogger log) : base(item, window, log)
    {
        _place = (PlaceItem)item.Item;
        if (string.IsNullOrEmpty(_place.PlaceName))
        {
            _place.PlaceName = _place.DisplayName[4..];
        }
        Preview = _place.GetPreview(window.OpenProject);

        using Stream typefaceStream = AssetLoader.Open(new("avares://SerialLoops/Assets/Graphics/MS-Gothic-Haruhi.ttf"));
        _msGothicHaruhi = SKTypeface.FromStream(typefaceStream);
        if (!PlaceItem.CustomFontMapper.HasFont())
        {
            PlaceItem.CustomFontMapper.AddFont(_msGothicHaruhi);
        }
    }
}
