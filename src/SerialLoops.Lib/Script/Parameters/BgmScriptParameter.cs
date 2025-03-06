using HaruhiChokuretsuLib.Archive.Event;
using LiteDB;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Items.Shims;

namespace SerialLoops.Lib.Script.Parameters;

public class BgmScriptParameter : ScriptParameter
{
    public BackgroundMusicItemShim Bgm { get; set; }
    public override short[] GetValues(object obj = null) => [(short)Bgm.Index];
    public BackgroundMusicItem GetBgm(ILiteCollection<ItemDescription> itemsCol) => (BackgroundMusicItem)Bgm?.GetItem(itemsCol);

    public override string GetValueString(Project project)
    {
        return Bgm?.DisplayName;
    }

    public BgmScriptParameter(string name, BackgroundMusicItemShim bgm) : base(name, ParameterType.BGM)
    {
        Bgm = bgm;
    }

    public override BgmScriptParameter Clone(Project project, EventFile eventFile)
    {
        return new(Name, Bgm);
    }
}
