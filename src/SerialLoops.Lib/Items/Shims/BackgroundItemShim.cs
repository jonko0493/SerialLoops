using HaruhiChokuretsuLib.Archive.Data;
using SerialLoops.Lib.Script.Parameters;

namespace SerialLoops.Lib.Items.Shims;

public class BackgroundItemShim : ItemShim
{
    public int Id { get; set; }
    public BgType BackgroundType { get; set; }
    public int Flag { get; set; }
    public string FlagName { get; set; }
    public string FlagNickname { get; set; }
    public int[] ArchiveIndices { get; set; }

    public BackgroundItemShim()
    {
    }

    public BackgroundItemShim(BackgroundItem bg) : base(bg)
    {
        Id = bg.Id;
        BackgroundType = bg.BackgroundType;
        Flag = bg.Flag;
        FlagName = new FlagScriptParameter("Flag", (short)Flag).FlagName;
        // FlagNickname = Flags.GetFlagNickname(Flag, project);
        ArchiveIndices = [bg.Graphic1.Index, bg.Graphic2?.Index ?? -1];
    }
}
