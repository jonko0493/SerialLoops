using System.Linq;
using HaruhiChokuretsuLib.Archive;
using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Archive.Graphics;
using SkiaSharp;

namespace SerialLoops.Lib.Util;

public static class GraphicsUtil
{
    public static SKBitmap GetCharacterIcon(ArchiveFile<GraphicsFile> grp, Speaker character)
    {
        SKBitmap bitmap = grp.GetFileByName("SYS_CMN_B36DNX").GetImage(transparentIndex: 0);

        // Crop a 16x16 bitmap portrait
        SKBitmap portrait = new(16, 16);
        if (new[] { Speaker.KYON, Speaker.HARUHI, Speaker.MIKURU, Speaker.NAGATO, Speaker.KOIZUMI }.Contains(character))
        {
            SKCanvas canvas = new(portrait);
            int characterOffset = (int)character - 1;
            canvas.DrawBitmap(bitmap,
                new SKRect((float)((characterOffset * 32) % 128), (characterOffset * 32 / 128) * 32f,
                    ((characterOffset * 32) % 128) + 32f, (characterOffset * 32 / 128) * 32f + 32f),
                new SKRect(0f, 0f, 0f, 0f));
            canvas.Flush();
        }
        return portrait;
    }
}
