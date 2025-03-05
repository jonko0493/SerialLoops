using ReactiveUI;

namespace SerialLoops.Lib.Items;

public class ReactiveItemDescription(ItemDescription item) : ReactiveObject
{
    public ItemDescription Item { get; } = item;

    public string DisplayName
    {
        get => Item.DisplayName;
        set
        {
            Item.DisplayName = value;
            this.RaisePropertyChanged();
        }
    }
}
