using SerialLoops.Lib.Script.Parameters;

namespace SerialLoops.Lib.Items.Shims;

public class BackgroundMusicItemShim : ItemShim
{
    public int Index { get; set; }
    public short? Flag { get; set; }
    public string FlagName { get; set; }
    public string FlagNickname { get; set; }

    public BackgroundMusicItemShim()
    {
    }

    public BackgroundMusicItemShim(BackgroundMusicItem bgm, Project project) : base(bgm)
    {
        Index = bgm.Index;
        Flag = bgm.Flag;
        if (Flag is not null)
        {
            FlagName = new FlagScriptParameter("Flag", (short)Flag).FlagName;
            // FlagNickname = Flags.GetFlagNickname((int)Flag, project);
        }
    }
}
