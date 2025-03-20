using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using Avalonia.Platform;
using HaruhiChokuretsuLib.Util;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Util;
using SerialLoops.Models;
using SerialLoops.Views.Dialogs;
using SkiaSharp;
using Topten.RichTextKit;

namespace SerialLoops.ViewModels.Dialogs;

public class EditEpisodeTitlesDialogViewModel : ViewModelBase
{
    private SKTypeface _msGothicHaruhi;
    public ObservableCollection<EpisodeInfoModel> Episodes { get; }
    [Reactive]
    public EpisodeInfoModel SelectedEpisode { get; set; }

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    public EditEpisodeTitlesDialogViewModel(Project project)
    {
        Episodes =
        [
            new(1, 17, 22, "SYS_ADV_T01DNX", project),
            new(2, 18, 23, "SYS_ADV_T02DNX", project),
            new(3, 19, 24, "SYS_ADV_T03DNX", project),
            new(4, 20, 25, "SYS_ADV_T04DNX", project),
            new(5, 21, 26, "SYS_ADV_T05DNX", project),
            new(6, "That One Day...", "SYS_ADV_T13DNX", project),
        ];

        using Stream typefaceStream = AssetLoader.Open(new("avares://SerialLoops/Assets/Graphics/MS-Gothic-Haruhi.ttf"));
        _msGothicHaruhi = SKTypeface.FromStream(typefaceStream);
        if (!PlaceItem.CustomFontMapper.HasFont())
        {
            PlaceItem.CustomFontMapper.AddFont(_msGothicHaruhi);
        }

        foreach (EpisodeInfoModel episode in Episodes)
        {
            episode.TitleChangedAction = () => episode.HeaderGraphic = GetNewTitleHeaderBitmap(episode.HeaderGraphic, episode.Title, transparent: false);
        }

        SaveCommand = ReactiveCommand.Create<EditEpisodeTitlesDialog>(dialog =>
        {
            foreach (EpisodeInfoModel episode in Episodes)
            {
                if (episode.TitleIndex > 0)
                {
                    project.UiText.Messages[episode.TitleIndex] = episode.Title.GetOriginalString(project);
                    project.UiText.Messages[episode.TickerTapeIndex] = episode.TickerTape.GetOriginalString(project);
                }

                project.Grp.GetFileByName("SYS_ADV_T01DNX").SetImage(PlaceItem.Unscramble(GetNewTitleHeaderBitmap(episode.HeaderGraphic, episode.Title, transparent: false)));
            }
            dialog.Close();
        });
        CancelCommand = ReactiveCommand.Create<EditEpisodeTitlesDialog>(dialog => dialog.Close());
    }

    private SKBitmap GetNewTitleHeaderBitmap(SKBitmap headerGraphic, string title, bool transparent)
    {
        SKBitmap newEpisodeHeader = new(headerGraphic.Width, headerGraphic.Height);
        SKCanvas canvas = new(newEpisodeHeader);
        SKColor bgColor;
        if (!transparent)
        {
            bgColor = new(0, 249, 0);
            canvas.DrawRegion(new(new SKRectI(0, 0, newEpisodeHeader.Width, newEpisodeHeader.Height)), new() { Color = bgColor });
        }
        else
        {
            bgColor = SKColors.Transparent;
        }

        TextBlock episodeHeaderText = new()
        {
            Alignment = TextAlignment.Left,
            FontMapper = new PlaceItem.CustomFontMapper(),
            MaxWidth = newEpisodeHeader.Width - 2,
            MaxHeight = newEpisodeHeader.Height - 12,
        };
        episodeHeaderText.AddText(title, new Style
        {
            TextColor = SKColors.White,
            FontFamily = _msGothicHaruhi.FamilyName,
            FontItalic = true,
            FontSize = 15.0f,
            LetterSpacing = -1.5f,
            HaloColor = SKColors.Black,
            HaloBlur = 0,
            HaloWidth = 4,
        });
        episodeHeaderText.Paint(canvas, new(1, 6), new() { Edging = SKFontEdging.SubpixelAntialias });
        canvas.Flush();

        if (bgColor == SKColors.Transparent)
        {
            return newEpisodeHeader;
        }

        // Antialiasing creates some semitransparent pixels on top of our green background, which causes them to render as green
        // rather than as transparent. To prevent this, we forcibly set them back to the transparent color
        for (int y = 0; y < newEpisodeHeader.Height; y++)
        {
            for (int x = 0; x < newEpisodeHeader.Width; x++)
            {
                if (Helpers.ColorDistance(newEpisodeHeader.GetPixel(x, y), bgColor) < 350)
                {
                    newEpisodeHeader.SetPixel(x, y, bgColor);
                }
            }
        }

        return newEpisodeHeader;
    }
}
