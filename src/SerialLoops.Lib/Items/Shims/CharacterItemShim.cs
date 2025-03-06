using HaruhiChokuretsuLib.Archive.Event;

namespace SerialLoops.Lib.Items.Shims;

public class CharacterItemShim : ItemShim
{
    public Speaker Character { get; set; }
    public string CharacterName { get; set; }

    public CharacterItemShim()
    {
    }

    public CharacterItemShim(CharacterItem character) : base(character)
    {
        Character = character.MessageInfo.Character;
        CharacterName= character.NameplateProperties.Name;
    }
}
