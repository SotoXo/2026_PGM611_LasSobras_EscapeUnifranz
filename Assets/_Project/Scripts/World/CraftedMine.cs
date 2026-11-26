using System.Collections.Generic;
using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.Encounters;
using EscapeUNIFRANZ.Hazards;
using EscapeUNIFRANZ.Narrative;
using UnityEngine;
namespace EscapeUNIFRANZ.World
{
    public sealed class CraftedMine : MonoBehaviour
    {
        public SpriteRenderer visual;public Sprite[] frames;public float[] durations;
        private GameplayModeController mode;private float age,clock;private int frame;private bool exploded;
        public bool Exploded=>exploded;
        private void Awake(){var body=GetComponent<Rigidbody2D>()??gameObject.AddComponent<Rigidbody2D>();body.bodyType=RigidbodyType2D.Dynamic;body.gravityScale=0;body.constraints=RigidbodyConstraints2D.FreezeAll;}
        public void Bind(GameplayModeController value)=>mode=value;
        private void Update()
        {
            if(mode!=null&&!mode.AllowsMovement)return;
            age+=Time.deltaTime;
            if(!exploded){if(frames!=null&&frames.Length>3)visual.sprite=frames[(int)(age*5)%4];return;}
            clock+=Time.deltaTime;
            while(clock>=durations[frame]){clock-=durations[frame];if(++frame>=frames.Length){Destroy(gameObject);return;}visual.sprite=frames[frame];}
        }
        private void OnTriggerStay2D(Collider2D other)=>TryDetonate(other);
        private void OnTriggerEnter2D(Collider2D other)=>TryDetonate(other);
        public bool TryDetonate(Collider2D other)
        {
            if(exploded||age<1f||(mode!=null&&!mode.AllowsMovement)||other.isTrigger)return false;
            var crab=other.GetComponentInParent<MechanicalCrabController>();var robot=other.GetComponentInParent<MiniAutoController>();
            if(crab==null&&robot==null)return false;
            exploded=true;frame=4;clock=0;visual.sortingLayerName="Effects";visual.sprite=frames[frame];GetComponent<Collider2D>().enabled=false;
            var seen=new HashSet<int>();
            foreach(var hit in Physics2D.OverlapCircleAll(transform.position,1.25f))
            {
                var c=hit.GetComponentInParent<MechanicalCrabController>();var r=hit.GetComponentInParent<MiniAutoController>();
                Transform target=c!=null?c.transform:r!=null?r.transform:null;
                if(target==null||!seen.Add(target.GetInstanceID())||!DetectionSensor2D.HasLineOfSight(transform.position,target.position,~0,transform,target))continue;
                if(c!=null)c.ReceiveMineHit();else r.ReceiveMineHit();
            }
            ProjectAudioHooks.EmitAt(AudioCue.DistantImpact,transform.position);return true;
        }
    }
}
