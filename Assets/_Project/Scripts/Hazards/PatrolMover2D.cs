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
        private Vector2? investigationPoint;
        private readonly RaycastHit2D[] obstacles=new RaycastHit2D[12];
        public Vector2 Facing { get; private set; } = Vector2.down;
        private DetectionSensor2D[] facingSensors;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            initialPosition = body.position;
            facingSensors=GetComponentsInChildren<DetectionSensor2D>(true);
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
            else if (investigationPoint.HasValue) { destination=investigationPoint.Value; speed=chaseSpeed; }
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

            Vector2 delta=destination-body.position;
            float distance=Mathf.Min(delta.magnitude,speed*Time.fixedDeltaTime);
            if (distance<0.001f) return;
            Vector2 direction=delta.normalized;
            var filter=ContactFilter2D.noFilter; filter.useTriggers=false;
            int count=body.Cast(direction,filter,obstacles,distance+0.04f);
            float allowed=distance;
            for(int i=0;i<count;i++) if (obstacles[i].collider!=null && obstacles[i].rigidbody!=body)
                allowed=Mathf.Min(allowed,Mathf.Max(0f,obstacles[i].distance-0.04f));
            if (allowed>0f)
            {
                Facing=direction; body.MovePosition(body.position+direction*allowed);
                foreach(var detector in facingSensors) if (detector.transform!=transform) detector.Face(direction);
            }
        }

        public void SetChaseTarget(Transform target, float speed)
        {
            chaseTarget = target;
            investigationPoint=null;
            chaseSpeed = Mathf.Max(0f, speed);
        }

        public void ResumePatrol()
        {
            chaseTarget = null;
            investigationPoint=null;
        }
        public void Investigate(Vector2 point,float speed)
        { chaseTarget=null; investigationPoint=point; chaseSpeed=speed; }

        public void ResetEncounter()
        {
            chaseTarget = null;
            investigationPoint=null;
            route.Reset();
            if (body != null)
            {
                body.position = initialPosition;
                body.linearVelocity = Vector2.zero;
            }
        }
    }
}
