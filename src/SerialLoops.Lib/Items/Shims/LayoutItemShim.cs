using System.Linq;

namespace SerialLoops.Lib.Items.Shims;

public class LayoutItemShim : ItemShim
{
    public int[] ArchiveIndices { get; set; }

    public LayoutItemShim()
    {
    }

    public LayoutItemShim(LayoutItem layout) : base(layout)
    {
        ArchiveIndices =
        [
            layout.Layout.Index,
            .. layout.GraphicsFiles.Select(g => g.Index),
        ];
    }
}
