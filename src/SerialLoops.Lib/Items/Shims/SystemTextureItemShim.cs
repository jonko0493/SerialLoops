namespace SerialLoops.Lib.Items.Shims;

public class SystemTextureItemShim : ItemShim
{
    public int GraphicIndex { get; set; }

    public SystemTextureItemShim()
    {
    }

    public SystemTextureItemShim(SystemTextureItem systex) : base(systex)
    {
        GraphicIndex = systex.SysTex.GrpIndex;
    }
}
