using HaruhiChokuretsuLib.Archive.Event;

namespace SerialLoops.Lib.Items.Shims;

public class CharacterItemShim : ItemShim
{
    public Speaker Character { get; set; }
    public string CharacterName { get; set; }
    public short VoiceFont { get; set; }
    public int MessInfoIndex { get; set; }

    public CharacterItemShim()
    {
    }

    public CharacterItemShim(CharacterItem character, Project project) : base(character)
    {
        Character = character.MessageInfo.Character;
        CharacterName= character.NameplateProperties.Name;
        VoiceFont = character.MessageInfo.VoiceFont;
        MessInfoIndex = project.MessInfo.MessageInfos.IndexOf(character.MessageInfo);
    }
}
