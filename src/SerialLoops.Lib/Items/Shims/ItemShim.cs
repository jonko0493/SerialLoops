using System.Collections.Generic;
using System.Linq;
using LiteDB;

namespace SerialLoops.Lib.Items.Shims;

public class ItemShim
{
    [BsonId]
    public string Name { get; set; }
    public string DisplayName { get; set; }
    public ItemDescription.ItemType Type { get; set; }
    public bool CanRename { get; set; }

    public ItemShim()
    {
    }

    public ItemShim(ItemDescription item)
    {
        Name = item.Name;
        DisplayName = item.DisplayName;
        Type = item.Type;
        CanRename = item.CanRename;
    }

    public ItemDescription GetItem(ILiteCollection<ItemDescription> itemsCol)
    {
        return itemsCol.FindById(Name);
    }

    public List<ReactiveItemShim> GetReferencesTo(Project project, LiteDatabase db = null)
    {
        bool dbWasNull = db is null;
        ItemDescription item = null;
        if (dbWasNull)
        {
            db = new(project.DbFile);
        }
        var itemsCol = db.GetCollection<ItemDescription>(Project.ItemsCollectionName);
        item = itemsCol.FindById(Name);
        List<ReactiveItemShim> references = item?.GetReferencesTo(project, db)?.Select(s => new ReactiveItemShim(s, project)).ToList();
        if (dbWasNull)
        {
            db.Dispose();
        }
        return references;
    }

    public void CommitRename(Project project)
    {
        using LiteDatabase db = new(project.DbFile);
        var itemsCol = db.GetCollection<ItemDescription>(Project.ItemsCollectionName);
        ItemDescription item = itemsCol.FindById(Name);
        item.DisplayName = DisplayName;
        itemsCol.Update(item.Name, item);
    }
}
