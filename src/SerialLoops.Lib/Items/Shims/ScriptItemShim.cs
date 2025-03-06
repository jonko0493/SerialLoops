using System.Linq;
using HaruhiChokuretsuLib.Archive.Event;
using SerialLoops.Lib.Util;

namespace SerialLoops.Lib.Items.Shims;

public class ScriptItemShim : ItemShim
{
    public short StartReadFlag { get; set; }
    public short SfxGroupIndex { get; set; }
    public int EventIndex { get; set; }
    public string DialogueLines { get; set; }
    public Speaker[] Speakers { get; set; }
    public string[] Conditionals { get; set; }

    public ScriptItemShim()
    {
    }

    public ScriptItemShim(ScriptItem script, Project project) : base(script)
    {
        StartReadFlag = script.StartReadFlag;
        SfxGroupIndex = script.SfxGroupIndex;
        EventIndex = script.Event.Index;
        DialogueLines = string.Join(" ", script.Event.DialogueSection.Objects.Select(d => d.Text.GetSubstitutedString(project)));
        Speakers = script.Event.DialogueSection.Objects.Select(d => d.Speaker).Distinct().ToArray();
        Conditionals = script.Event.ConditionalsSection.Objects.Select(c => c).Distinct().ToArray();
    }
}
