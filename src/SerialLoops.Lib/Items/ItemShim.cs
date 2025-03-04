using System.Collections.Generic;

namespace SerialLoops.Lib.Items;

public class ItemShim(string name, string displayName, ItemDescription.ItemType type, bool canRename)
{
    public string Name { get; set; } = name;
    public string DisplayName { get; set; } = displayName;
    public ItemDescription.ItemType Type { get; set; } = type;
    public bool CanRename { get; set; } = canRename;

    public List<ItemShim> GetReferencesTo(Project project)
    {
        return [];
    }
}
