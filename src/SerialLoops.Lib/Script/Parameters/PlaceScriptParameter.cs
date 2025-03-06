using HaruhiChokuretsuLib.Archive.Event;
using LiteDB;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Items.Shims;

namespace SerialLoops.Lib.Script.Parameters;

public class PlaceScriptParameter(string name, PlaceItemShim place) : ScriptParameter(name, ParameterType.PLACE)
{
    public PlaceItemShim Place { get; set; } = place;
    public override short[] GetValues(object obj = null) => [(short)Place.Index];
    public PlaceItem GetPlace(ILiteCollection<ItemDescription> itemsCol) => (PlaceItem)Place?.GetItem(itemsCol);

    public override string GetValueString(Project project)
    {
        return Place?.DisplayName;
    }

    public override PlaceScriptParameter Clone(Project project, EventFile eventFile)
    {
        return new(Name, Place);
    }
}
