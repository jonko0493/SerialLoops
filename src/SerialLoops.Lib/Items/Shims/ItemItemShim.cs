namespace SerialLoops.Lib.Items.Shims;

public class ItemItemShim : ItemShim
{
    public int ItemIndex { get; set; }
    public int GraphicIndex { get; set; }

    public ItemItemShim()
    {
    }

    public ItemItemShim(ItemItem item) : base(item)
    {
        ItemIndex = item.ItemIndex;
        GraphicIndex = item.ItemGraphic.Index;
    }
}
