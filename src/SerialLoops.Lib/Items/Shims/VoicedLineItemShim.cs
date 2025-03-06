namespace SerialLoops.Lib.Items.Shims;

public class VoicedLineItemShim : ItemShim
{
    public int Index { get; set; }

    public VoicedLineItemShim()
    {
    }

    public VoicedLineItemShim(VoicedLineItem vce) : base(vce)
    {
        Index = vce.Index;
    }
}
