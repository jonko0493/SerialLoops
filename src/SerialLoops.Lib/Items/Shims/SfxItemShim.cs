using System.Collections.Generic;

namespace SerialLoops.Lib.Items.Shims;

public class SfxItemShim : ItemShim
{
    public short Index { get; set; }
    public List<string> AssociatedGroups { get; set; }

    public SfxItemShim()
    {
    }

    public SfxItemShim(SfxItem sfx) : base(sfx)
    {
        Index = sfx.Index;
        AssociatedGroups = sfx.AssociatedGroups;
    }
}
