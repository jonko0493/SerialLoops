using HaruhiChokuretsuLib.Archive.Event;

namespace SerialLoops.Lib.Items.Shims;

public class CharacterSpriteItemShim : ItemShim
{
    public bool IsLarge { get; set; }
    public Speaker Character { get; set; }
    public int Index { get; set; }
    public int[] ArchiveIndices { get; set; }

    public CharacterSpriteItemShim()
    {
    }

    public CharacterSpriteItemShim(CharacterSpriteItem sprite) : base(sprite)
    {
        IsLarge = sprite.Sprite.IsLarge;
        Character = sprite.Sprite.Character;
        Index = sprite.Index;
        ArchiveIndices =
        [
            sprite.Sprite.TextureIndex1, sprite.Sprite.TextureIndex2,
            sprite.Sprite.TextureIndex3, sprite.Sprite.LayoutIndex,
            sprite.Sprite.EyeAnimationIndex, sprite.Sprite.MouthAnimationIndex,
            sprite.Sprite.EyeTextureIndex, sprite.Sprite.MouthTextureIndex,
        ];
    }
}
