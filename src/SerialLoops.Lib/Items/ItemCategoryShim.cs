using LiteDB;

namespace SerialLoops.Lib.Items;

public class ItemCategoryShim<T>(T item) where T : ItemDescription
{
    [BsonRef]
    public T Item { get; set; } = item;

    public T GetItem(ILiteCollection<ItemDescription> items)
    {
        return (T)items.FindById(Item.Name);
    }
}
