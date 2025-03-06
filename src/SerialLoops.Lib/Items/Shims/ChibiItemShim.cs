namespace SerialLoops.Lib.Items.Shims;

public class ChibiItemShim : ItemShim
{
    public int TopScreenIndex { get; set; }
    public int ChibiIndex { get; set; }

    public ChibiItemShim()
    {
    }

    public ChibiItemShim(ChibiItem chibi) : base(chibi)
    {
        TopScreenIndex = chibi.TopScreenIndex;
        ChibiIndex = chibi.ChibiIndex;
    }
}
