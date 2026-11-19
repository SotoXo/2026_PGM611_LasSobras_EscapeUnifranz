using System.Collections;
using EscapeUNIFRANZ.Player;
using EscapeUNIFRANZ.UI;
using TMPro;
using UnityEngine;

namespace EscapeUNIFRANZ.Interaction
{
    /// <summary>
    /// Efectos visuales ligeros y sin arte nuevo para recoger, usar y lanzar objetos:
    /// el objeto vuela hacia el jugador, aparece un texto flotante y sale una nube de polvo.
    /// Todo se crea en tiempo de ejecución, así que no requiere cambios en las escenas.
    /// </summary>
    public static class InteractionFx
    {
        private const string SortingLayerName = "World";
        private const int TopOrder = 29500;

        private static Sprite circle;
        private static FxRunner runner;

        private sealed class FxRunner : MonoBehaviour { }

        private static FxRunner Runner
        {
            get
            {
                if (runner == null)
                {
                    var holder = new GameObject("InteractionFxRunner");
                    Object.DontDestroyOnLoad(holder);
                    runner = holder.AddComponent<FxRunner>();
                }

                return runner;
            }
        }

        /// <summary>Círculo suave blanco (1 unidad de diámetro) para sombras y nubes de polvo.</summary>
        public static Sprite Circle
        {
            get
            {
                if (circle == null)
                {
                    circle = BuildCircle();
                }

                return circle;
            }
        }

        /// <summary>Recoger: pose del jugador, el objeto vuela hacia él y sale "+ nombre".</summary>
        public static void Pickup(Sprite sprite, Vector2 from, string label)
        {
            PlayerVisualController player = PlayerVisualController.Active;
            if (player != null)
            {
                player.PlayAction(PlayerVisualController.ActionPose.Pickup);
            }

            if (sprite != null)
            {
                Runner.StartCoroutine(FlySprite(sprite, from, PlayerPoint(from)));
            }

            if (!string.IsNullOrWhiteSpace(label))
            {
                Popup("+ " + label, from + Vector2.up * 0.5f, new Color(0.6f, 1f, 0.7f, 1f));
            }
        }

        /// <summary>Usar un objeto sobre algo del mundo: pose del jugador y texto flotante.</summary>
        public static void Use(Vector2 at, string label)
        {
            PlayerVisualController player = PlayerVisualController.Active;
            if (player != null)
            {
                player.PlayAction(PlayerVisualController.ActionPose.Use);
            }

            if (!string.IsNullOrWhiteSpace(label))
            {
                Popup("Usas " + label, at + Vector2.up * 0.7f, new Color(0.5f, 0.95f, 1f, 1f));
            }
        }

        /// <summary>Nube de polvo que se expande y se desvanece (p. ej. al caer una lata).</summary>
        public static void Puff(Vector2 position, Color color)
        {
            Runner.StartCoroutine(PuffRoutine(position, color));
        }

        /// <summary>Texto que sube y se desvanece sobre un punto del mundo.</summary>
        public static void Popup(string text, Vector2 position, Color color)
        {
            Runner.StartCoroutine(PopupRoutine(text, position, color));
        }

        private static Vector2 PlayerPoint(Vector2 fallback)
        {
            PlayerVisualController player = PlayerVisualController.Active;
            return player != null
                ? (Vector2)player.transform.position + Vector2.up * 0.35f
                : fallback;
        }

        private static IEnumerator FlySprite(Sprite sprite, Vector2 from, Vector2 to)
        {
            var go = new GameObject("FxPickup");
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingLayerName = SortingLayerName;
            renderer.sortingOrder = TopOrder;

            // Se normaliza el tamaño para que un sprite grande no tape la pantalla.
            float largest = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
            float fit = largest > 0.0001f ? 0.6f / largest : 1f;

            const float duration = 0.32f;
            float t = 0f;
            while (t < 1f && go != null)
            {
                t += Time.deltaTime / duration;
                float k = Mathf.Clamp01(t);
                float eased = 1f - (1f - k) * (1f - k);
                Vector2 position = Vector2.Lerp(from, to, eased) + Vector2.up * Mathf.Sin(k * Mathf.PI) * 0.35f;
                go.transform.position = position;
                go.transform.localScale = Vector3.one * (fit * Mathf.Lerp(1f, 0.5f, k));
                Color color = renderer.color;
                color.a = k < 0.7f ? 1f : 1f - (k - 0.7f) / 0.3f;
                renderer.color = color;
                yield return null;
            }

            if (go != null)
            {
                Object.Destroy(go);
            }
        }

        private static IEnumerator PopupRoutine(string text, Vector2 position, Color color)
        {
            var go = new GameObject("FxPopup");
            go.transform.position = new Vector3(position.x, position.y, -0.2f);
            TextMeshPro label = go.AddComponent<TextMeshPro>();
            label.font = PresentationTheme.Font;
            label.fontSize = 1.6f;
            label.alignment = TextAlignmentOptions.Center;
            label.rectTransform.sizeDelta = new Vector2(5f, 0.8f);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.text = text;
            label.color = color;
            MeshRenderer meshRenderer = go.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                meshRenderer.sortingLayerName = SortingLayerName;
                meshRenderer.sortingOrder = TopOrder + 1;
            }

            const float duration = 0.9f;
            float t = 0f;
            while (t < 1f && go != null)
            {
                t += Time.deltaTime / duration;
                float k = Mathf.Clamp01(t);
                go.transform.position = new Vector3(position.x, position.y + k * 0.6f, -0.2f);
                Color tint = color;
                tint.a = k < 0.6f ? 1f : 1f - (k - 0.6f) / 0.4f;
                label.color = tint;
                yield return null;
            }

            if (go != null)
            {
                Object.Destroy(go);
            }
        }

        private static IEnumerator PuffRoutine(Vector2 position, Color color)
        {
            var go = new GameObject("FxPuff");
            go.transform.position = position;
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = Circle;
            renderer.sortingLayerName = SortingLayerName;
            renderer.sortingOrder = TopOrder - 1;

            const float duration = 0.3f;
            float t = 0f;
            while (t < 1f && go != null)
            {
                t += Time.deltaTime / duration;
                float k = Mathf.Clamp01(t);
                go.transform.localScale = new Vector3(Mathf.Lerp(0.25f, 0.95f, k), Mathf.Lerp(0.12f, 0.45f, k), 1f);
                Color tint = color;
                tint.a = 0.5f * (1f - k);
                renderer.color = tint;
                yield return null;
            }

            if (go != null)
            {
                Object.Destroy(go);
            }
        }

        private static Sprite BuildCircle()
        {
            const int size = 32;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "InteractionFxCircle",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            var pixels = new Color32[size * size];
            float center = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy) / center;
                    float alpha = Mathf.Clamp01((1f - distance) * 3f);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        }
    }
}
