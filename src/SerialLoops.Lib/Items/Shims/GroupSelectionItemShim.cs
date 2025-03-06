using System.Linq;

namespace SerialLoops.Lib.Items.Shims;

public class GroupSelectionItemShim : ItemShim
{
    public int Index { get; set; }
    public string TextSearch { get; set; }
    public short[] RouteFlags { get; set; }
    public short[] ScriptIndices { get; set; }

    public GroupSelectionItemShim()
    {
    }

    public GroupSelectionItemShim(GroupSelectionItem selection) : base(selection)
    {
        Index = selection.Index;
        TextSearch = selection.TextSearch;
        RouteFlags = selection.Selection.Activities.Where(a => a is not null)
            .SelectMany(a => a.Routes.Select(r => r.Flag)).ToArray();
        ScriptIndices = selection.Selection.Activities.Where(a => a is not null)
            .SelectMany(a => a.Routes.Select(r => r.ScriptIndex)).ToArray();
    }
}
