using HaruhiChokuretsuLib.Archive.Event;
using LiteDB;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Items.Shims;

namespace SerialLoops.Lib.Script.Parameters;

public class VoicedLineScriptParameter : ScriptParameter
{
    public VoicedLineItemShim VoiceLine { get; set; }
    public override short[] GetValues(object obj = null) => [(short)(VoiceLine?.Index ?? 0)];
    public VoicedLineItem GetVoicedLine(ILiteCollection<ItemDescription> itemsCol) => (VoicedLineItem)VoiceLine?.GetItem(itemsCol);

    public override string GetValueString(Project project)
    {
        return VoiceLine?.DisplayName;
    }

    public VoicedLineScriptParameter(string name, VoicedLineItemShim vce) : base(name, ParameterType.VOICE_LINE)
    {
        VoiceLine = vce;
    }

    public override VoicedLineScriptParameter Clone(Project project, EventFile eventFile)
    {
        return new(Name, VoiceLine);
    }
}
