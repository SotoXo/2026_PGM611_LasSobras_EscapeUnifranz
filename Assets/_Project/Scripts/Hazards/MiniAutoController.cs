using EscapeUNIFRANZ.Encounters;
using EscapeUNIFRANZ.World;
using UnityEngine;

namespace EscapeUNIFRANZ.Hazards
{
    /// <summary>
    /// Adds a brief deterministic chase to a reusable patrol mover.
    /// </summary>
    public sealed class MiniAutoController : MonoBehaviour, IZoneRuntimeInitializable, IEncounterResettable
    {
        [SerializeField] private PatrolMover2D mover;
        [SerializeField] private DetectionSensor2D sensor;
        [SerializeField, Min(0f)] private float chaseSpeed = 3.5f;
        [SerializeField, Min(0f)] private float chaseSeconds = 1.5f;

        private float chaseUntil;

        public void Initialize(ZoneRuntimeContext context)
        {
            if (sensor != null)
            {
                sensor.TargetDetected += OnTargetDetected;
                sensor.TargetLost += OnTargetLost;
            }
        }

        private void OnDestroy()
        {
            if (sensor != null)
            {
                sensor.TargetDetected -= OnTargetDetected;
                sensor.TargetLost -= OnTargetLost;
            }
        }

        private void Update()
        {
            if (chaseUntil > 0f && Time.time >= chaseUntil)
            {
                chaseUntil = 0f;
                mover?.ResumePatrol();
            }
        }

        private void OnTargetDetected(Transform target)
        {
            chaseUntil = Time.time + chaseSeconds;
            mover?.SetChaseTarget(target, chaseSpeed);
        }

        private void OnTargetLost()
        {
            if (chaseUntil <= Time.time)
            {
                mover?.ResumePatrol();
            }
        }

        public void ResetEncounter()
        {
            chaseUntil = 0f;
            sensor?.ClearTarget();
            mover?.ResetEncounter();
        }
    }
}
