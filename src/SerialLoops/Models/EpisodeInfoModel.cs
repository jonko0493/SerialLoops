using System;
using HaruhiChokuretsuLib.Archive.Graphics;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Util;
using SkiaSharp;

namespace SerialLoops.Models;

public class EpisodeInfoModel : ReactiveObject
{
    public Action TitleChangedAction { private get; set; }

    [Reactive]
    public int Number { get; set; }
    public int TitleIndex { get; }

    private string _title;
    public string Title
    {
        get => _title;
        set
        {
            this.RaiseAndSetIfChanged(ref _title, value);
            TitleChangedAction?.Invoke();
        }
    }
    public int TickerTapeIndex { get; }
    [Reactive]
    public string TickerTape { get; set; }
    public string HeaderGraphicName { get; }
    [Reactive]
    public SKBitmap HeaderGraphic { get; set; }

    public EpisodeInfoModel(int number, int titleIndex, int tickerTapeIndex, string headerGraphicName, Project project)
    {
        Number = number;
        TitleIndex = titleIndex;
        TickerTapeIndex = tickerTapeIndex;
        HeaderGraphicName = headerGraphicName;
        Title = project.UiText.Messages[titleIndex].GetSubstitutedString(project);
        TickerTape = project.UiText.Messages[tickerTapeIndex].GetSubstitutedString(project);
        HeaderGraphic = PlaceItem.Unscramble(project.Grp.GetFileByName(headerGraphicName).GetImage(transparentIndex: 0));
    }

    public EpisodeInfoModel(int number, string title, string headerGraphicName, Project project)
    {
        Number = number;
        Title = title;
        HeaderGraphicName = headerGraphicName;
        HeaderGraphic = PlaceItem.Unscramble(project.Grp.GetFileByName(headerGraphicName).GetImage(transparentIndex: 0));
    }

    public override string ToString() => Number <= 5 ? $"Episode {Number}" : "Epilogue";
}
