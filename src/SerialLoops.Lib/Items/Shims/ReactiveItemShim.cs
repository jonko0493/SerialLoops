using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace SerialLoops.Lib.Items.Shims;

public class ReactiveItemShim(ItemShim shim, Project project) : ReactiveObject
{
    public ItemShim Shim { get; } = shim;
    [Reactive]
    public ReactiveItemDescription Item { get; set; }

    public bool CommitRename { get; set; } = true;

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
            else if (CommitRename)
            {
                Shim.CommitRename(project);
            }
            this.RaisePropertyChanged();
        }
    }

    public ItemDescription.ItemType Type => Shim.Type;

    public bool CanRename => Shim.CanRename;
}
