using System;
using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.Player;
using EscapeUNIFRANZ.World;
using UnityEngine;
using System.Collections.Generic;

namespace EscapeUNIFRANZ.Hazards
{
    [RequireComponent(typeof(CircleCollider2D))]
    public sealed class DetectionSensor2D : MonoBehaviour, IZoneRuntimeInitializable
    {
        private CircleCollider2D sensor;
        private GameplayModeController gameplayMode;
        [SerializeField] private bool directional;
        [SerializeField, Range(1f,179f)] private float fieldOfView=75f;
        [SerializeField] private LayerMask obstacleLayers=~0;
        private readonly HashSet<PlayerMovement2D> candidates=new HashSet<PlayerMovement2D>();
        private readonly List<RaycastHit2D> hits=new List<RaycastHit2D>();

        public Transform CurrentTarget { get; private set; }
        public bool HasTarget => CurrentTarget != null;
        public event Action<Transform> TargetDetected;
        public event Action TargetLost;
        public void Face(Vector2 direction)
        { if (directional && direction.sqrMagnitude>0.001f) transform.rotation=Quaternion.Euler(0f,0f,Vector2.SignedAngle(Vector2.up,direction)); }
        public static bool HasLineOfSight(Vector2 origin,Vector2 target,LayerMask layers,Transform ignoredOwner=null,Transform ignoredTarget=null,List<RaycastHit2D> buffer=null)
        {
            Vector2 delta=target-origin; var filter=ContactFilter2D.noFilter; filter.useTriggers=false; filter.SetLayerMask(layers);
            var obstacles=buffer ?? new List<RaycastHit2D>(); obstacles.Clear(); Physics2D.Raycast(origin,delta.normalized,filter,obstacles,delta.magnitude);
            foreach(var hit in obstacles)
            {
                if (hit.collider==null || (ignoredOwner!=null && hit.transform.IsChildOf(ignoredOwner)) ||
                    (ignoredTarget!=null && hit.transform.IsChildOf(ignoredTarget))) continue;
                return false;
            }
            return true;
        }

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
            if (!PlayerPhysicsContact2D.IsPlayerBody(other))
            {
                return;
            }

            PlayerMovement2D player = other.GetComponent<PlayerMovement2D>();
            if (player!=null) candidates.Add(player);
        }

        private void FixedUpdate()
        {
            Transform visible=null;
            if (CanSense()) foreach(var player in candidates)
            {
                if (player==null || !player.isActiveAndEnabled) continue;
                float range=sensor.radius*Mathf.Max(transform.lossyScale.x,transform.lossyScale.y);
                if (!IsWithinRange(transform.position,player.transform.position,range)) continue;
                if ((directional && !IsInField(transform.position,player.transform.position,transform.up,range,fieldOfView)) || !ClearLine(player.transform)) continue;
                visible=player.transform; break;
            }
            if (visible==CurrentTarget) return;
            ClearTarget(); CurrentTarget=visible;
            if (visible!=null) TargetDetected?.Invoke(visible);
        }
        private bool ClearLine(Transform player)
        {
            var owner=GetComponentInParent<PatrolMover2D>();
            return HasLineOfSight(transform.position,player.position,obstacleLayers,owner!=null ? owner.transform : transform,player,hits);
        }
        public static bool IsInField(Vector2 origin,Vector2 target,Vector2 facing,float range,float angle)
        { return IsWithinRange(origin,target,range) && Vector2.Angle(facing,target-origin)<=angle*0.5f; }

        private void OnTriggerExit2D(Collider2D other)
        {
            var player=other.GetComponent<PlayerMovement2D>(); if (player!=null) candidates.Remove(player);
            if (CurrentTarget == null ||
                !PlayerPhysicsContact2D.IsPlayerBody(other) ||
                other.GetComponent<PlayerMovement2D>()?.transform != CurrentTarget)
            {
                return;
            }

            CurrentTarget = null;
            TargetLost?.Invoke();
        }
        private void OnDisable() { candidates.Clear(); ClearTarget(); }

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
