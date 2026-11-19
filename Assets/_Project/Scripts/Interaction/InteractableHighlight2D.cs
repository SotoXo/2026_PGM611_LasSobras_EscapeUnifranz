using EscapeUNIFRANZ.Player;
using UnityEngine;

namespace EscapeUNIFRANZ.Interaction
{
    /// <summary>
    /// Contorno cálido que late alrededor de los objetos que se pueden usar ahora mismo.
    /// Solo se muestra cerca del jugador y solo si el objeto está disponible y cumple sus
    /// requisitos, así un objeto bloqueado no parece utilizable. Se agrega solo (ver
    /// <see cref="InteractableHighlightBootstrap"/>), no hace falta tocar las escenas.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InteractableHighlight2D : MonoBehaviour
    {
        private const float VisibleRange = 5.5f;
        private static readonly Color Tint = new Color(1f, 0.86f, 0.42f, 1f);

        private InteractableBehaviour target;
        private SpriteRenderer source;
        private SpriteRenderer halo;
        private float alpha;

        public void Bind(InteractableBehaviour interactable, SpriteRenderer sourceRenderer)
        {
            target = interactable;
            source = sourceRenderer;
        }

        private void LateUpdate()
        {
            if (target == null || source == null)
            {
                return;
            }

            alpha = Mathf.MoveTowards(alpha, ShouldShow() ? 1f : 0f, Time.unscaledDeltaTime * 4f);
            if (alpha <= 0.001f)
            {
                if (halo != null && halo.enabled)
                {
                    halo.enabled = false;
                }

                return;
            }

            EnsureHalo();
            halo.enabled = true;
            halo.sprite = source.sprite;
            halo.flipX = source.flipX;
            halo.flipY = source.flipY;
            halo.sortingLayerID = source.sortingLayerID;
            halo.sortingOrder = source.sortingOrder - 1;

            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 3.2f);
            Color color = Tint;
            color.a = Mathf.Lerp(0.12f, 0.42f, pulse) * alpha;
            halo.color = color;
        }

        private bool ShouldShow()
        {
            if (!source.enabled || source.sprite == null || !target.isActiveAndEnabled)
            {
                return false;
            }

            PlayerVisualController player = PlayerVisualController.Active;
            if (player == null)
            {
                return false;
            }

            Vector2 offset = player.transform.position - transform.position;
            if (offset.sqrMagnitude > VisibleRange * VisibleRange)
            {
                return false;
            }

            return target.CanInteract && target.RequirementsMet;
        }

        private void EnsureHalo()
        {
            if (halo != null)
            {
                return;
            }

            var holder = new GameObject("UsableHighlight");
            holder.transform.SetParent(transform, false);
            holder.transform.localScale = new Vector3(1.1f, 1.1f, 1f);
            halo = holder.AddComponent<SpriteRenderer>();
        }
    }

    /// <summary>
    /// Revisa cada medio segundo los objetos interactuables cercanos al jugador y les agrega
    /// <see cref="InteractableHighlight2D"/>. Se crea solo al iniciar el juego.
    /// </summary>
    public static class InteractableHighlightBootstrap
    {
        private const float ScanRange = 9f;
        private const float MaxSpriteSize = 4f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            var holder = new GameObject("InteractableHighlightScanner");
            Object.DontDestroyOnLoad(holder);
            holder.AddComponent<Scanner>();
        }

        private sealed class Scanner : MonoBehaviour
        {
            private float nextScanAt;

            private void Update()
            {
                if (Time.unscaledTime < nextScanAt)
                {
                    return;
                }

                nextScanAt = Time.unscaledTime + 0.5f;
                PlayerVisualController player = PlayerVisualController.Active;
                if (player == null)
                {
                    return;
                }

                Vector2 playerPosition = player.transform.position;
                InteractableBehaviour[] items =
                    FindObjectsByType<InteractableBehaviour>(FindObjectsSortMode.None);
                for (int i = 0; i < items.Length; i++)
                {
                    InteractableBehaviour item = items[i];
                    if (item == null || item.GetComponent<InteractableHighlight2D>() != null)
                    {
                        continue;
                    }

                    if (((Vector2)item.transform.position - playerPosition).sqrMagnitude > ScanRange * ScanRange)
                    {
                        continue;
                    }

                    SpriteRenderer spriteRenderer = item.GetComponent<SpriteRenderer>();
                    if (spriteRenderer == null || spriteRenderer.sprite == null || spriteRenderer.drawMode != SpriteDrawMode.Simple)
                    {
                        continue;
                    }

                    Vector3 size = spriteRenderer.bounds.size;
                    if (size.x > MaxSpriteSize || size.y > MaxSpriteSize)
                    {
                        continue;
                    }

                    item.gameObject.AddComponent<InteractableHighlight2D>().Bind(item, spriteRenderer);
                }
            }
        }
    }
}
