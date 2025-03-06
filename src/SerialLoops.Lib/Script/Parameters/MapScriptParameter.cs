using HaruhiChokuretsuLib.Archive.Event;
using LiteDB;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Items.Shims;

namespace SerialLoops.Lib.Script.Parameters;

public class MapScriptParameter : ScriptParameter
{
    public MapItemShim Map { get; set; }
    public override short[] GetValues(object obj = null) => [(short)Map.MapIndex];
    public MapItem GetMap(ILiteCollection<ItemDescription> itemsCol) => (MapItem)Map?.GetItem(itemsCol);

    public override string GetValueString(Project project)
    {
        return Map?.DisplayName;
    }

    public MapScriptParameter(string name, MapItemShim map) : base(name, ParameterType.MAP)
    {
        Map = map;
    }

    public override MapScriptParameter Clone(Project project, EventFile eventFile)
    {
        return new(Name, Map);
    }
}
