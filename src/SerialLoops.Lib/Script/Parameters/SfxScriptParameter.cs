using HaruhiChokuretsuLib.Archive.Event;
using LiteDB;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Items.Shims;

namespace SerialLoops.Lib.Script.Parameters;

public class SfxScriptParameter : ScriptParameter
{
    public SfxItemShim Sfx { get; set; }
    public override short[] GetValues(object obj = null) => [Sfx.Index];
    public SfxItem GetSfx(ILiteCollection<ItemDescription> itemsCol) => (SfxItem)Sfx?.GetItem(itemsCol);

    public override string GetValueString(Project project)
    {
        return Sfx?.DisplayName;
    }

    public SfxScriptParameter(string name, SfxItemShim sfx) : base(name, ParameterType.SFX)
    {
        Sfx = sfx;
    }

    public override SfxScriptParameter Clone(Project project, EventFile eventFile)
    {
        return new(Name, Sfx);
    }
}
