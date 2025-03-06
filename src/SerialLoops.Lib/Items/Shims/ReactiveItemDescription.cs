using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace SerialLoops.Lib.Items.Shims;

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

    [Reactive]
    public bool UnsavedChanges { get; set; }

    public void Rename(string newName, Project project)
    {
        DisplayName = newName;
        project.ItemNames[Item.Name] = DisplayName;
    }
}
