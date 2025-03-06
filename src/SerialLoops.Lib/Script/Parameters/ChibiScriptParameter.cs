using HaruhiChokuretsuLib.Archive.Event;
using LiteDB;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Items.Shims;

namespace SerialLoops.Lib.Script.Parameters;

public class ChibiScriptParameter : ScriptParameter
{
    public ChibiItemShim Chibi { get; set; }
    public override short[] GetValues(object obj = null) => [(short)Chibi.TopScreenIndex];
    public ChibiItem GetChibi(ILiteCollection<ItemDescription> itemsCol) => (ChibiItem)Chibi?.GetItem(itemsCol);

    public override string GetValueString(Project project)
    {
        return Chibi.DisplayName;
    }

    public ChibiScriptParameter(string name, ChibiItemShim chibi) : base(name, ParameterType.CHIBI)
    {
        Chibi = chibi;
    }

    public override ChibiScriptParameter Clone(Project project, EventFile eventFile)
    {
        return new(Name, Chibi);
    }
}
