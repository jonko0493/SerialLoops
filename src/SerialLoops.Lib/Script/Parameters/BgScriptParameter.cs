using HaruhiChokuretsuLib.Archive.Event;
using LiteDB;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Items.Shims;

namespace SerialLoops.Lib.Script.Parameters;

public class BgScriptParameter : ScriptParameter
{
    public BackgroundItemShim Background { get; set; }
    public bool Kinetic { get; set; }
    public override short[] GetValues(object obj = null) => [(short)(Background?.Id ?? 0)];
    public BackgroundItem GetBackground(ILiteCollection<ItemDescription> itemsCol) => (BackgroundItem)Background?.GetItem(itemsCol);

    public override string GetValueString(Project project)
    {
        return Background?.DisplayName;
    }

    public BgScriptParameter(string name, BackgroundItemShim background, bool kinetic) : base(name, ParameterType.BG)
    {
        Background = background;
        Kinetic = kinetic;
    }

    public override BgScriptParameter Clone(Project project, EventFile eventFile)
    {
        return new(Name, Background, Kinetic);
    }
}
