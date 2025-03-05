using System.Collections.Generic;
using System.Linq;
using LiteDB;

namespace SerialLoops.Lib.Items;

public class ItemShim(string name, string displayName, ItemDescription.ItemType type, bool canRename)
{
    public string Name { get; set; } = name;
    public string DisplayName { get; set; } = displayName;
    public ItemDescription.ItemType Type { get; set; } = type;
    public bool CanRename { get; set; } = canRename;

    public List<ReactiveItemShim> GetReferencesTo(Project project)
    {
        ItemDescription item = null;
        using (LiteDatabase db = new(project.DbFile))
        {
            var itemsCol = db.GetCollection<ItemDescription>(Project.ItemsTableName);
            item = itemsCol.FindById(Name);
        }
        return item?.GetReferencesTo(project).Select(i => new ReactiveItemShim(new(i.Name, i.DisplayName, i.Type, i.CanRename))).ToList();
    }
}
