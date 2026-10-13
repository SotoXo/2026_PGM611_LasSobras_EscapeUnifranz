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
