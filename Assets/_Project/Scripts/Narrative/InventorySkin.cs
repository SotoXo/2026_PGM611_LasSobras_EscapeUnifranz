using UnityEngine;
namespace EscapeUNIFRANZ.Narrative
{
    public sealed class InventorySkin : ScriptableObject
    {
        public Sprite[] sprites;
        public Sprite Find(string name)
        {
            if (sprites != null) foreach (var sprite in sprites)
                if (sprite != null && sprite.name == name) return sprite;
            return null;
        }
    }
}
