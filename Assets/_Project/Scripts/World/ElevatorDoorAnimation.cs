using System.Collections;
using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.Narrative;
using EscapeUNIFRANZ.Player;
using UnityEngine;

namespace EscapeUNIFRANZ.World
{
    /// <summary>Plays the authored door frames once, retaining their JSON timing.</summary>
    public sealed class ElevatorDoorAnimation : MonoBehaviour
    {
        public SpriteRenderer doorRenderer;
        public Sprite[] frames;
        public float[] frameSeconds;
        public Rect cabinBounds;
        public Vector2 cabinCenter;
        public bool Boarded { get; private set; }
        private bool armed;
        private Coroutine opening;
        private bool? available;
        private bool travelling;

        public void SetAvailable(bool value)
        {
            if (available == value || travelling) return;
            available = value;
            if (opening != null) StopCoroutine(opening);
            opening = null;
            if (value) opening = StartCoroutine(Play(true));
            else Show(0);
        }

        public bool ShouldBoard(SceneFlowController flow)
        {
            if (flow == null || flow.PlayerBody == null || travelling) return false;
            bool inside = cabinBounds.Contains(flow.PlayerBody.position);
            if (!inside) armed = true;
            return inside && armed && !flow.IsTransitioning && flow.ModeController?.CurrentMode == GameplayMode.Explore;
        }

        public IEnumerator PrepareTravel(Rigidbody2D player)
        {
            travelling = true;
            if (opening != null) StopCoroutine(opening);
            opening = null;
            if (frames != null && frames.Length > 0 && doorRenderer != null && doorRenderer.sprite != frames[frames.Length - 1])
                yield return Play(true);
            Boarded = false;
            var movement = player != null ? player.GetComponent<PlayerMovement2D>() : null;
            bool restoreMovement = movement != null && movement.enabled;
            if (restoreMovement) movement.enabled = false;
            try
            {
                float startedAt = Time.time;
                while (player != null && Vector2.Distance(player.position, cabinCenter) > .025f && Time.time - startedAt < 2f)
                {
                    player.linearVelocity = Vector2.ClampMagnitude((cabinCenter - player.position) / Mathf.Max(Time.deltaTime, .001f), 2f);
                    yield return null;
                }
            }
            finally
            {
                if (player != null) player.linearVelocity = Vector2.zero;
                if (restoreMovement && movement != null) movement.enabled = true;
            }
            Boarded = player != null && cabinBounds.Contains(player.position) && Vector2.Distance(player.position, cabinCenter) < .08f;
            if (!Boarded) yield break;
            ProjectAudioHooks.Emit(AudioCue.DoorClose);
            yield return Play(false);
        }

        public void BeginArrival()
        {
            travelling = true; armed = false;
            if (opening != null) StopCoroutine(opening);
            opening = null; Show(0);
        }

        public IEnumerator Arrive()
        {
            yield return Play(true);
            available = true; travelling = false; armed = false;
        }

        public void CancelTravel()
        {
            travelling = false;
            available = doorRenderer != null && frames != null && frames.Length > 0 &&
                doorRenderer.sprite == frames[frames.Length - 1] ? true : (bool?)null;
        }

        private IEnumerator Play(bool open)
        {
            if (frames == null || frames.Length == 0 || doorRenderer == null) yield break;
            for (int step = 0; step < frames.Length; step++)
            {
                int index = open ? step : frames.Length - 1 - step;
                Show(index);
                float seconds = frameSeconds != null && index < frameSeconds.Length ? frameSeconds[index] : .07f;
                yield return new WaitForSeconds(Mathf.Max(.01f, seconds));
            }
        }

        private void Show(int index)
        {
            if (doorRenderer != null && frames != null && index < frames.Length) doorRenderer.sprite = frames[index];
        }

        private void OnDisable()
        {
            StopAllCoroutines(); opening = null; available = null; travelling = false; armed = false; Show(0);
        }
    }
}
