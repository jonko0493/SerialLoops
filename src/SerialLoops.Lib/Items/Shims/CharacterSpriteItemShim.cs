using HaruhiChokuretsuLib.Archive.Event;

namespace SerialLoops.Lib.Items.Shims;

public class CharacterSpriteItemShim : ItemShim
{
    public bool IsLarge { get; set; }
    public Speaker Character { get; set; }
    public int Index { get; set; }

    public CharacterSpriteItemShim()
    {
    }

    public CharacterSpriteItemShim(CharacterSpriteItem sprite) : base(sprite)
    {
        IsLarge = sprite.Sprite.IsLarge;
        Character = sprite.Sprite.Character;
        Index = sprite.Index;
    }
}
