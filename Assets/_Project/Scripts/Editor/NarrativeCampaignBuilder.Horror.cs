#if UNITY_EDITOR
using System.Linq;
using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.Encounters;
using EscapeUNIFRANZ.Hazards;
using EscapeUNIFRANZ.Interaction;
using EscapeUNIFRANZ.Map;
using EscapeUNIFRANZ.Narrative;
using EscapeUNIFRANZ.Puzzles;
using EscapeUNIFRANZ.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using Object=UnityEngine.Object;
namespace EscapeUNIFRANZ.EditorTools
{
    public static partial class NarrativeCampaignBuilder
    {
        private static void BuildGlitch(GameRuntimeRoot runtime,Transform ui)
        {
            var old=ui.Find("ScreenGlitch"); if (old!=null) Object.DestroyImmediate(old.gameObject);
            var root=Rect(ui,"ScreenGlitch",Vector2.zero,Vector2.zero); Stretch(root);
            var glitch=root.gameObject.AddComponent<ScreenGlitchController>();
            var dim=Image(root,"Dim",Color.clear,Vector2.zero,Vector2.zero); Stretch(dim); glitch.dim=dim.GetComponent<Image>();
            glitch.bands=new Image[6];
            for(int i=0;i<6;i++) glitch.bands[i]=Image(root,"Scanline_"+i,Color.clear,new Vector2(0f,190f-i*70f),new Vector2(1920f,i%2==0 ? 2f : 5f)).GetComponent<Image>();
            glitch.intrusion=Text(root,"Intrusion",Vector2.zero,new Vector2(500f,80f),22f); glitch.intrusion.color=new Color(0.8f,0.25f,0.23f,0.45f); glitch.intrusion.enabled=false;
            glitch.optionalAudio=root.gameObject.AddComponent<AudioSource>(); glitch.optionalAudio.playOnAwake=false;
        }
        private static void BuildHorrorZone(int index,Transform root,ZoneContext zone,Geometry geometry)
        {
            var director=root.gameObject.AddComponent<WorldHorrorDirector>();
            director.optionalAudio=root.gameObject.AddComponent<AudioSource>(); director.optionalAudio.playOnAwake=false;
            var ambience=new GameObject("ElectricalAmbience"); ambience.transform.SetParent(root,false);
            director.ambientAudio=ambience.AddComponent<AudioSource>(); director.ambientAudio.playOnAwake=false; director.ambientAudio.loop=true; director.ambientAudio.volume=0.25f;
            director.alarmDoors=zone.GetComponentsInChildren<ZoneExitInteractable>(true);
            var lamps=zone.transform.Find("GroupLevelArt").GetComponentsInChildren<Light2D>(true);
            director.powerLights=lamps.Where(l=>l.name=="AuthoredLamp_1" || l.name=="AuthoredLamp_2").ToArray();
            director.alarmLights=lamps.Where(l=>l.name=="AuthoredLamp_3").ToArray();
            ConfigureBuildingLamps(index,lamps);
            if (index==3) director.localPowerFlag=GameFlagId.SimulationPowerRestored;
            foreach(var camera in root.GetComponentsInChildren<SecurityCamera2D>(true))
                camera.ledLight=Eyes(camera.transform,camera.transform.position,"CameraLED");
            foreach(var exit in director.alarmDoors)
            {
                var audio=Ensure<AudioSource>(exit.gameObject); audio.playOnAwake=false; Set(exit,"optionalAudio",audio);
                var old=exit.transform.Find("AccessLED"); if (old!=null) Object.DestroyImmediate(old.gameObject);
                var led=new GameObject("AccessLED").transform; led.SetParent(exit.transform,false); led.localPosition=new Vector3(0f,0.8f,0f);
                var sr=led.gameObject.AddComponent<SpriteRenderer>(); sr.sprite=Sprite(Art+"Items/bateria.png"); sr.sharedMaterial=lit;
                sr.sortingLayerName="World"; sr.transform.localScale=Vector3.one*0.25f; sr.sortingOrder=WorldYSort2D.Order(exit.transform.position.y)-1; exit.statusIndicator=sr;
                if (index==0 && exit.TargetZoneId=="hall")
                { Set(exit,"requiredFlags",new[]{GameFlagId.EntranceCodeAccepted}); Set(exit,"requirementMessage","Falta autorización. Revisa CCTV y valida el lector local."); Set(exit,"lockedMessage","Revisa el circuito de respaldo y la autorización del lector."); }
                if (index==3 && exit.TargetZoneId=="piso3") Set(exit,"requiredFlags",new[]{GameFlagId.MaintenanceAccessRestored});
            }
            foreach(var sequence in root.GetComponentsInChildren<LightSequenceTrigger>(true))
            {
                sequence.stepSeconds=index==0 ? 0.2f : 0.15f; sequence.blackoutSeconds=1.4f;
                sequence.distantEyes=Eyes(root,Free(geometry,(Vector2)sequence.transform.position+Vector2.right*3f),"DistantEyes_"+sequence.eventId); sequence.distantEyes.enabled=false;
                sequence.optionalAudio=Ensure<AudioSource>(sequence.gameObject); sequence.optionalAudio.playOnAwake=false;
            }
            switch(index)
            {
                case 0:
                    foreach(var camera in root.GetComponentsInChildren<SecurityCamera2D>(true)) camera.enabledAfterFlag=GameFlagId.FirstThreatActivated;
                    PlaceClue(root,geometry,"power_confirm",new Vector2(6.7f,3.8f));
                    director.firstRobot=FirstRobot(root,zone,geometry,out var encounter); director.firstEncounter=encounter;
                    NewPuzzle(root,geometry,"entrance_code",new Vector2(11f,4.2f));
                    SecurityTerminal(root,zone,geometry,"entrada",new Vector2(7f,4.8f),GameFlagId.FirstThreatActivated,true);
                    break;
                case 1:
                    PlaceClue(root,geometry,"hall_mantenimiento",new Vector2(15f,8f));
                    NewPuzzle(root,geometry,"maintenance_hall",new Vector2(15.8f,8f));
                    SecurityTerminal(root,zone,geometry,"hall",new Vector2(18.5f,9f),GameFlagId.PowerEntranceRestored,false);
                    break;
                case 2: PlaceClue(root,geometry,"arca_confirm",new Vector2(5f,10f)); break;
                case 3:
                    PlaceClue(root,geometry,"hardware_confirm",new Vector2(14.8f,4f));
                    NewPuzzle(root,geometry,"simulation_power",new Vector2(12.5f,4.7f));
                    var fuse=WorldObject(root,geometry,"Pickup_Fusible",new Vector2(6.7f,6f),Sprite(Art+"Items/bateria.png"));
                    var pickup=fuse.AddComponent<ItemPickupInteractable>(); pickup.itemId="fuse"; pickup.pickupId="simulacion_fusible";
                    Set(pickup,"prompt","[E] Recoger fusible de respaldo"); Track(fuse,"simulacion_fusible",ScanCategory.Interactable);
                    SecurityTerminal(root,zone,geometry,"simulacion",new Vector2(16.3f,7.8f),GameFlagId.SimulationPowerRestored,false);
                    break;
                case 4:
                    PlaceClue(root,geometry,"network_confirm",new Vector2(15.8f,5.4f));
                    director.alarmDog=zone.GetComponentInChildren<RobotDogEncounterController>(true); director.alarmDog.requireCameraAlarm=true;
                    SecurityTerminal(root,zone,geometry,"tecnologia",new Vector2(18.5f,5.8f),GameFlagId.PowerEntranceRestored,false);
                    var slow=FirstRobot(root,zone,geometry,out _); slow.name="SurveillanceRobot"; Set(slow,"chaseSpeed",2.6f);
                    var slowMover=slow.GetComponent<PatrolMover2D>(); Set(slowMover,"patrolSpeed",1.1f);
                    slow.GetComponentInChildren<DetectionSensor2D>(true).GetComponent<CircleCollider2D>().radius=4.8f;
                    break;
                case 5:
                    PlaceClue(root,geometry,"software_confirm",new Vector2(10.8f,6.5f));
                    SecurityTerminal(root,zone,geometry,"laboratorio",new Vector2(9f,8f),GameFlagId.PowerEntranceRestored,false);
                    break;
                case 6: PlaceClue(root,geometry,"core_confirm",new Vector2(17f,3.8f)); break;
            }
            foreach(var panel in zone.GetComponentsInChildren<PuzzleInteractable>(true)) ConfigureTerminal(panel.gameObject,panel.puzzle);
            foreach(var sensor in zone.GetComponentsInChildren<DetectionSensor2D>(true))
                if (sensor.GetComponentInParent<SecurityCamera2D>()==null && sensor.GetComponentInParent<PatrolMover2D>()!=null)
                { Set(sensor,"directional",true); Set(sensor,"fieldOfView",110f); }
            foreach(var clue in root.GetComponentsInChildren<ClueInteractable>(true))
                if (clue.clue.id.EndsWith("confirm")) Set(clue,"prompt","[E] Examinar equipo");
        }
        private static void ConfigureTerminal(GameObject obj,PuzzleData data)
        {
            var status=Ensure<TerminalStatus2D>(obj); status.monitor=VisibleRenderer(obj);
            status.puzzleId=data.id;
            status.powerFlag=data.id=="power_entrada" || data.id=="simulation_power" ? GameFlagId.None : GameFlagId.PowerEntranceRestored;
            if (data.id=="power_entrada") status.powerFlag=GameFlagId.PowerEntranceRestored;
            if (data.id=="simulation_power") status.powerFlag=GameFlagId.SimulationPowerRestored;
            status.credentialItem=data.requiredItem; status.compromisedUntilRepair=data.id=="software_security" || data.id.StartsWith("francis_");
            status.repairedFlag=data.completionFlags!=null && data.completionFlags.Length>0 ? data.completionFlags[0] : GameFlagId.None;
            status.diagnostic="Valida el diagnóstico y las etiquetas físicas. Registro: J • Objetos: I.";
        }
        private static void ConfigureBuildingLamps(int index,Light2D[] lamps)
        {
            foreach(var lamp in lamps)
            {
                if (lamp.lightType==Light2D.LightType.Global) { lamp.color=new Color(0.7f,0.8f,1f); lamp.intensity=index==3 ? 0.045f : 0.065f; continue; }
                if (lamp.name=="AuthoredLamp_0") lamp.color=new Color(0.82f,0.91f,1f);
                else if (lamp.name=="AuthoredLamp_1") lamp.color=new Color(1f,0.87f,0.54f);
                else if (lamp.name=="AuthoredLamp_2") lamp.color=new Color(0.82f,0.95f,1f);
                else if (lamp.name=="AuthoredLamp_3") lamp.color=new Color(0.8f,0.25f,0.18f);
                var flicker=lamp.GetComponent<GroupLampFlicker2D>();
                if (flicker!=null)
                {
                    var audio=Ensure<AudioSource>(lamp.gameObject); audio.playOnAwake=false; Set(flicker,"optionalAudio",audio);
                    Set(flicker,"randomSeed",1009+index*137+(lamp.name=="AuthoredLamp_0" ? 7 : 29));
                    Set(flicker,"minimumIntensity",0.015f); Set(flicker,"failureProbability",lamp.name=="AuthoredLamp_0" ? 0.45f : 0.2f);
                    Set(flicker,"onSeconds",new Vector2(5.5f,13f)); Set(flicker,"offSeconds",new Vector2(0.15f,0.6f));
                }
            }
        }
        private static Light2D Eyes(Transform root,Vector2 position,string name)
        {
            var obj=new GameObject(name); obj.transform.SetParent(root,false); obj.transform.position=position+Vector2.up*0.35f;
            var light=obj.AddComponent<Light2D>(); light.lightType=Light2D.LightType.Point; light.color=new Color(1f,0.04f,0.02f);
            light.pointLightOuterRadius=0.42f; light.pointLightInnerRadius=0.1f; light.intensity=0.8f; light.shadowsEnabled=true;
            light.targetSortingLayers=SortingLayer.layers.Select(s=>s.id).ToArray(); return light;
        }
        private static MiniAutoController FirstRobot(Transform root,ZoneContext zone,Geometry geometry,out EncounterContext encounter)
        {
            var encounterObj=new GameObject("InspectionEncounter"); encounterObj.transform.SetParent(root,false);
            encounter=encounterObj.AddComponent<EncounterContext>(); Set(encounter,"checkpointSpawnId",zone.DefaultCheckpointSpawnId);
            var robot=WorldObject(encounterObj.transform,geometry,"MiniAuto_Inspection",new Vector2(12f,6f),Sprite(Art+"Characters/Enemies/Shadow/sombra_spritesheet_12x1.png"));
            var body=robot.AddComponent<Rigidbody2D>(); body.bodyType=RigidbodyType2D.Kinematic; body.gravityScale=0f; body.freezeRotation=true; body.interpolation=RigidbodyInterpolation2D.Interpolate;
            var feet=robot.AddComponent<CapsuleCollider2D>(); feet.size=new Vector2(0.4f,0.32f); feet.offset=new Vector2(0f,0.15f);
            var mover=robot.AddComponent<PatrolMover2D>(); Set(mover,"patrolSpeed",1.35f);
            var path=encounterObj.AddComponent<PatrolPath2D>(); var points=new Transform[3];
            Vector2 origin=robot.transform.position; Vector2 routeAxis=Vector2.right; float routeLength=0f;
            foreach(var direction in new[]{Vector2.right,Vector2.left,Vector2.up,Vector2.down})
            {
                float length=0f;
                for(float d=0.125f;d<=2.8f;d+=0.125f)
                {
                    Vector2 p=origin+direction*d;
                    if (geometry.collisions.Any(c=>c.Bounds.Overlaps(new Rect(p.x-0.22f,p.y-0.03f,0.44f,0.38f))) || !accessibleCells.Contains(Vector2Int.RoundToInt(p/0.125f))) break;
                    length=d;
                }
                if (length>routeLength) { routeLength=length; routeAxis=direction; }
            }
            for(int i=0;i<3;i++) { points[i]=new GameObject("Route_"+i).transform; points[i].SetParent(encounterObj.transform,false); points[i].position=origin+routeAxis*routeLength*(i/2f); }
            SetObjectArray(path,"points",points); Set(mover,"path",path);
            var field=new GameObject("DetectionField"); field.transform.SetParent(robot.transform,false);
            var circle=field.AddComponent<CircleCollider2D>(); circle.radius=3.1f; circle.isTrigger=true;
            var sensor=field.AddComponent<DetectionSensor2D>(); Set(sensor,"directional",true); Set(sensor,"fieldOfView",100f);
            var mini=robot.AddComponent<MiniAutoController>(); Set(mini,"mover",mover); Set(mini,"sensor",sensor); Set(mini,"chaseSpeed",3.2f);
            mini.optionalAudio=Ensure<AudioSource>(robot); mini.optionalAudio.playOnAwake=false;
            var contact=robot.AddComponent<HazardContact2D>(); Set(contact,"encounter",encounter); Set(contact,"defeatReason","La unidad de inspección hizo contacto. Busca cobertura y evita su ruta.");
            Eyes(robot.transform,(Vector2)robot.transform.position,"RobotLED"); Track(robot,"inspection_robot_"+zone.ZoneId,ScanCategory.Sensor,true);
            return mini;
        }
        private static void SecurityTerminal(Transform root,ZoneContext zone,Geometry geometry,string id,Vector2 pos,GameFlagId flag,bool code)
        {
            var obj=WorldObject(root,geometry,"SecurityTerminal_"+id,pos,Sprite(Art+"Environment/Props/desk_pc.png"));
            var terminal=obj.AddComponent<SecurityTerminalInteractable>(); terminal.terminalId=id; terminal.powerFlag=flag;
            var camera=zone.GetComponentsInChildren<SecurityCamera2D>(true).FirstOrDefault();
            terminal.feeds=new[]{
                new SecurityFeed{name="CAM 1 — HALL",schematic=code ? "[LECTOR] ← placa visible: 3174 → [PASILLO / PATRULLA]" : "[HALL] — mostrador — gabinete de mantenimiento — acceso ARCA",clue=code ? clues["cctv_codigo"] : null},
                new SecurityFeed{name="CAM 2 — LAB",schematic="[PANEL] → [FRANJA DE BRAZOS] → [SALIDA]. Cruza durante el intervalo sin descarga."},
                new SecurityFeed{name="CAM 3 — SECTOR LOCAL",schematic="[COBERTURA] ← barrido de cámara → [ACCESO]. Este enlace permite desviar o suspender la cámara del sector.",camera=camera},
                new SecurityFeed{name="CAM 4 — SIN SEÑAL",schematic="ERROR / enlace físico perdido. El reinicio remoto no repara un cable retirado.",noSignal=true}};
            terminal.optionalAudio=obj.AddComponent<AudioSource>(); terminal.optionalAudio.playOnAwake=false;
            Set(terminal,"prompt","[E] CCTV / seguridad"); Track(obj,"security_terminal_"+id,ScanCategory.Terminal);
            var status=obj.AddComponent<TerminalStatus2D>(); status.powerFlag=flag; status.monitor=VisibleRenderer(obj); status.diagnostic="Control CCTV local. Cuatro feeds y una suspensión temporal disponible.";
        }
        private static void SetObjectArray(Object target,string field,Object[] values)
        {
            var so=new SerializedObject(target); var p=so.FindProperty(field); p.arraySize=values.Length;
            for(int i=0;i<values.Length;i++) p.GetArrayElementAtIndex(i).objectReferenceValue=values[i]; so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
