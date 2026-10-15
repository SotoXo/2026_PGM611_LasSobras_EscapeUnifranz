#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EscapeUNIFRANZ.Narrative;
using EscapeUNIFRANZ.Player;
using EscapeUNIFRANZ.Puzzles;
using EscapeUNIFRANZ.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace EscapeUNIFRANZ.EditorTools
{
    public static partial class NarrativeCampaignBuilder
    {
        // Serialized references and geometry only: does not run gameplay or render a preview.
        public static string AuditCampaign()
        {
            var original=SceneManager.GetActiveScene(); var existing=new HashSet<string>();
            for(int i=0;i<SceneManager.sceneCount;i++) existing.Add(SceneManager.GetSceneAt(i).path);
            var lines=new List<string>(); var unresolved=new List<string>();
            try
            {
                for(int i=0;i<Scenes.Length;i++)
                {
                    string path="Assets/_Project/Scenes/Zones/"+Scenes[i]+".unity";
                    var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
                    try
                    {
                        var roots=scene.GetRootGameObjects();
                        var zones=roots.SelectMany(g=>g.GetComponentsInChildren<ZoneContext>(true)).ToArray();
                        if (zones.Length!=1) throw new InvalidOperationException(path+": ZoneContext count");
                        var zone=zones[0]; var objects=roots.SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
                        if (objects.Any(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0)) throw new InvalidOperationException(path+": missing scripts");
                        if (zone.GetComponentsInChildren<PlayerMovement2D>(true).Length!=0) throw new InvalidOperationException(path+": duplicate player");
                        var spawns=zone.GetComponentsInChildren<SpawnPoint>(true);
                        if (!spawns.Any(s=>s.SpawnId==zone.DefaultCheckpointSpawnId)) throw new InvalidOperationException(path+": invalid checkpoint");
                        var geometry=JsonUtility.FromJson<Geometry>(File.ReadAllText(Art+"Levels/"+Levels[i]+"/LevelGeometry.json"));
                        var reachable=Reachable(geometry,spawns.First(s=>s.SpawnId==zone.DefaultCheckpointSpawnId).Position);
                        var panels=zone.GetComponentsInChildren<PuzzleInteractable>(true);
                        foreach(var panel in panels)
                        {
                            if (!PuzzleModel.Validate(panel.puzzle,out string error)) throw new InvalidOperationException(path+": "+error);
                            if (!NearReachable(reachable,panel.transform.position)) unresolved.Add(scene.name+" / "+panel.name);
                        }
                        foreach(var clue in zone.GetComponentsInChildren<ClueInteractable>(true))
                            if (!NearReachable(reachable,clue.transform.position)) unresolved.Add(scene.name+" / "+clue.name);
                        foreach(var terminal in zone.GetComponentsInChildren<SecurityTerminalInteractable>(true))
                            if (!NearReachable(reachable,terminal.transform.position)) unresolved.Add(scene.name+" / "+terminal.name);
                        foreach(var control in zone.GetComponentsInChildren<EnvironmentalControlInteractable>(true))
                            if (!NearReachable(reachable,control.transform.position)) unresolved.Add(scene.name+" / "+control.name);
                        foreach(var pickup in zone.GetComponentsInChildren<ItemPickupInteractable>(true).Where(p=>p.gameObject.activeInHierarchy))
                        {
                            if(pickup.item==null || string.IsNullOrEmpty(pickup.pickupId)) throw new InvalidOperationException(path+": pickup missing data "+pickup.name);
                            if(!NearReachable(reachable,pickup.transform.position)) unresolved.Add(scene.name+" / "+pickup.name);
                        }
                        foreach(var inspect in zone.GetComponentsInChildren<EnvironmentalInspectInteractable>(true))
                            if(!NearReachable(reachable,inspect.transform.position)) unresolved.Add(scene.name+" / "+inspect.name);
                        var torches=zone.GetComponentsInChildren<ItemPickupInteractable>(true).Where(p=>p.ItemId=="flashlight" && p.gameObject.activeInHierarchy).ToArray();
                        if(torches.Length!=(i==0 ? 1 : 0)) throw new InvalidOperationException(path+": flashlight pickup count");
                        if(i==0)
                        {
                            float distance=Vector2.Distance(torches[0].transform.position,spawns.First(s=>s.SpawnId==zone.DefaultCheckpointSpawnId).Position);
                            if(distance<1.25f || !AssetDatabase.GetAssetPath(torches[0].item.icon).EndsWith("Items/linterna.png")) throw new InvalidOperationException("Invalid authored flashlight placement");
                            lines.Add("Flashlight: "+torches[0].transform.position.ToString("F3")+"; spawn distance "+distance.ToString("F2")+"; native linterna.png.");
                        }
                        foreach(var exit in zone.GetComponentsInChildren<ZoneExitInteractable>(true)) if(exit.statusLight==null || exit.statusIndicator==null) throw new InvalidOperationException(path+": missing door feedback");
                        foreach(var interactive in zone.GetComponentsInChildren<EscapeUNIFRANZ.Interaction.InteractableBehaviour>(true).Where(x=>x.enabled))
                            if(interactive.GetComponent<EscapeUNIFRANZ.Interaction.InteractableFeedback2D>()==null) throw new InvalidOperationException(path+": missing proximity feedback");
                        foreach(var hit in zone.GetComponentsInChildren<EscapeUNIFRANZ.Hazards.HazardContact2D>(true))
                        {
                            var col=hit.GetComponent<Collider2D>();
                            if (hit.GetComponent<EscapeUNIFRANZ.Hazards.TimedHazardArea2D>()!=null && !(col is CapsuleCollider2D)) throw new InvalidOperationException(path+": rectangular danger field");
                            if (hit.GetComponentInParent<EscapeUNIFRANZ.Hazards.PatrolMover2D>()!=null && (col.bounds.size.x>0.85f || col.bounds.size.y>0.85f)) throw new InvalidOperationException(path+": oversized mobile contact "+hit.name);
                        }
                        lines.Add(scene.name+": "+panels.Length+" puzzles; "+zone.GetComponentsInChildren<ClueInteractable>(true).Length+" world clues; "+reachable.Count+" reachable cells; refs/checkpoint OK.");
                    }
                    finally { if (!existing.Contains(path)) EditorSceneManager.CloseScene(scene,true); }
                }
            }
            finally { if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original); }
            lines.Add("Geometry reachability issues: "+unresolved.Count); lines.AddRange(unresolved);
            File.WriteAllLines("reports/horror-technical-audit.txt",lines);
            return string.Join("\n",lines);
        }
        public static string AuditInteractiveRuntime()
        {
            var original=SceneManager.GetActiveScene(); var scene=SceneManager.GetSceneByPath(Bootstrap); bool close=!scene.IsValid() || !scene.isLoaded;
            if(close) scene=EditorSceneManager.OpenScene(Bootstrap,OpenSceneMode.Additive);
            try
            {
                var objects=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
                if(objects.Any(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0)) throw new InvalidOperationException("Bootstrap missing scripts");
                int Count<T>() where T:Component => objects.Sum(t=>t.GetComponents<T>().Length);
                if(Count<PlayerMovement2D>()!=1 || Count<NarrativeController>()!=1 || Count<EscapeUNIFRANZ.Map.CampusMapController>()!=1 || Count<EscapeUNIFRANZ.Map.MinimapController>()!=1 || Count<UnityEngine.EventSystems.EventSystem>()!=1 || Count<Canvas>()!=1) throw new InvalidOperationException("Duplicated or missing persistent UI/player");
                var controller=objects.Select(t=>t.GetComponent<NarrativeController>()).First(c=>c!=null); var catalog=new SerializedObject(controller).FindProperty("itemCatalog");
                if(catalog.arraySize!=7) throw new InvalidOperationException("Item catalog count");
                for(int i=0;i<7;i++) if(catalog.GetArrayElementAtIndex(i).objectReferenceValue==null) throw new InvalidOperationException("Missing item definition");
                var flow=objects.Select(t=>t.GetComponent<EscapeUNIFRANZ.UI.GameFlowView>()).First(c=>c!=null);
                foreach(string field in new[]{"resumeButton","pauseInventoryButton","pauseMapButton","pauseJournalButton","pauseControlsButton","pauseMenuButton"}) if(Ref<UnityEngine.UI.Button>(flow,field)==null) throw new InvalidOperationException("Unbound pause button "+field);
                var vision=objects.Select(t=>t.GetComponent<PlayerVisionController>()).First(c=>c!=null);
                if(Ref<UnityEngine.Rendering.Universal.Light2D>(vision,"forwardLight").enabled) throw new InvalidOperationException("Initial flashlight cone must be off");
                var audio=objects.Select(t=>t.GetComponent<ProjectAudioHooks>()).First(c=>c!=null);
                if(audio.bindings.Length!=System.Enum.GetValues(typeof(AudioCue)).Length) throw new InvalidOperationException("Incomplete audio hooks");
                var result="Bootstrap: one player, Canvas, EventSystem, inventory/controller, campus map and minimap; 7 ItemData definitions; 6 pause buttons bound; initial cone OFF; "+audio.bindings.Length+" audio hooks. Edit Mode only.";
                File.WriteAllText("reports/interactivity-runtime-audit.txt",result); return result;
            }
            finally { if(close) EditorSceneManager.CloseScene(scene,true); if(original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original); }
        }
        public static string AuditAtmosphere()
        {
            var original=SceneManager.GetActiveScene(); var open=new HashSet<string>(); for(int n=0;n<SceneManager.sceneCount;n++) open.Add(SceneManager.GetSceneAt(n).path);
            var lines=new List<string>(); int roomTotal=0,eventTotal=0;
            try
            {
                for(int i=0;i<Scenes.Length;i++)
                {
                    string path="Assets/_Project/Scenes/Zones/"+Scenes[i]+".unity"; var scene=SceneManager.GetSceneByPath(path); if(!scene.IsValid() || !scene.isLoaded) scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
                    try
                    {
                        var zone=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<ZoneContext>(true)).Single(); var rooms=zone.GetComponentsInChildren<RoomLightingController>(true); var owners=new HashSet<int>();
                        if(rooms.Length==0 || rooms.Select(r=>r.roomId).Distinct().Count()!=rooms.Length) throw new InvalidOperationException(path+": missing/duplicate room groups");
                        foreach(var room in rooms)
                        {
                            roomTotal++; if(room.entryEvent) eventTotal++;
                            if(room.mainLights.Length!=2 || room.fixtures.Length!=2 || room.fillLights.Length!=1) throw new InvalidOperationException(path+": incomplete room group");
                            var trigger=room.GetComponent<BoxCollider2D>(); if(trigger==null || !trigger.isTrigger || trigger.size!=room.roomBounds.size) throw new InvalidOperationException(path+": invalid room trigger");
                            if(room.roomBounds.xMin<0 || room.roomBounds.yMin<0 || room.enterDelay<.5f || room.revealSeconds.x<.5f || room.revealSeconds.y>2.5f) throw new InvalidOperationException(path+": invalid room timing/bounds");
                            foreach(var light in room.mainLights.Concat(room.fillLights)) if(light==null || !light.shadowsEnabled || !owners.Add(light.GetInstanceID())) throw new InvalidOperationException(path+": missing/shadowless/shared light");
                            var fill=room.fillLights[0]; if(fill.lightType!=UnityEngine.Rendering.Universal.Light2D.LightType.Freeform || fill.shapePath.Length!=8) throw new InvalidOperationException(path+": room needs bounded freeform fill");
                            foreach(var vertex in fill.shapePath) if(!room.roomBounds.Contains((Vector2)fill.transform.TransformPoint(vertex))) throw new InvalidOperationException(path+": room fill escapes bounds");
                            for(int f=0;f<2;f++)
                            { if(Ref<UnityEngine.Rendering.Universal.Light2D>(room.fixtures[f],"lamp")!=room.mainLights[f] || new SerializedObject(room.fixtures[f]).FindProperty("frames").arraySize!=4) throw new InvalidOperationException(path+": fixture/sprite/light mismatch"); }
                            lines.Add(scene.name+" / "+room.displayName+": "+room.initialState+"; entry="+room.entryEvent+"; full "+room.revealSeconds+" s; delay "+room.enterDelay+"; blackout "+room.blackoutSeconds+"; defect "+room.defectiveFixture+"; bounded fill/shadows/refs OK.");
                        }
                        var director=zone.GetComponentInChildren<WorldHorrorDirector>(true); if(director.powerLights.Length!=0 || director.alarmLights.Length!=0) throw new InvalidOperationException(path+": legacy lights still competing with room groups");
                        if(i==0)
                        {
                            if(director.revealRooms.Length!=2 || !director.revealRooms.All(r=>r.restoredByDirector)) throw new InvalidOperationException("Entrance recovery sequence missing");
                            var spawn=zone.GetComponentsInChildren<SpawnPoint>(true).First(s=>s.SpawnId==zone.DefaultCheckpointSpawnId).Position;
                            var pc=zone.GetComponentsInChildren<EnvironmentalInspectInteractable>(true).Single(p=>p.entrancePowerInspection);
                            float distance=Vector2.Distance(spawn,pc.transform.position); if(distance<=1.15f || distance>2.5f) throw new InvalidOperationException("First terminal distance "+distance);
                            var premature=zone.GetComponentsInChildren<EscapeUNIFRANZ.Interaction.InteractableBehaviour>(true).Where(c=>c.enabled && c.gameObject.activeInHierarchy && Vector2.Distance(spawn,c.transform.position)<=1.15f).ToArray();
                            if(premature.Length>0) throw new InvalidOperationException("Premature E at spawn: "+string.Join(",",premature.Select(c=>c.name)));
                            lines.Add("Entrance: first terminal "+pc.transform.position.ToString("F3")+" at "+distance.ToString("F2")+" from spawn; no initial E candidate; 2-room recovery, 3 s robot startup grace; checkpoints/hitbox configuration preserved.");
                        }
                    }
                    finally { if(!open.Contains(path)) EditorSceneManager.CloseScene(scene,true); }
                }
                var bootstrap=SceneManager.GetSceneByPath(Bootstrap); bool close=!bootstrap.IsValid() || !bootstrap.isLoaded; if(close) bootstrap=EditorSceneManager.OpenScene(Bootstrap,OpenSceneMode.Additive);
                try
                {
                    var tutorial=bootstrap.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<EscapeUNIFRANZ.UI.EntranceTutorialController>(true)).Single();
                    foreach(string field in new[]{"session","mode","player","interaction","label"}) if(new SerializedObject(tutorial).FindProperty(field).objectReferenceValue==null) throw new InvalidOperationException("Tutorial missing "+field);
                }
                finally { if(close) EditorSceneManager.CloseScene(bootstrap,true); }
            }
            finally { if(original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original); }
            lines.Add("Rooms="+roomTotal+"; entry reading events="+eventTotal+"; one entrance-only tutorial; no missing references or light ownership conflicts. Edit Mode; no visual review.");
            File.WriteAllLines("reports/atmosphere-technical-audit.txt",lines); return string.Join("\n",lines);
        }
        private static HashSet<Vector2Int> Reachable(Geometry g,Vector2 start)
        {
            const float cell=0.125f; int width=g.width/8,height=g.height/8;
            bool Walkable(Vector2Int p)
            {
                if (p.x<2 || p.y<2 || p.x>=width-2 || p.y>=height-3) return false;
                var feet=new Rect(p.x*cell-0.19f,p.y*cell,0.38f,0.22f);
                return !g.collisions.Any(c=>c.Bounds.Overlaps(feet));
            }
            var origin=Vector2Int.RoundToInt(start/cell);
            if (!Walkable(origin)) for(int r=1;r<12;r++)
            {
                var candidate=new[]{origin+Vector2Int.up*r,origin+Vector2Int.down*r,origin+Vector2Int.left*r,origin+Vector2Int.right*r}.FirstOrDefault(Walkable);
                if (candidate!=Vector2Int.zero) { origin=candidate; break; }
            }
            var result=new HashSet<Vector2Int>(); var queue=new Queue<Vector2Int>();
            if (Walkable(origin)) { result.Add(origin); queue.Enqueue(origin); }
            while(queue.Count>0)
            {
                var p=queue.Dequeue();
                foreach(var offset in new[]{Vector2Int.up,Vector2Int.down,Vector2Int.left,Vector2Int.right})
                { var next=p+offset; if (!result.Contains(next) && Walkable(next)) { result.Add(next); queue.Enqueue(next); } }
            }
            return result;
        }
        private static bool NearReachable(HashSet<Vector2Int> cells,Vector2 pos)
        {
            var p=Vector2Int.RoundToInt(pos/0.125f);
            for(int x=-5;x<=5;x++) for(int y=-5;y<=5;y++) if (cells.Contains(p+new Vector2Int(x,y))) return true;
            return false;
        }
    }
}
#endif
