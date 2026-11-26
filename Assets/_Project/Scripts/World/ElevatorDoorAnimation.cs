using System.Collections;
using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.Narrative;
using UnityEngine;

namespace EscapeUNIFRANZ.World
{
    public enum ElevatorState { Unavailable, Opening, AwaitingPassenger, Closing, Travelling, Arriving, AwaitingExit }

    public sealed class ElevatorDoorAnimation : MonoBehaviour
    {
        public SpriteRenderer doorRenderer;
        public Sprite[] frames;
        public float[] frameSeconds;
        public Rect cabinBounds;
        public Vector2 cabinCenter;
        [Min(0f)] public float boardingDepth = .12f;
        public bool entranceAtTop = true;
        public ElevatorState State { get; private set; }
        public bool Boarded => State == ElevatorState.Travelling;

        private bool armed, available;
        private Coroutine opening;
        private Rigidbody2D assessedPlayer;
        private Collider2D playerFootprint;
        private BoxCollider2D threshold;

        public void SetAvailable(bool value)
        {
            if (State == ElevatorState.Closing || State == ElevatorState.Travelling ||
                State == ElevatorState.Arriving || State == ElevatorState.AwaitingExit) return;
            if (value == available && (State != ElevatorState.Unavailable || threshold != null)) return;
            available = value;
            StopOpening();
            if (!value)
            {
                State = ElevatorState.Unavailable;
                Show(0);
                SetThreshold(true);
                return;
            }
            State = ElevatorState.Opening;
            SetThreshold(true);
            ProjectAudioHooks.Emit(AudioCue.ElevatorOpen);
            opening = StartCoroutine(OpenForBoarding());
        }

        private IEnumerator OpenForBoarding()
        {
            yield return Play(true);
            SetThreshold(false);
            State = ElevatorState.AwaitingPassenger;
            opening = null;
        }

        public bool ShouldBoard(SceneFlowController flow)
        {
            if (flow == null || flow.PlayerBody == null) return false;
            Bounds footprint = GetFootprint(flow.PlayerBody);
            if (!cabinBounds.Overlaps(Rect.MinMaxRect(footprint.min.x, footprint.min.y, footprint.max.x, footprint.max.y)) &&
                !cabinBounds.Contains(flow.PlayerBody.position))
            {
                armed = true;
                if (State == ElevatorState.AwaitingExit) State = ElevatorState.AwaitingPassenger;
            }
            return armed && State == ElevatorState.AwaitingPassenger && !flow.IsTransitioning &&
                flow.ModeController?.CurrentMode == GameplayMode.Explore && IsDeepInside(cabinBounds, footprint, boardingDepth, entranceAtTop);
        }

        public static bool IsFullyInside(Rect cabin, Bounds footprint, float margin = .015f) =>
            footprint.min.x >= cabin.xMin + margin && footprint.max.x <= cabin.xMax - margin &&
            footprint.min.y >= cabin.yMin + margin && footprint.max.y <= cabin.yMax - margin;

        public static bool IsDeepInside(Rect cabin, Bounds footprint, float depth, bool entranceAtTop=true) =>
            IsFullyInside(cabin, footprint) && (entranceAtTop
                ? footprint.max.y <= cabin.yMax - Mathf.Max(.015f, depth)
                : footprint.min.y >= cabin.yMin + Mathf.Max(.015f, depth));

        public IEnumerator PrepareTravel(Rigidbody2D player)
        {
            if (!armed || State != ElevatorState.AwaitingPassenger || player == null ||
                !IsDeepInside(cabinBounds, GetFootprint(player), boardingDepth, entranceAtTop)) yield break;
            State = ElevatorState.Closing;
            armed = false;
            player.linearVelocity = Vector2.zero;
            SetThreshold(true);
            ProjectAudioHooks.Emit(AudioCue.DoorClose);
            yield return Play(false);
            if (player != null && IsFullyInside(cabinBounds, GetFootprint(player)))
                { State = ElevatorState.Travelling; ProjectAudioHooks.Emit(AudioCue.ElevatorTravel); }
        }

        public void BeginArrival()
        {
            StopOpening();
            State = ElevatorState.Arriving; ProjectAudioHooks.Emit(AudioCue.ElevatorArrive);
            armed = false;
            Show(0);
            SetThreshold(true);
        }

        public IEnumerator Arrive()
        {
            ProjectAudioHooks.Emit(AudioCue.ElevatorOpen);
            yield return Play(true);
            available = true;
            SetThreshold(false);
            State = ElevatorState.AwaitingExit;
        }

        public void CancelTravel()
        {
            StopOpening();
            State = ElevatorState.AwaitingExit;
            armed = false;
            if (frames != null) Show(frames.Length - 1);
            SetThreshold(false);
        }

        private Bounds GetFootprint(Rigidbody2D player)
        {
            if (assessedPlayer != player)
            {
                assessedPlayer = player;
                playerFootprint = player.GetComponent<CapsuleCollider2D>();
                if (playerFootprint == null) playerFootprint = player.GetComponent<Collider2D>();
            }
            return playerFootprint != null ? playerFootprint.bounds : new Bounds(player.position, Vector3.zero);
        }

        private void SetThreshold(bool closed)
        {
            if (threshold == null && closed && Application.isPlaying && cabinBounds.width > 0f)
            {
                var barrier = new GameObject("Umbral del ascensor");
                barrier.transform.SetParent(transform, false);
                barrier.transform.position = new Vector3(cabinBounds.center.x, entranceAtTop ? cabinBounds.yMax + .05f : cabinBounds.yMin - .05f, transform.position.z);
                barrier.layer = gameObject.layer;
                threshold = barrier.AddComponent<BoxCollider2D>();
                threshold.size = new Vector2(cabinBounds.width + .1f, .10f);
            }
            if (threshold != null) threshold.enabled = closed;
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
            if (doorRenderer != null && frames != null && index >= 0 && index < frames.Length)
                doorRenderer.sprite = frames[index];
        }

        private void StopOpening()
        {
            if (opening != null) StopCoroutine(opening);
            opening = null;
        }

        private void OnDisable()
        {
            StopOpening();
            State = ElevatorState.Unavailable;
            armed = available = false;
            SetThreshold(false);
            Show(0);
        }
    }
}

