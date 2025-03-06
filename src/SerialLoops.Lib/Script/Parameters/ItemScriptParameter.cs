using System.Linq;
using HaruhiChokuretsuLib.Archive.Event;
using LiteDB;
using SerialLoops.Lib.Items;

namespace SerialLoops.Lib.Script.Parameters;

public class ItemScriptParameter(string name, short itemIndex) : ScriptParameter(name, ParameterType.ITEM)
{
    public short ItemIndex { get; set; } = itemIndex;
    public override short[] GetValues(object obj = null) => [ItemIndex];

    public override string GetValueString(Project project)
    {
        using LiteDatabase db = new(project.DbFile);
        var itemsCol = db.GetCollection<ItemDescription>(Project.ItemsCollectionName);
        return itemsCol.FindOne(i => i.Type == ItemDescription.ItemType.Item &&
                                        ((ItemItem)i).ItemIndex == ItemIndex)?.DisplayName;
    }

    public override ItemScriptParameter Clone(Project project, EventFile eventFile)
    {
        return new(Name, ItemIndex);
    }
}
