using System.Collections.Generic;
using System.Linq;
using SerialLoops.Lib.Util;

namespace SerialLoops.Lib.Items.Shims;

public class ScriptItemShim : ItemShim
{
    public short StartReadFlag { get; set; }
    public short SfxGroupIndex { get; set; }
    public int EventIndex { get; set; }
    public List<string> DialogueLines { get; set; }

    public ScriptItemShim()
    {
    }

    public ScriptItemShim(ScriptItem script, Project project) : base(script)
    {
        StartReadFlag = script.StartReadFlag;
        SfxGroupIndex = script.SfxGroupIndex;
        EventIndex = script.Event.Index;
        DialogueLines = script.Event.DialogueLines.Select(d => d.Text.GetSubstitutedString(project)).ToList();
    }
}
