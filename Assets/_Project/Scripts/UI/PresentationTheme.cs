using TMPro;
using UnityEngine;

namespace EscapeUNIFRANZ.UI
{
    public static class PresentationTheme
    {
        private static TMP_FontAsset font;

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
