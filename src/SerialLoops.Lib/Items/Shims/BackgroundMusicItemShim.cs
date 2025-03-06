namespace SerialLoops.Lib.Items.Shims;

public class BackgroundMusicItemShim : ItemShim
{
    public int Index { get; set; }
    public short? Flag { get; set; }

    public BackgroundMusicItemShim()
    {
    }

    public BackgroundMusicItemShim(BackgroundMusicItem bgm) : base(bgm)
    {
        Index = bgm.Index;
        Flag = bgm.Flag;
    }
}
