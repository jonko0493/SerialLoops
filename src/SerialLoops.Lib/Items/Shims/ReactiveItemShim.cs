using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace SerialLoops.Lib.Items.Shims;

public class ReactiveItemShim(ItemShim shim) : ReactiveObject
{
    public ItemShim Shim { get; } = shim;
    [Reactive]
    public ReactiveItemDescription Item { get; set; }

    public string Name => Shim.Name;

    public string DisplayName
    {
        get => Shim.DisplayName;
        set
        {
            Shim.DisplayName = value;
            if (Item is not null)
            {
                Item.DisplayName = value;
            }
            this.RaisePropertyChanged();
        }
    }

    public ItemDescription.ItemType Type => Shim.Type;

    public bool CanRename => Shim.CanRename;
}
