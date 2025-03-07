using System.Linq;

namespace SerialLoops.Lib.Items.Shims;

public class ChibiItemShim : ItemShim
{
    public int TopScreenIndex { get; set; }
    public int ChibiIndex { get; set; }
    public int[] ArchiveIndices { get; set; }
    public int FirstFrameWidth { get; set; }

    public ChibiItemShim()
    {
    }

    public ChibiItemShim(ChibiItem chibi) : base(chibi)
    {
        TopScreenIndex = chibi.TopScreenIndex;
        ChibiIndex = chibi.ChibiIndex;
        ArchiveIndices =
        [
            .. chibi.Chibi.ChibiEntries.Select(c => c.Texture),
            .. chibi.Chibi.ChibiEntries.Select(c => c.Animation),
        ];
        FirstFrameWidth = chibi.ChibiAnimations.First().Value.ElementAt(0).Frame.Width;
    }
}
