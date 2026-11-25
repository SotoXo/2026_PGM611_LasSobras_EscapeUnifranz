using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EscapeUNIFRANZ.UI
{
    public static class PresentationTheme
    {
        private static TMP_FontAsset font;
        private static TMP_FontAsset pixelFont;
        public static TMP_FontAsset PixelFont
        {
            get
            {
                if (pixelFont == null)
                    pixelFont = Resources.Load<TMP_FontAsset>("UI/PixelifySans");
                return pixelFont != null ? pixelFont : TMP_Settings.defaultFontAsset;
            }
        }
        private static Sprite frame;
        public static void StylePanel(Image image)
        {
            if (image == null) return;
            if (frame == null) frame = Resources.Load<Sprite>("UI/PixelFrame");
            if (frame == null) return;
            image.sprite = frame;
            image.type = Image.Type.Sliced;
            image.color = Color.white;
        }

        public static void StyleText(TMP_Text label)
        {
            if (label == null || PixelFont == null || label.font == PixelFont) return;
            label.font = PixelFont;
            label.fontSharedMaterial = PixelFont.material;
        }

        public static TMP_FontAsset Font
        {
            get
            {
                if (font == null)
                {
                    font = Resources.Load<TMP_FontAsset>("UI/EscapeUnifranzFont");
                    if (font == null)
                    {
                        font = TMP_Settings.defaultFontAsset;
                    }
                }
                return font;
            }
        }
    }
}
