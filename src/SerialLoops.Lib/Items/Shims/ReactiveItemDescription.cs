using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace SerialLoops.Lib.Items.Shims;

public class ReactiveItemDescription(ItemDescription item, Project project) : ReactiveObject
{
    public ItemDescription Item { get; } = item;
    public bool Renamed { get; set; } = false;

    public string DisplayName
    {
        get => Item.DisplayName;
        set
        {
            Item.DisplayName = value;
            project.ItemNames[Item.Name] = DisplayName;
            Renamed = true;
            this.RaisePropertyChanged();
        }
    }

    [Reactive]
    public bool UnsavedChanges { get; set; }
}
