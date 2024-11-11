using System.Linq;
using HaruhiChokuretsuLib.Archive.Event;
using SerialLoops.Lib.Items;
using SkiaSharp;

namespace SerialLoops.Lib.Util;

public static class GraphicsUtil
{
    public static SKBitmap GetCharacterIcon(Project project, Speaker character)
    {
        SKBitmap bitmap = ((SystemTextureItem)project.Items.First(i => i.Name == "SYSTEX_XTR_PRG_T08")).GetTexture();

        // Crop a 16x16 bitmap portrait
        SKBitmap portrait = new(16, 16);
        if (new[] { Speaker.HARUHI, Speaker.MIKURU, Speaker.NAGATO, Speaker.KOIZUMI, Speaker.TSURUYA }.Contains(character))
        {
            SKCanvas canvas = new(portrait);
            int characterOffset = (int)character + 3;
            if (character == Speaker.TSURUYA)
            {
                characterOffset--;
            }
            canvas.DrawBitmap(bitmap,
                new SKRect((characterOffset * 16) % 128, (characterOffset * 16 / 128) * 16f,
                    ((characterOffset * 16) % 128) + 16f, (characterOffset * 16 / 128) * 16f + 16f),
                new SKRect(0f, 0f, 16f, 16f));
            canvas.Flush();
        }
        return portrait;
    }
}
