using EscapeUNIFRANZ.Encounters;
using UnityEngine;
using System.Linq;
namespace EscapeUNIFRANZ.World
{
    public sealed class CrabRoomRupture : MonoBehaviour,IZoneRuntimeInitializable
    {
        public Sprite[] frames;public float[] durations;public SpriteRenderer visual;public MechanicalCrabController crab;
        public MechanicalCrabController[] crabs;
        public BoxCollider2D[] middleHoles,finalHoles;
        private int collisionStage=-1;
        private ZoneRuntimeContext runtime;private int frame,hits,target;private float elapsed;private bool playing;
        public int CurrentFrame=>frame;
        public int GroundHits=>hits;
        const string Key="piso3_ruptura_15_golpes";
        public static int StageFrame(int count)=>count>=45?23:count>=30?15:count>=15?7:0;
        public void Initialize(ZoneRuntimeContext context)
        {
            runtime=context;
            if(crabs==null||crabs.Length==0)crabs=new[]{crab};
            foreach(var actor in crabs)if(actor!=null)actor.GroundSlam+=Trigger;
            hits=0;for(int i=1;i<=45;i++)if(context.Session.State.IsWorldItemDiscovered("room_damage",Key+i))hits=i;
            frame=target=StageFrame(hits);visual.sprite=frames[frame];
            RefreshObstacles();
        }
        public void Trigger()
        {
            if(hits>=45)return;
            hits++;runtime.Session?.State.DiscoverWorldItem("room_damage",Key+hits);
            target=StageFrame(hits);
            if(target<=frame)return;
            if(!playing){frame++;elapsed=0;visual.sprite=frames[frame];}
            playing=true;
        }
        private void Update()
        {
            if(!playing||runtime.GameplayMode==null||!runtime.GameplayMode.AllowsMovement)return;
            elapsed+=Time.deltaTime;
            while(playing&&elapsed>=durations[frame])
            {
                elapsed-=durations[frame];
                if(frame>=target){playing=false;break;}
                frame++;visual.sprite=frames[frame];RefreshObstacles();
            }
        }
        public void RefreshObstacles()
        {
            int stage=frame>=23?2:frame>=15?1:0;if(collisionStage==stage)return;collisionStage=stage;
            if(middleHoles!=null)foreach(var hole in middleHoles)hole.enabled=stage==1;
            if(finalHoles!=null)foreach(var hole in finalHoles)hole.enabled=stage==2;
            Physics2D.SyncTransforms();
            if(Application.isPlaying&&stage>0)MoveOccupants(stage==1?middleHoles:finalHoles);
        }
        private void MoveOccupants(BoxCollider2D[] holes)
        {
            if(holes==null)return;
            var filter=ContactFilter2D.noFilter;filter.useTriggers=false;var obstacles=new System.Collections.Generic.List<Collider2D>();
            foreach(var body in FindObjectsByType<Rigidbody2D>(FindObjectsSortMode.None))
            {
                if(body.bodyType==RigidbodyType2D.Static||(body.gameObject.scene!=gameObject.scene&&body!=runtime.SceneFlow?.PlayerBody))continue;
                var feet=body.GetComponents<Collider2D>().FirstOrDefault(c=>c.enabled&&!c.isTrigger);if(feet==null)continue;
                foreach(var hole in holes)
                {
                    if(!feet.Distance(hole).isOverlapped)continue;
                    Vector2 p=body.position,offset=(Vector2)feet.bounds.center-p,extent=feet.bounds.extents;
                    Vector2[] candidates={new Vector2(hole.bounds.min.x-extent.x-offset.x-.04f,p.y),new Vector2(hole.bounds.max.x+extent.x-offset.x+.04f,p.y),new Vector2(p.x,hole.bounds.min.y-extent.y-offset.y-.04f),new Vector2(p.x,hole.bounds.max.y+extent.y-offset.y+.04f)};
                    foreach(var candidate in candidates.OrderBy(c=>(c-p).sqrMagnitude))
                    {
                        obstacles.Clear();Physics2D.OverlapBox(candidate+offset,(Vector2)feet.bounds.size+Vector2.one*.02f,0,filter,obstacles);
                        if(obstacles.Any(c=>c.attachedRigidbody!=body))continue;
                        body.position=candidate;body.linearVelocity=Vector2.zero;Physics2D.SyncTransforms();break;
                    }
                }
            }
        }
        private void OnDestroy(){if(crabs!=null)foreach(var actor in crabs)if(actor!=null)actor.GroundSlam-=Trigger;}
    }
}
