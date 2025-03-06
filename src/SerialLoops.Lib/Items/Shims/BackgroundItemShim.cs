using HaruhiChokuretsuLib.Archive.Data;

namespace SerialLoops.Lib.Items.Shims;

public class BackgroundItemShim : ItemShim
{
    public int Id { get; set; }
    public BgType BackgroundType { get; set; }
    public int Flag { get; set; }

    public BackgroundItemShim()
    {
    }

    public BackgroundItemShim(BackgroundItem bg) : base(bg)
    {
        Id = bg.Id;
        BackgroundType = bg.BackgroundType;
        Flag = bg.Flag;
    }
}
