using System.Linq;
using EscapeUNIFRANZ.Encounters;
using EscapeUNIFRANZ.Hazards;
using UnityEngine;
namespace EscapeUNIFRANZ.World
{
    public enum UpperActorRole { Fox,Rescue,Camera }
    public sealed class UpperFloorActor : MonoBehaviour,IZoneRuntimeInitializable
    {
        public UpperActorRole role;public SpriteRenderer visual;public Sprite[] frames;public float[] durations;
        public MechanicalCrabController[] rescueTargets;public SecurityCamera2D cameraUnit;public float safeRadius=6;
        private ZoneRuntimeContext runtime;private int frame;private float clock;private bool started,finished;
        public bool Released=>role==UpperActorRole.Rescue&&started;
        public void Initialize(ZoneRuntimeContext context)
        {
            runtime=context;
            if(role==UpperActorRole.Fox){finished=context.Session.State.IsWorldItemDiscovered("story","piso5_fox_once");visual.enabled=false;}
            if(role==UpperActorRole.Rescue){started=context.Session.State.IsWorldItemDiscovered("story","piso3_rescued");frame=started?36:0;visual.sprite=frames[frame];}
        }
        public bool HasNearbyEnemy()=>runtime.Zone.GetComponentsInChildren<MiniAutoController>().Any(r=>r.isActiveAndEnabled&&Vector2.Distance(r.transform.position,transform.position)<safeRadius)||runtime.Zone.GetComponentsInChildren<MechanicalCrabController>().Any(r=>r.isActiveAndEnabled&&Vector2.Distance(r.transform.position,transform.position)<safeRadius);
        private void Update()
        {
            if(runtime.GameplayMode==null||!runtime.GameplayMode.AllowsMovement)return;
            if(role==UpperActorRole.Fox)
            {
                if(finished)return;
                if(!started)
                {
                    if(runtime.SceneFlow?.PlayerBody==null||Vector2.Distance(runtime.SceneFlow.PlayerBody.position,transform.position)>2.5f||HasNearbyEnemy())return;
                    if(!DetectionSensor2D.HasLineOfSight(runtime.SceneFlow.PlayerBody.position,transform.position,~0,runtime.SceneFlow.PlayerBody.transform,transform))return;
                    if(!runtime.Session.State.DiscoverWorldItem("story","piso5_fox_once")){finished=true;return;}
                    started=true;visual.enabled=true;
                }
                if(HasNearbyEnemy()){finished=true;visual.enabled=false;return;}
            }
            if(role==UpperActorRole.Rescue&&!started&&rescueTargets!=null&&rescueTargets.Length>0&&rescueTargets.All(c=>c==null||c.HitsRemaining<=0))
            {started=true;frame=6;clock=0;runtime.Session.State.DiscoverWorldItem("story","piso3_rescued");runtime.Session.ShowMessage("Has derrotado a los cangrejos. La persona se está liberando.");}
            if(role==UpperActorRole.Camera)
            {
                int begin=cameraUnit.sensor.HasTarget?20:0,end=cameraUnit.sensor.HasTarget?27:19;
                if(frame<begin||frame>end){frame=begin;clock=0;}
            }
            clock+=Time.deltaTime;
            while(clock>=durations[frame])
            {
                clock-=durations[frame];frame++;
                if(role==UpperActorRole.Fox&&frame>=frames.Length){finished=true;visual.enabled=false;return;}
                if(role==UpperActorRole.Rescue){if(!started&&frame>5)frame=0;else if(started&&frame>=48)frame=36;}
                if(role==UpperActorRole.Camera){int begin=cameraUnit.sensor.HasTarget?20:0,end=cameraUnit.sensor.HasTarget?27:19;if(frame>end)frame=begin;}
            }
            visual.sprite=frames[frame];visual.sortingOrder=WorldYSort2D.Order(transform.position.y);
        }
    }
}
