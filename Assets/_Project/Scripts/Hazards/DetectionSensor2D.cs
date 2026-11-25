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
        private PatrolMover2D owner;
        private Transform visionPlayer;
        private PlayerVisionController playerVision;
        private float nextSenseAt;
        [SerializeField, Min(0.02f)] private float sensingInterval = 0.1f;
        [SerializeField] private bool directional;
        [SerializeField, Range(1f,179f)] private float fieldOfView=75f;
        [SerializeField] private LayerMask obstacleLayers=~0;
        [SerializeField] private bool reactToPlayerGaze;
        [SerializeField] private float noticeRange = 2.5f;
        [SerializeField] private float pursuitRange = 4.5f;
        public bool IsTargetVisible { get; private set; }
        public static bool RetainPursuit(bool withinReach, bool visible) => withinReach || visible;
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
            owner = GetComponentInParent<PatrolMover2D>();
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
        private void OnTriggerStay2D(Collider2D other) => OnTriggerEnter2D(other);

        private void FixedUpdate()
        {
            if (Time.time < nextSenseAt) return;
            nextSenseAt=Time.time+sensingInterval;
            Transform visible=null;
            IsTargetVisible = false;
            float range = sensor.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y);
            // Keep the remembered target when nearby, but never expose its position through walls.
            if (reactToPlayerGaze && CanSense() && CurrentTarget != null)
            {
                bool line = ClearLine(CurrentTarget);
                bool inSight = line && IsWithinRange(transform.position, CurrentTarget.position, range);
                if (RetainPursuit(IsWithinRange(transform.position, CurrentTarget.position, pursuitRange), inSight))
                { IsTargetVisible = inSight; return; }
            }
            if (CanSense()) foreach(var player in candidates)
            {
                if (player==null || !player.isActiveAndEnabled) continue;
                if (!IsWithinRange(transform.position,player.transform.position,range)) continue;
                bool noticed = !directional || IsInField(transform.position,player.transform.position,transform.up,
                    reactToPlayerGaze ? noticeRange : range,fieldOfView);
                if (reactToPlayerGaze)
                {
                    var vision = ResolveVision(player.transform);
                    if (vision != null) noticed |=
                        (vision.IsLookingAt(transform.position) && vision.CanSeePoint(transform.position, owner != null ? owner.transform : transform)) ||
                        vision.IlluminatesPoint(transform.position, owner != null ? owner.transform : transform);
                }
                if (!noticed || !ClearLine(player.transform)) continue;
                visible=player.transform; break;
            }
            IsTargetVisible = visible != null;
            if (visible==CurrentTarget) return;
            ClearTarget(); CurrentTarget=visible;
            IsTargetVisible = visible != null;
            if (visible!=null) TargetDetected?.Invoke(visible);
        }
        private bool ClearLine(Transform player)
        {
            var vision = reactToPlayerGaze ? ResolveVision(player) : null;
            bool exposedByBeam=vision != null && vision.IlluminatesPoint(transform.position,owner!=null ? owner.transform : transform);
            if (player.TryGetComponent<PlayerMovement2D>(out var movement) && movement.IsConcealed &&
                !exposedByBeam && !IsWithinRange(transform.position, player.position, 1.05f))
                return false;
            return HasLineOfSight(transform.position,player.position,obstacleLayers,owner!=null ? owner.transform : transform,player,hits);
        }

        private PlayerVisionController ResolveVision(Transform player)
        {
            if (visionPlayer != player)
            {
                visionPlayer = player;
                playerVision = player.GetComponentInChildren<PlayerVisionController>();
            }
            return playerVision;
        }
        public static bool IsInField(Vector2 origin,Vector2 target,Vector2 facing,float range,float angle)
        { return IsWithinRange(origin,target,range) && Vector2.Angle(facing,target-origin)<=angle*0.5f; }

        private void OnTriggerExit2D(Collider2D other)
        {
            var player=other.GetComponent<PlayerMovement2D>(); if (player!=null) candidates.Remove(player);
            if (reactToPlayerGaze) return; // FixedUpdate evaluates both range and line of sight.
            if (CurrentTarget == null ||
                !PlayerPhysicsContact2D.IsPlayerBody(other) ||
                other.GetComponent<PlayerMovement2D>()?.transform != CurrentTarget)
            {
                return;
            }

            ClearTarget();
        }
        private void OnDisable() { visionPlayer = null; playerVision = null; candidates.Clear(); ClearTarget(); }

        public void ClearTarget()
        {
            IsTargetVisible = false;
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
