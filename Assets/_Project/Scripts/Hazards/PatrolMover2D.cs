using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.Encounters;
using EscapeUNIFRANZ.World;
using UnityEngine;

namespace EscapeUNIFRANZ.Hazards
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PatrolMover2D : MonoBehaviour, IZoneRuntimeInitializable, IEncounterResettable
    {
        [SerializeField] private PatrolPath2D path;
        [SerializeField, Min(0f)] private float patrolSpeed = 2f;
        [SerializeField, Min(0.01f)] private float arrivalDistance = 0.08f;

        private readonly PatrolRouteState route = new PatrolRouteState();
        private Rigidbody2D body;
        private GameplayModeController gameplayMode;
        private Transform chaseTarget;
        private float chaseSpeed;
        private Vector2 initialPosition;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            initialPosition = body.position;
        }

        public void Initialize(ZoneRuntimeContext context)
        {
            gameplayMode = context.GameplayMode;
        }

        private void FixedUpdate()
        {
            if (body == null || gameplayMode == null ||
                (gameplayMode.CurrentMode != GameplayMode.Explore &&
                 gameplayMode.CurrentMode != GameplayMode.Encounter))
            {
                return;
            }

            Vector2 destination;
            float speed;
            if (chaseTarget != null)
            {
                destination = chaseTarget.position;
                speed = chaseSpeed;
            }
            else if (path != null && path.Count > 0)
            {
                destination = path.GetPoint(route.Index);
                speed = patrolSpeed;
                if ((destination - body.position).sqrMagnitude <= arrivalDistance * arrivalDistance)
                {
                    route.Advance(path.Count, path.PingPong);
                    destination = path.GetPoint(route.Index);
                }
            }
            else
            {
                return;
            }

            body.MovePosition(Vector2.MoveTowards(
                body.position,
                destination,
                speed * Time.fixedDeltaTime));
        }

        public void SetChaseTarget(Transform target, float speed)
        {
            chaseTarget = target;
            chaseSpeed = Mathf.Max(0f, speed);
        }

        public void ResumePatrol()
        {
            chaseTarget = null;
        }

        public void ResetEncounter()
        {
            chaseTarget = null;
            route.Reset();
            if (body != null)
            {
                body.position = initialPosition;
                body.linearVelocity = Vector2.zero;
            }
        }
    }
}
