using System.Linq;
using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib.Script.Parameters;
using SerialLoops.Lib.Util;

namespace SerialLoops.Lib.Items.Shims;

public class ScriptItemShim : ItemShim
{
    public short StartReadFlag { get; set; }
    public short SfxGroupIndex { get; set; }
    public int EventIndex { get; set; }
    public string DialogueLines { get; set; }
    public Speaker[] Speakers { get; set; }
    public string[] SpeakerStrings { get; set; }
    public string Conditionals { get; set; }
    public short[] FlagIds { get; set; }
    public string[] FlagNames { get; set; }
    public string FlagNicknames { get; set; }

    public ScriptItemShim()
    {
    }

    public ScriptItemShim(ScriptItem script, Project project, ILogger log) : base(script)
    {
        // var commands = script.GetScriptCommandTree(project, log);

        StartReadFlag = script.StartReadFlag;
        SfxGroupIndex = script.SfxGroupIndex;
        EventIndex = script.Event.Index;
        DialogueLines = string.Join(" ", script.Event.DialogueSection.Objects.Select(d => d.Text.GetSubstitutedString(project)));
        Speakers = script.Event.DialogueSection.Objects.Select(d => d.Speaker).Distinct().ToArray();
        SpeakerStrings = Speakers.Select(s => s.ToString()).ToArray();
        Conditionals = string.Join(' ', script.Event.ConditionalsSection.Objects.Where(c => !string.IsNullOrEmpty(c))
            .Select(c => c).Distinct());
        // FlagIds = commands.SelectMany(s => s.Value.SelectMany(c => c.Parameters
        //     .Where(p => p.Type == ScriptParameter.ParameterType.FLAG)
        //     .Cast<FlagScriptParameter>().Select(f => f.Id))).ToArray();
        // FlagNames = FlagIds.Select(f => new FlagScriptParameter("Flag", f).FlagName).ToArray();
        // FlagNicknames = string.Join(' ', FlagIds.Select(f => Flags.GetFlagNickname(f, project)));
    }
}
