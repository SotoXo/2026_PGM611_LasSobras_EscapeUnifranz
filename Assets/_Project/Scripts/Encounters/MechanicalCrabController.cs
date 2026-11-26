using System;
using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.Hazards;
using EscapeUNIFRANZ.World;
using UnityEngine;

namespace EscapeUNIFRANZ.Encounters
{
    [Serializable] public sealed class CrabDirectionFrames { public Sprite[] frames; public float[] seconds; }
    public sealed class MechanicalCrabController : MonoBehaviour, IZoneRuntimeInitializable, IEncounterResettable
    {
        public CrabDirectionFrames[] directions;
        public Rect room;
        public SpriteRenderer visual;
        public EncounterContext encounter;
        private Rigidbody2D body;
        private ZoneRuntimeContext runtime;
        private Vector2 origin, facing=Vector2.down;
        private float clock, cooldown;
        private int direction, frame, patrol;
        private bool attacking, impacted, moving;
        private readonly RaycastHit2D[] hits=new RaycastHit2D[12];
        public bool IsAttacking=>attacking;
        public const int ImpactFrame=6;
        public event Action GroundSlam;
        public float animationSpeed=1.75f;
        private int health=3;
        private float stunnedUntil,nextCanHit;
        private bool defeated;
        public int HitsRemaining=>health;
        public bool CanBeStunned=>!defeated&&isActiveAndEnabled;
        public bool IsStunned=>Time.time<stunnedUntil;
        public void ReceiveMineHit(){if(defeated)return;health=0;defeated=true;attacking=moving=false;gameObject.SetActive(false);}
        public bool ReceiveCanHit()
        {
            if(!CanBeStunned||Time.time<nextCanHit)return false;
            nextCanHit=Time.time+.6f;health--;
            if(health<=0){defeated=true;attacking=moving=false;gameObject.SetActive(false);}
            else SuspendFor(.4f);
            return true;
        }
        public void SuspendFor(float seconds)
        {
            if(!CanBeStunned)return;
            stunnedUntil=Mathf.Max(stunnedUntil,Time.time+seconds);attacking=moving=impacted=false;frame=0;clock=0;
            if(body!=null)body.linearVelocity=Vector2.zero;
        }
        public void Initialize(ZoneRuntimeContext context){runtime=context;body=GetComponent<Rigidbody2D>();origin=body.position;}
        private void FixedUpdate()
        {
            if(body==null||runtime.GameplayMode==null||!runtime.GameplayMode.AllowsMovement)return;
            if(IsStunned||defeated)return;
            var player=runtime.SceneFlow.PlayerBody;
            if(attacking){moving=false;return;}
            Vector2[] corners={new Vector2(room.xMin+.8f,room.yMin+.8f),new Vector2(room.xMax-.8f,room.yMin+.8f),new Vector2(room.xMax-.8f,room.yMax-.8f),new Vector2(room.xMin+.8f,room.yMax-.8f)};
            Vector2 destination=corners[patrol];
            bool sees=player!=null&&room.Contains(player.position)&&DetectionSensor2D.HasLineOfSight(body.position,player.position,~0,transform,player.transform);
            if(sees)destination=player.position;
            Vector2 delta=destination-body.position;
            if(sees&&delta.magnitude<1.65f&&Time.time>=cooldown){facing=delta.normalized;SetDirection();attacking=true;impacted=false;clock=0;frame=0;moving=false;return;}
            if(delta.magnitude<.2f){patrol=(patrol+1)%4;moving=false;return;}
            facing=delta.normalized;SetDirection();float distance=Mathf.Min(delta.magnitude,(sees?1.25f:.65f)*Time.fixedDeltaTime);
            var filter=ContactFilter2D.noFilter;filter.useTriggers=false;
            int count=body.Cast(facing,filter,hits,distance+.04f);
            for(int i=0;i<count;i++)if(hits[i].rigidbody!=body)distance=Mathf.Min(distance,Mathf.Max(0,hits[i].distance-.04f));
            moving=distance>.001f;if(moving)body.MovePosition(body.position+facing*distance);else if(!sees)patrol=(patrol+1)%4;
        }
        void SetDirection(){direction=Mathf.Abs(facing.x)>Mathf.Abs(facing.y)?(facing.x>0?2:1):(facing.y>0?3:0);}
        private void Update()
        {
            if(body==null||runtime.GameplayMode==null||!runtime.GameplayMode.AllowsMovement||directions==null||directions.Length!=4)return;
            visual.color=IsStunned?new Color(.45f,.85f,1):Color.white;
            if(IsStunned){visual.sprite=directions[direction].frames[0];return;}
            var sequence=directions[direction];int start=attacking?24:moving?36:0;int length=attacking?12:moving?8:4;
            clock+=Time.deltaTime*animationSpeed;
            int index=start+Mathf.Clamp(frame,0,length-1);
            while(clock>=sequence.seconds[index])
            {
                clock-=sequence.seconds[index];frame++;
                if(attacking&&frame>=ImpactFrame&&!impacted){impacted=true;Impact();}
                if(frame>=length){frame=0;if(attacking){attacking=false;cooldown=Time.time+1.4f;clock=0;break;}}
                index=start+Mathf.Clamp(frame,0,length-1);
            }
            int render=(attacking?24:moving?36:0)+Mathf.Clamp(frame,0,attacking?11:moving?7:3);
            visual.sprite=sequence.frames[render];visual.sortingOrder=WorldYSort2D.Order(transform.position.y);
        }
        private void Impact()
        {
            GroundSlam?.Invoke();
            Vector2 center=(Vector2)transform.position+facing*.55f;
            if(Application.isPlaying)EscapeUNIFRANZ.Interaction.InteractionFx.Puff(center,new Color(.85f,.35f,.15f));
            EscapeUNIFRANZ.Narrative.ProjectAudioHooks.EmitAt(EscapeUNIFRANZ.Narrative.AudioCue.CanImpact,center);
            var player=runtime.SceneFlow.PlayerBody;
            if(player!=null&&Vector2.Distance(player.position,center)<=1.2f&&DetectionSensor2D.HasLineOfSight(center,player.position,~0,transform,player.transform))
                encounter.RequestContactDamage("Te alcanzó el golpe al suelo del cangrejo mecánico.",out _);
        }
        public void ResetEncounter(){defeated=false;health=3;stunnedUntil=nextCanHit=0;gameObject.SetActive(true);if(visual!=null)visual.color=Color.white;if(body!=null){body.position=origin;body.linearVelocity=Vector2.zero;}attacking=impacted=moving=false;clock=cooldown=0;frame=patrol=0;}
    }
}
