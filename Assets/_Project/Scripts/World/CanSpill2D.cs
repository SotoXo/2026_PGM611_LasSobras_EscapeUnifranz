using System.Collections.Generic;
using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.Encounters;
using EscapeUNIFRANZ.Hazards;
using EscapeUNIFRANZ.Interaction;
using UnityEngine;

namespace EscapeUNIFRANZ.World
{
    /// <summary>A full can leaves a temporary trap; each robot can be stunned once per spill.</summary>
    [RequireComponent(typeof(CircleCollider2D))]
    public sealed class CanSpill2D : MonoBehaviour
    {
        private readonly HashSet<int> affected=new HashSet<int>();
        private GameplayModeController mode;
        private SpriteRenderer visual;
        private float age;
        public const float Lifetime=20f;
        public const float StunSeconds=4f;
        public static CanSpill2D Create(Vector2 point,Transform parent,GameplayModeController mode,MiniAutoController alreadyHit=null,MechanicalCrabController crabHit=null)
        {
            var obj=new GameObject("DerrameDeRefresco");obj.transform.SetParent(parent,true);obj.transform.position=point;
            obj.transform.localScale=new Vector3(1.4f,.65f,1);
            var circle=obj.AddComponent<CircleCollider2D>();circle.isTrigger=true;circle.radius=.5f;
            var renderer=obj.AddComponent<SpriteRenderer>();renderer.sprite=InteractionFx.Circle;
            renderer.sortingLayerName="Decals";renderer.sortingOrder=5;renderer.color=new Color(.65f,.28f,.07f,.8f);
            var spill=obj.AddComponent<CanSpill2D>();spill.mode=mode;spill.visual=renderer;
            if(alreadyHit!=null)spill.affected.Add(alreadyHit.GetInstanceID());
            if(crabHit!=null)spill.affected.Add(crabHit.GetInstanceID());
            return spill;
        }
        public bool TryAffect(MiniAutoController robot)
        {
            if(robot==null || (mode!=null&&!mode.AllowsMovement) || !robot.CanBeStunned || affected.Contains(robot.GetInstanceID()))return false;
            Vector2 nearest=GetComponent<CircleCollider2D>().ClosestPoint(robot.transform.position);
            if(Vector2.Distance(nearest,robot.transform.position)>.3f)return false;
            if(!DetectionSensor2D.HasLineOfSight(transform.position,robot.transform.position,~0,transform,robot.transform))return false;
            affected.Add(robot.GetInstanceID());robot.SuspendFor(StunSeconds);return true;
        }
        private void OnTriggerEnter2D(Collider2D other)
        {
            // A vision sensor reaching the puddle is not a wheel touching the liquid.
            if(!other.isTrigger || other.GetComponent<HazardContact2D>()!=null)
            {
                TryAffect(other.GetComponentInParent<MiniAutoController>());
                TryAffectCrab(other.GetComponentInParent<MechanicalCrabController>());
            }
        }
        public bool TryAffectCrab(MechanicalCrabController crab)
        {
            if(crab==null||(mode!=null&&!mode.AllowsMovement)||!crab.CanBeStunned||affected.Contains(crab.GetInstanceID()))return false;
            if(Vector2.Distance(GetComponent<CircleCollider2D>().ClosestPoint(crab.transform.position),crab.transform.position)>.3f)return false;
            if(!DetectionSensor2D.HasLineOfSight(transform.position,crab.transform.position,~0,transform,crab.transform))return false;
            affected.Add(crab.GetInstanceID());crab.SuspendFor(StunSeconds);return true;
        }
        private void OnTriggerStay2D(Collider2D other)=>OnTriggerEnter2D(other);
        private void Update()
        {
            if(mode!=null&&!mode.AllowsMovement)return;
            age+=Time.deltaTime;
            if(visual!=null){var color=visual.color;color.a=.8f*Mathf.Clamp01((Lifetime-age)/3f);visual.color=color;}
            if(age>=Lifetime)Destroy(gameObject);
        }
    }
}
