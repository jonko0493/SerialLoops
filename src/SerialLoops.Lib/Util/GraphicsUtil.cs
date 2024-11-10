using SerialLoops.Lib.Items;
using SkiaSharp;

namespace SerialLoops.Lib.Util
{
    public static class GraphicsUtil
    {
        public static SKBitmap GetCharacterIcon(Project project, CharacterIcon character)
        {
            ItemDescription id = project.Items.Find(i => i.Name.Equals("SYSTEX_XTR_PRG_T08"));
            if (id is not SystemTextureItem tex)
            {
                return null;
            }
            SKBitmap bitmap = tex.Grp.GetImage(transparentIndex: 0, width: 16);

            // Crop a 16x16 bitmap portrait
            SKBitmap portrait = new(16, 16);
            int z = (4 + (int) character) * 16;

            SKRectI cropRect = new(0, z, 16, z + 16);
            bitmap.ExtractSubset(portrait, cropRect);
            return portrait;
        }
    }

    public enum CharacterIcon
    {
        Haruhi = 1,
        Mikuru = 2,
        Nagato = 3,
        Koizumi = 4,
        Tsuruya = 5,
        Unknown = 6,
    }
}


