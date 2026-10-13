#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.Encounters;
using EscapeUNIFRANZ.Hazards;
using EscapeUNIFRANZ.Interaction;
using EscapeUNIFRANZ.Map;
using EscapeUNIFRANZ.Narrative;
using EscapeUNIFRANZ.Player;
using EscapeUNIFRANZ.Puzzles;
using EscapeUNIFRANZ.UI;
using EscapeUNIFRANZ.World;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace EscapeUNIFRANZ.EditorTools
{
    public static partial class NarrativeCampaignBuilder
    {
        private const string Art="Assets/_Project/Art/";
        private const string Bootstrap="Assets/_Project/Scenes/Bootstrap/Bootstrap.unity";
        private static readonly string[] Scenes={"Z00_Entrada","Z01_Hall","Z02_ARCA","Z03_Piso2","Z04_Piso3","Z05_LabSoftware","Z06_NucleoIA"};
        private static readonly string[] Levels={"nivel1_planta_baja","nivel1_planta_baja","nivel1_planta_baja","nivel2_piso1_cocina_simulacion","nivel3_tecnologia","nivel3_tecnologia","nivel4_aulas"};
        private static HashSet<string> loaded;
        private static Material lit;
        private static List<string> placement;
        private static HashSet<Vector2Int> accessibleCells;
        [Serializable] private class Solid { public int x,y,w,h; public Rect Bounds=>new Rect(x/64f,y/64f,w/64f,h/64f); }
        [Serializable] private class Geometry { public int width,height; public Solid[] collisions; }

        [MenuItem("Escape UNIFRANZ/Narrative/Build Campaign (No Play Mode)")]
        public static void BuildAll()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Build narrative only in Edit Mode.");
            var original=SceneManager.GetActiveScene(); loaded=new HashSet<string>();
            for(int i=0;i<SceneManager.sceneCount;i++)
            {
                var scene=SceneManager.GetSceneAt(i); loaded.Add(scene.path);
                if (scene.isDirty && scene.path!="Assets/_Project/Scenes/UI/MainMenu.unity")
                    throw new InvalidOperationException("Save affected scene changes first: "+scene.path);
            }
            lit=AssetDatabase.LoadAssetAtPath<Material>(Art+"Levels/GroupWorldLit.mat");
            if (lit==null) throw new InvalidOperationException("Integrate the real group art before building the campaign.");
            placement=new List<string>();
            try
            {
                BuildData(); BuildRuntimeUI();
                for(int i=0;i<Scenes.Length;i++) BuildZone(i);
                AssetDatabase.SaveAssets(); Directory.CreateDirectory("reports");
                File.WriteAllLines("reports/narrative-placement.txt",placement);
                Debug.Log("Narrative campaign saved: "+clues.Count+" clues, "+puzzles.Count+" puzzles, key item backtracking, CCTV, staged threat and world reactions. No Play Mode or visual review.");
            }
            finally { if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original); }
        }
        private static Scene Open(string path)
        { var scene=SceneManager.GetSceneByPath(path); if (!scene.IsValid() || !scene.isLoaded) scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive); SceneManager.SetActiveScene(scene); return scene; }
        private static void Save(Scene scene)
        { EditorSceneManager.MarkSceneDirty(scene); if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save "+scene.path); if (!loaded.Contains(scene.path)) EditorSceneManager.CloseScene(scene,true); }

        private static void BuildRuntimeUI()
        {
            var scene=Open(Bootstrap);
            var runtime=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<GameRuntimeRoot>(true)).First();
            var session=runtime.GetComponent<GameSessionController>(); var mode=runtime.GetComponent<GameplayModeController>();
            var controller=Ensure<NarrativeController>(runtime.gameObject);
            BuildAudioCatalog(runtime.gameObject);
            Transform ui=runtime.GetComponentsInChildren<Canvas>(true).First().transform;
            var previous=ui.Find("NarrativeUI"); if (previous!=null) Object.DestroyImmediate(previous.gameObject);
            var holder=Rect(ui,"NarrativeUI",Vector2.zero,Vector2.zero); Stretch(holder);
            var view=holder.gameObject.AddComponent<NarrativePanelView>();
            var overlay=Image(holder,"Overlay",new Color(0.005f,0.015f,0.025f,0.88f),Vector2.zero,Vector2.zero); Stretch(overlay); overlay.GetComponent<Image>().raycastTarget=true;
            var card=Image(overlay,"TerminalPanel",new Color(0.018f,0.035f,0.05f,1f),Vector2.zero,new Vector2(940f,540f));
            var title=Text(card,"Title",new Vector2(0f,224f),new Vector2(850f,48f),27f);
            var body=Text(card,"Body",new Vector2(0f,82f),new Vector2(850f,228f),22f); body.alignment=TextAlignmentOptions.TopLeft;
            var wireRoot=Rect(card,"Wires",new Vector2(0f,-70f),new Vector2(900f,210f));
            var ports=Rect(card,"Ports",new Vector2(0f,-70f),new Vector2(900f,210f));
            var feedback=Text(card,"Feedback",new Vector2(0f,-186f),new Vector2(850f,42f),17f);
            var close=Button(card,"Close","CERRAR / ESC",new Vector2(260f,-235f));
            var reset=Button(card,"Reset","REINICIAR PANEL",new Vector2(-260f,-235f));
            var hint=Button(card,"Hint","REVISAR PISTA",new Vector2(0f,-235f));
            Set(view,"overlay",overlay.gameObject); Set(view,"title",title); Set(view,"body",body); Set(view,"feedback",feedback);
            Set(view,"wiresRoot",wireRoot); Set(view,"portsRoot",ports); Set(view,"closeButton",close); Set(view,"resetButton",reset);
            Set(view,"hintButton",hint);
            Set(controller,"session",session); Set(controller,"mode",mode); Set(controller,"view",view);
            SetObjectArray(controller,"journalCatalog",clues.Values.ToArray());
            BuildGlitch(runtime,ui);
            var vision=runtime.GetComponentInChildren<PlayerVisionController>(true); Set(controller,"vision",vision); vision.ApplyLightConfiguration();
            var movement=vision.GetComponent<PlayerMovement2D>();
            var steps=vision.transform.Find("FootstepAudio"); if (steps==null) { steps=new GameObject("FootstepAudio").transform; steps.SetParent(vision.transform,false); }
            var stepAudio=Ensure<AudioSource>(steps.gameObject); stepAudio.playOnAwake=false; Set(movement,"optionalFootsteps",stepAudio);
            var audio=runtime.transform.Find("NarrativeAudio");
            if (audio==null) { audio=new GameObject("NarrativeAudio").transform; audio.SetParent(runtime.transform,false); }
            var source=Ensure<AudioSource>(audio.gameObject); source.playOnAwake=false;
            Set(controller,"audioSource",source);
            overlay.gameObject.SetActive(false); holder.SetAsLastSibling(); Save(scene);
        }
        private static void BuildZone(int index)
        {
            var scene=Open("Assets/_Project/Scenes/Zones/"+Scenes[index]+".unity");
            var zone=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<ZoneContext>(true)).First();
            var old=zone.transform.Find("NarrativeContent"); if (old!=null) Object.DestroyImmediate(old.gameObject);
            var root=new GameObject("NarrativeContent").transform; root.SetParent(zone.transform,false);
            var geometry=JsonUtility.FromJson<Geometry>(File.ReadAllText(Art+"Levels/"+Levels[index]+"/LevelGeometry.json"));
            accessibleCells=Reachable(geometry,zone.GetComponentsInChildren<SpawnPoint>(true).First(s=>s.SpawnId==zone.DefaultCheckpointSpawnId).Position);
            var objectives=root.gameObject.AddComponent<NarrativeZoneObjectives>(); objectives.steps=Objectives(index);
            var level=zone.transform.Find("GroupLevelArt");
            foreach(var item in level.GetComponentsInChildren<SpriteRenderer>(true).Where(s=>s.name.StartsWith("ItemVisual_",StringComparison.Ordinal)))
            {
                var pickup=Ensure<ItemPickupInteractable>(item.gameObject);
                string asset=item.name.Substring("ItemVisual_".Length); pickup.itemId=asset=="bateria" ? "battery" : asset=="llave" ? "key" : "flashlight";
                Vector3 pos=item.transform.localPosition;
                pickup.pickupId=Levels[index]+":"+asset+":"+pos.x.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+":"+pos.y.ToString("F3",System.Globalization.CultureInfo.InvariantCulture);
                var col=Ensure<BoxCollider2D>(item.gameObject); col.isTrigger=true; col.size=new Vector2(0.55f,0.55f); col.offset=new Vector2(item.sprite.bounds.center.x,item.sprite.bounds.center.y);
                Set(pickup,"prompt","[E] Recoger "+ItemPickupInteractable.Label(pickup.itemId)); Track(item.gameObject,"pickup_"+pickup.pickupId,ScanCategory.Interactable);
            }
            foreach(var marker in zone.GetComponentsInChildren<MinimapTrackable2D>(true)) if (marker.ShowAsThreat) Set(marker,"initiallyDiscovered",false);
            ConfigureExits(index,zone);
            switch(index)
            {
                case 0:
                    PlaceClue(root,geometry,"entrada_manual",new Vector2(3.2f,4.3f));
                    var power=NewPuzzle(root,geometry,"power_entrada",new Vector2(6f,3.8f));
                    Effect(root,zone,GameFlagId.PowerEntranceRestored,VisibleRenderer(power));
                    Camera(root,geometry,"Entrada",new Vector2(8f,6f),180f,0.7f);
                    Sequence(root,zone,geometry,"entrada_corredor",new Vector2(10f,3.5f),GameFlagId.PowerEntranceRestored);
                    break;
                case 1:
                    PlaceClue(root,geometry,"hall_admision",new Vector2(5f,9f));
                    Camera(root,geometry,"Hall",new Vector2(21f,10.8f),180f,2.3f);
                    Effect(root,zone,GameFlagId.PowerEntranceRestored,null);
                    break;
                case 2:
                    PlaceClue(root,geometry,"arca_protocolo",new Vector2(4f,9f));
                    var terminal=zone.GetComponentInChildren<ProgressionFlagInteractable>(true);
                    ReplacePuzzle(terminal,puzzles["arca_access"],PuzzleWorldAction.None,null);
                    Effect(root,zone,GameFlagId.ArcaAccessRestored,VisibleRenderer(terminal.gameObject));
                    break;
                case 3:
                    PlaceClue(root,geometry,"hardware_circuito",new Vector2(13.8f,5f));
                    PlaceClue(root,geometry,"hardware_alerta",new Vector2(6f,6f));
                    NewPuzzle(root,geometry,"hardware_routing",new Vector2(14.5f,5f));
                    foreach(var point in zone.GetComponentsInChildren<ExolegOverloadInteractable>(true))
                    { Set(point,"requiredFlags",new[]{GameFlagId.HardwareRoutingRestored}); Set(point,"requirementMessage","El controlador aún no recibe datos válidos. Revisa la ficha y el panel de conexión."); }
                    Camera(root,geometry,"Simulacion",new Vector2(18f,9f),180f,3.4f);
                    Sequence(root,zone,geometry,"simulacion_pasillo",new Vector2(12f,6f),GameFlagId.PowerEntranceRestored);
                    break;
                case 4:
                    PlaceClue(root,geometry,"tecnologia_proyecto",new Vector2(6f,5f));
                    PlaceClue(root,geometry,"tecnologia_francis",new Vector2(9f,6.5f));
                    PlaceClue(root,geometry,"red_topologia",new Vector2(14f,4.5f));
                    var nodes=zone.GetComponentsInChildren<RobotDogNetworkNodeInteractable>(true).OrderBy(n=>n.name).ToArray();
                    var dog=zone.GetComponentInChildren<RobotDogEncounterController>(true);
                    ReplacePuzzle(nodes[0],puzzles["technology_network"],PuzzleWorldAction.RobotDogNetwork,dog);
                    for(int i=1;i<nodes.Length;i++) DisableOld(nodes[i]);
                    Camera(root,geometry,"Tecnologia",new Vector2(22f,4.5f),180f,4.8f);
                    Effect(root,zone,GameFlagId.TechnologyNetworkRestored,VisibleRenderer(nodes[0].gameObject));
                    break;
                case 5:
                    PlaceClue(root,geometry,"software_servicios",new Vector2(4.5f,5f));
                    PlaceClue(root,geometry,"software_escalada",new Vector2(8.5f,7.5f));
                    NewPuzzle(root,geometry,"software_security",new Vector2(10f,6.5f));
                    var panel=zone.GetComponentInChildren<RobotArmsPanelInteractable>(true);
                    Set(panel,"requiredFlags",new[]{GameFlagId.SecurityNodeDisabled}); Set(panel,"requiredItem","lab_card");
                    Set(panel,"requirementMessage","SECURITY sigue enviando órdenes o falta la tarjeta de laboratorio.");
                    Camera(root,geometry,"Laboratorio",new Vector2(7f,8.5f),180f,6.2f);
                    Sequence(root,zone,geometry,"lab_pasillo",new Vector2(20f,2.5f),GameFlagId.TechnologyNetworkRestored);
                    break;
                case 6:
                    PlaceClue(root,geometry,"core_protocolo",new Vector2(13.5f,2.7f));
                    PlaceClue(root,geometry,"core_francis",new Vector2(7f,6f));
                    var boss=zone.GetComponentInChildren<FrancisBossController>(true);
                    var systems=zone.GetComponentsInChildren<FrancisSystemNodeInteractable>(true);
                    foreach(var node in systems)
                    {
                        var so=new SerializedObject(node); int phase=so.FindProperty("phase").intValue, step=so.FindProperty("stepIndex").intValue;
                        if (phase==0 && step==0) ReplacePuzzle(node,puzzles["francis_network"],PuzzleWorldAction.FrancisNetwork,boss);
                        else if (phase==2 && step==0) ReplacePuzzle(node,puzzles["francis_shutdown"],PuzzleWorldAction.FrancisShutdown,boss);
                        else if (phase!=1) DisableOld(node);
                    }
                    var francis=zone.GetComponentsInChildren<PatrolMover2D>(true).First(m=>m.name.Contains("Francis"));
                    var human=francis.transform.Find("GroupVisual").GetComponent<SpriteRenderer>();
                    human.sprite=Sprite(Art+"Characters/Player/Official/Sprite-delanteActual.aseprite"); human.color=new Color(0.9f,0.75f,0.6f); human.transform.localPosition=Vector3.zero;
                    Camera(root,geometry,"Aulas",new Vector2(16f,4.5f),180f,8.1f);
                    break;
            }
            Set(zone,"objectiveText",objectives.steps[0].text); Set(zone,"objectiveId",objectives.steps[0].id); Set(zone,"objectiveTotal",0);
            BuildHorrorZone(index,root,zone,geometry);
            BuildClarityZone(index,root,zone,geometry);
            Save(scene);
        }
        private static void ConfigureExits(int index,ZoneContext zone)
        {
            foreach(var exit in zone.GetComponentsInChildren<ZoneExitInteractable>(true))
            {
                if (index==0 && exit.TargetZoneId=="hall")
                { Set(exit,"requiredFlag",GameFlagId.PowerEntranceRestored); Set(exit,"lockState",DoorLockState.RequiresPuzzle); Set(exit,"lockedMessage","Acceso sin energía. Busca el panel de respaldo."); }
                if (index==1 && exit.TargetZoneId=="arca")
                { Set(exit,"requiredItem","key"); Set(exit,"lockState",DoorLockState.RequiresKey); Set(exit,"requirementMessage","Se requiere la llave del gabinete de control. Revisa el aviso de admisión."); }
                if (index==4 && exit.TargetZoneId=="lab_software")
                { Set(exit,"requiredItem","lab_card"); Set(exit,"requiredFlags",new[]{GameFlagId.TechnologyNetworkRestored}); Set(exit,"lockState",DoorLockState.RequiresCard); Set(exit,"requirementMessage","Se requiere tarjeta de laboratorio y una ruta local de red recuperada."); }
                if (index==5 && exit.TargetZoneId=="nucleo_ia")
                { Set(exit,"requiredFlags",new[]{GameFlagId.SecurityNodeDisabled}); Set(exit,"lockState",DoorLockState.DisabledByAI); Set(exit,"requirementMessage","El nodo de seguridad todavía controla este acceso."); }
            }
        }
        private static NarrativeObjectiveStep[] Objectives(int zone)
        {
            switch(zone)
            {
                case 0:return new[]{Step("entrada_restablecer","Encuentra una forma de restablecer el acceso.",GameFlagId.FirstClueFound),Step("entrada_restablecer","Restablece el panel de acceso.",GameFlagId.PowerEntranceRestored),Step("entrada_cctv","Evita la unidad de inspección y revisa CCTV. Valida el código de acceso.",GameFlagId.EntranceCodeAccepted),Step("entrada_ingresar","Ingresa al Hall por el acceso recuperado.")};
                case 1:return new[]{Step("hall_acceso","Encuentra acceso al control local.",item:"key"),Step("hall_explorar","Restablece el acceso al siguiente sector.",GameFlagId.ArcaAccessRestored),Step("hall_acceder_piso2","Accede al sector de cocina y simulación.")};
                case 2:return new[]{Step("arca_restaurar","Valida el control local de accesos.",GameFlagId.ArcaAccessRestored),Step("arca_regresar","Regresa al Hall; el siguiente acceso está habilitado.")};
                case 3:return new[]{Step("piso2_energia","Encuentra el fusible y ordena el arranque del sector.",GameFlagId.SimulationPowerRestored),Step("piso2_circuito","Diagnostica el dispositivo de simulación.",GameFlagId.HardwareRoutingRestored),Step("piso2_exoleg","Desactiva el dispositivo mediante sus puntos de sobrecarga.",GameFlagId.ExolegsDisabled),Step("piso2_regresar","Regresa al Hall: usa la tarjeta de mantenimiento en el gabinete. Recupera el módulo de red.",GameFlagId.MaintenanceAccessRestored),Step("piso2_avanzar","Encuentra la ruta hacia Tecnología.")};
                case 4:return new[]{Step("tecnologia_red","Investiga los registros y recupera la ruta local.",GameFlagId.TechnologyNetworkRestored),Step("piso3_laboratorio","Accede al laboratorio con la tarjeta emitida por ARCA.")};
                case 5:return new[]{Step("software_servicios","Identifica y detén el servicio comprometido.",GameFlagId.SecurityNodeDisabled),Step("lab_brazos","Desenergiza los brazos durante su ventana segura.",GameFlagId.ArmsDisabled),Step("lab_nucleo","Accede al sector de aulas y al control final.")};
                default:return new[]{Step("nucleo_investigar","Investiga el protocolo de apagado coordinado.",GameFlagId.FrancisEncounterStarted),Step("francis_PhaseNetwork","Aísla la IA de la red.",GameFlagId.FrancisNetworkPhaseComplete),Step("francis_PhaseHardware","Corta alimentación, actuadores y seguridad en orden.",GameFlagId.FrancisHardwarePhaseComplete),Step("francis_PhaseShutdown","Ejecuta el apagado seguro.",GameFlagId.GameCompleted)};
            }
        }
        private static NarrativeObjectiveStep Step(string id,string text,GameFlagId flag=GameFlagId.None,string item="")=>new NarrativeObjectiveStep{id=id,text=text,untilFlag=flag,untilItem=item};
        private static void PlaceClue(Transform root,Geometry geometry,string id,Vector2 pos)
        {
            var obj=WorldObject(root,geometry,"Clue_"+id,pos,Sprite(Art+"Environment/Decals/papeles.png"));
            obj.AddComponent<ClueInteractable>().clue=clues[id]; Set(obj.GetComponent<ClueInteractable>(),"prompt","[E] Leer"); Track(obj,"clue_"+id,ScanCategory.Interactable);
        }
        private static GameObject NewPuzzle(Transform root,Geometry geometry,string id,Vector2 pos)
        {
            var obj=WorldObject(root,geometry,"Panel_"+id,pos,Sprite(Art+"Environment/Props/desk_pc.png"));
            var puzzle=obj.AddComponent<PuzzleInteractable>(); puzzle.puzzle=puzzles[id]; Set(puzzle,"prompt","[E] Usar panel"); Track(obj,"puzzle_"+id,ScanCategory.Terminal); return obj;
        }
        private static void ReplacePuzzle(InteractableBehaviour old,PuzzleData data,PuzzleWorldAction action,MonoBehaviour target)
        {
            old.enabled=false;
            var component=Ensure<PuzzleInteractable>(old.gameObject);
            component.enabled=true; component.puzzle=data; component.worldAction=action; component.actionTarget=target;
            Set(component,"prompt","[E] Usar terminal"); Track(old.gameObject,"puzzle_"+data.id,ScanCategory.Terminal);
        }
        private static void DisableOld(InteractableBehaviour old)
        {
            old.enabled=false;
            var scan=old.GetComponent<Scannable2D>(); if (scan!=null) scan.enabled=false;
            var marker=old.GetComponent<MinimapTrackable2D>(); if (marker!=null) Set(marker,"secret",true);
        }
        private static void Effect(Transform root,ZoneContext zone,GameFlagId flag,SpriteRenderer monitor)
        {
            var effect=new GameObject("StateEffect_"+flag).AddComponent<WorldStateEffect2D>(); effect.transform.SetParent(root,false); effect.flag=flag;
            var lamp=zone.transform.Find("GroupLevelArt").GetComponentsInChildren<Light2D>(true).FirstOrDefault(l=>l.name=="AuthoredLamp_2");
            effect.restoredLights=lamp!=null ? new[]{lamp} : new Light2D[0]; effect.monitors=monitor!=null ? new[]{monitor} : new SpriteRenderer[0];
        }
        private static void Camera(Transform root,Geometry geometry,string name,Vector2 pos,float angle,float phase)
        {
            // No camera sprite was supplied: a cropped monitor from the group's PC art represents its head.
            string path=Art+"Environment/Props/SecurityCameraHead.asset";
            var head=AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (head==null)
            {
                var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"Environment/Props/desk_pc.png");
                head=UnityEngine.Sprite.Create(texture,new Rect(8,52,48,24),new Vector2(0.5f,0f),64f,0,SpriteMeshType.FullRect,Vector4.zero,false); AssetDatabase.CreateAsset(head,path);
            }
            var obj=WorldObject(root,geometry,"SecurityCamera_"+name,pos,head); obj.GetComponent<BoxCollider2D>().enabled=false;
            var sensorObj=new GameObject("DetectionField"); sensorObj.transform.SetParent(obj.transform,false); sensorObj.transform.localRotation=Quaternion.Euler(0f,0f,angle);
            var circle=sensorObj.AddComponent<CircleCollider2D>(); circle.isTrigger=true; circle.radius=5.5f;
            var sensor=sensorObj.AddComponent<DetectionSensor2D>(); Set(sensor,"directional",true); Set(sensor,"fieldOfView",75f);
            var lightObj=new GameObject("CameraFieldLight"); lightObj.transform.SetParent(obj.transform,false); lightObj.transform.localRotation=sensorObj.transform.localRotation;
            var light=lightObj.AddComponent<Light2D>(); light.lightType=Light2D.LightType.Point; light.pointLightOuterRadius=5.5f; light.pointLightInnerRadius=0.2f;
            light.pointLightOuterAngle=75f; light.pointLightInnerAngle=48f; light.intensity=0.13f; light.shadowsEnabled=true; light.shadowIntensity=0.95f;
            light.targetSortingLayers=SortingLayer.layers.Select(l=>l.id).ToArray();
            var camera=obj.AddComponent<SecurityCamera2D>(); camera.sensor=sensor; camera.fieldLight=light; camera.baseAngle=angle; camera.phase=phase;
            if (name=="Aulas") camera.disabledFlag=GameFlagId.FrancisNetworkPhaseComplete;
            camera.alertAudio=obj.AddComponent<AudioSource>(); camera.alertAudio.playOnAwake=false; Track(obj,"camera_"+name,ScanCategory.Sensor,true);
        }
        private static void Sequence(Transform root,ZoneContext zone,Geometry geometry,string id,Vector2 pos,GameFlagId flag)
        {
            var obj=new GameObject("LightSequence_"+id); obj.transform.SetParent(root,false); obj.transform.position=Free(geometry,pos);
            var box=obj.AddComponent<BoxCollider2D>(); box.isTrigger=true; box.size=new Vector2(1.5f,1.5f);
            var sequence=obj.AddComponent<LightSequenceTrigger>(); sequence.eventId=id; sequence.requiredFlag=flag;
            sequence.lights=zone.transform.Find("GroupLevelArt").GetComponentsInChildren<Light2D>(true).Where(l=>l.name=="AuthoredLamp_0" || l.name=="AuthoredLamp_1").OrderBy(l=>l.name).ToArray();
        }
        private static GameObject WorldObject(Transform root,Geometry geometry,string name,Vector2 pos,Sprite sprite)
        {
            var obj=new GameObject(name); obj.transform.SetParent(root,false); obj.transform.position=Free(geometry,pos);
            var sr=obj.AddComponent<SpriteRenderer>(); sr.sprite=sprite; sr.sharedMaterial=lit; sr.sortingLayerName="World"; sr.sortingOrder=WorldYSort2D.Order(obj.transform.position.y);
            var visual=obj.transform;
            // Keep native sprite scale. Center its image around the interaction root using a child.
            var child=new GameObject("Art").transform; child.SetParent(visual,false); child.localPosition=new Vector3(-sprite.bounds.center.x,-sprite.bounds.min.y,0f);
            var actual=child.gameObject.AddComponent<SpriteRenderer>(); actual.sprite=sprite; actual.sharedMaterial=lit; actual.sortingLayerName="World"; actual.sortingOrder=sr.sortingOrder;
            child.gameObject.AddComponent<WorldYSort2D>(); Object.DestroyImmediate(sr);
            var col=obj.AddComponent<BoxCollider2D>(); col.isTrigger=true; col.size=new Vector2(0.65f,0.65f);
            placement.Add(SceneManager.GetActiveScene().name+" / "+name+" = "+obj.transform.position.ToString("F3")); return obj;
        }
        private static Vector2 Free(Geometry geometry,Vector2 pos)
        {
            for(int r=0;r<120;r++) for(int x=-r;x<=r;x++) for(int y=-r;y<=r;y++)
            {
                if (r>0 && Math.Abs(x)!=r && Math.Abs(y)!=r) continue;
                Vector2 p=pos+new Vector2(x,y)/16f;
                if (p.x<0.5f || p.y<0.5f || p.x>geometry.width/64f-0.5f || p.y>geometry.height/64f-0.7f) continue;
                if (accessibleCells!=null && !accessibleCells.Contains(Vector2Int.RoundToInt(p/0.125f))) continue;
                if (!geometry.collisions.Any(c=>c.Bounds.Overlaps(new Rect(p.x-0.3f,p.y-0.03f,0.6f,0.4f)))) return p;
            }
            throw new InvalidOperationException("No free interaction position near "+pos);
        }
        private static void Track(GameObject obj,string id,ScanCategory category,bool threat=false)
        {
            var marker=Ensure<MinimapTrackable2D>(obj); Set(marker,"markerId",id); Set(marker,"initiallyDiscovered",false); Set(marker,"secret",false); Set(marker,"showAsThreat",threat);
            var scan=Ensure<Scannable2D>(obj); scan.enabled=true; Set(scan,"category",category); Set(scan,"shortLabel",category==ScanCategory.Sensor ? "CÁMARA" : category==ScanCategory.Terminal ? "TERMINAL" : "OBJETO");
        }
        private static Sprite Sprite(string path)=>AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s=>s.name).First();
        private static SpriteRenderer VisibleRenderer(GameObject obj)=>obj.GetComponentsInChildren<SpriteRenderer>(true).FirstOrDefault(s=>s.enabled);
        private static T Ensure<T>(GameObject obj) where T:Component
        { var component=obj.GetComponent<T>(); if (component==null) component=obj.AddComponent<T>(); return component; }
        private static RectTransform Rect(Transform root,string name,Vector2 pos,Vector2 size)
        { var obj=new GameObject(name,typeof(RectTransform)); var rect=obj.GetComponent<RectTransform>(); rect.SetParent(root,false); rect.anchoredPosition=pos; rect.sizeDelta=size; return rect; }
        private static RectTransform Image(Transform root,string name,Color color,Vector2 pos,Vector2 size)
        { var rect=Rect(root,name,pos,size); var image=rect.gameObject.AddComponent<Image>(); image.color=color; image.raycastTarget=false; return rect; }
        private static TMP_Text Text(Transform root,string name,Vector2 pos,Vector2 size,float font)
        { var rect=Rect(root,name,pos,size); var text=rect.gameObject.AddComponent<TextMeshProUGUI>(); text.font=PresentationTheme.Font; text.fontSize=font; text.alignment=TextAlignmentOptions.Center; text.raycastTarget=false; text.color=new Color(0.85f,0.94f,0.95f); return text; }
        private static Button Button(Transform root,string name,string label,Vector2 pos)
        { var rect=Image(root,name,new Color(0.08f,0.18f,0.23f),pos,new Vector2(240f,40f)); rect.GetComponent<Image>().raycastTarget=true; var button=rect.gameObject.AddComponent<Button>(); var text=Text(rect,"Label",Vector2.zero,new Vector2(232f,36f),17f); text.text=label; return button; }
        private static void Stretch(RectTransform rect) { rect.anchorMin=Vector2.zero; rect.anchorMax=Vector2.one; rect.offsetMin=Vector2.zero; rect.offsetMax=Vector2.zero; }
        private static void Folder(string path)
        { Directory.CreateDirectory(path); AssetDatabase.Refresh(); }
        private static void Set(Object target,string field,object value)
        {
            var so=new SerializedObject(target); var p=so.FindProperty(field); if (p==null) throw new InvalidOperationException("Missing field "+field+" on "+target);
            if (value is Object o) p.objectReferenceValue=o;
            else if (value is string s) p.stringValue=s;
            else if (value is bool b) p.boolValue=b;
            else if (value is int n) p.intValue=n;
            else if (value is float f) p.floatValue=f;
            else if (value is Vector2 v) p.vector2Value=v;
            else if (value is Enum e) p.intValue=Convert.ToInt32(e);
            else if (value is GameFlagId[] flags) { p.arraySize=flags.Length; for(int i=0;i<flags.Length;i++) p.GetArrayElementAtIndex(i).intValue=(int)flags[i]; }
            else throw new InvalidOperationException("Unsupported value for "+field);
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
