using HaruhiChokuretsuLib.Archive.Event;

namespace SerialLoops.Lib.Items.Shims;

public class TopicItemShim : ItemShim
{
    public Topic TopicEntry { get; set; }
    public Topic HiddenMainTopic { get; set; }
    public (string ScriptName, ScriptCommandInvocation command)[] ScriptUses { get; set; }

    public TopicItemShim()
    {
    }

    public TopicItemShim(TopicItem topic) : base(topic)
    {
        TopicEntry = topic.TopicEntry;
        HiddenMainTopic = topic.HiddenMainTopic;
        ScriptUses = topic.ScriptUses;
    }
}
