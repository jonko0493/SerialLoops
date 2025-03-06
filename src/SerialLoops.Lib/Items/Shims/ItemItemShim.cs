namespace SerialLoops.Lib.Items.Shims;

public class ItemItemShim : ItemShim
{
    public int ItemIndex { get; set; }

    public ItemItemShim()
    {
    }

    public ItemItemShim(ItemItem item) : base(item)
    {
        ItemIndex = item.ItemIndex;
    }
}
