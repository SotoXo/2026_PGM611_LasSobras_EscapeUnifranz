using System;
using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.Player;
using EscapeUNIFRANZ.World;
using UnityEngine;

namespace EscapeUNIFRANZ.Hazards
{
    [RequireComponent(typeof(CircleCollider2D))]
    public sealed class DetectionSensor2D : MonoBehaviour, IZoneRuntimeInitializable
    {
        private CircleCollider2D sensor;
        private GameplayModeController gameplayMode;

        public Transform CurrentTarget { get; private set; }
        public bool HasTarget => CurrentTarget != null;
        public event Action<Transform> TargetDetected;
        public event Action TargetLost;

        private void Awake()
        {
            sensor = GetComponent<CircleCollider2D>();
            sensor.isTrigger = true;
        }

        public void Initialize(ZoneRuntimeContext context)
        {
            gameplayMode = context.GameplayMode;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!CanSense() || CurrentTarget != null)
            {
                return;
            }

            if (!PlayerPhysicsContact2D.IsPlayerBody(other))
            {
                return;
            }

            PlayerMovement2D player = other.GetComponent<PlayerMovement2D>();
            CurrentTarget = player.transform;
            TargetDetected?.Invoke(CurrentTarget);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (CurrentTarget == null ||
                !PlayerPhysicsContact2D.IsPlayerBody(other) ||
                other.GetComponent<PlayerMovement2D>()?.transform != CurrentTarget)
            {
                return;
            }

            CurrentTarget = null;
            TargetLost?.Invoke();
        }

        public void ClearTarget()
        {
            if (CurrentTarget == null)
            {
                return;
            }

            CurrentTarget = null;
            TargetLost?.Invoke();
        }

        private bool CanSense()
        {
            return gameplayMode != null &&
                (gameplayMode.CurrentMode == GameplayMode.Explore ||
                 gameplayMode.CurrentMode == GameplayMode.Encounter);
        }

        public static bool IsWithinRange(Vector2 origin, Vector2 target, float range)
        {
            if (range < 0f)
            {
                return false;
            }

            return (target - origin).sqrMagnitude <= range * range;
        }

        private void OnDrawGizmosSelected()
        {
            CircleCollider2D circle = sensor != null ? sensor : GetComponent<CircleCollider2D>();
            Gizmos.color = new Color(1f, 0.25f, 0.1f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, circle.radius * transform.lossyScale.x);
        }
    }
}
