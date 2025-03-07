using SerialLoops.Lib.Items;
using SerialLoops.Lib.Items.Shims;
using SkiaSharp;

namespace SerialLoops.Lib.Script;

public class SpritePositioning
{
    public int X { get; set; }
    public int Layer { get; set; }

    public SKPoint GetSpritePosition(SKBitmap sprite, int verticalOffset)
    {
        return new(X, 192 + verticalOffset - sprite.Height);
    }

    public enum SpritePosition
    {
        LEFT,
        CENTER,
        RIGHT,
    }
}

public struct PositionedSprite
{
    public CharacterSpriteItemShim Sprite { get; set; }
    public SpritePositioning Positioning { get; set; }
    public SKPaint PalEffect { get; set; }
}
