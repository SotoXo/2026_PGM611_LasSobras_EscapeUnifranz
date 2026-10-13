#if UNITY_EDITOR
using System.Linq;
using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.Encounters;
using EscapeUNIFRANZ.Hazards;
using EscapeUNIFRANZ.Interaction;
using EscapeUNIFRANZ.Map;
using EscapeUNIFRANZ.Narrative;
using EscapeUNIFRANZ.World;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Object=UnityEngine.Object;
namespace EscapeUNIFRANZ.EditorTools
{
    public static partial class NarrativeCampaignBuilder
    {
        private static void BuildAudioCatalog(GameObject runtime)
        {
            var hooks=Ensure<ProjectAudioHooks>(runtime);
            var old=hooks.bindings; hooks.bindings=System.Enum.GetValues(typeof(AudioCue)).Cast<AudioCue>().Select(c=>old.FirstOrDefault(b=>b.cue==c) ?? new AudioCueBinding{cue=c}).ToArray();
            AudioSource Source(string name) { var child=runtime.transform.Find(name); if (child==null) { child=new GameObject(name).transform; child.SetParent(runtime.transform,false); } var source=Ensure<AudioSource>(child.gameObject); source.playOnAwake=false; return source; }
            hooks.effects=Source("ProjectEffectsAudio"); hooks.ambience=Source("ProjectAmbienceAudio"); hooks.music=Source("ProjectMusicAudio");
        }
        private static void BuildClarityZone(int index,Transform root,ZoneContext zone,Geometry geometry)
        {
            var objectives=root.GetComponent<NarrativeZoneObjectives>(); objectives.steps=ClearObjectives(index);
            Set(zone,"objectiveText",objectives.steps[0].text); Set(zone,"objectiveId",objectives.steps[0].id);
            foreach(var mini in zone.GetComponentsInChildren<MiniAutoController>(true))
            {
                mini.feedback=Feedback(mini.transform);
                foreach(var terminal in root.GetComponentsInChildren<SecurityTerminalInteractable>(true)) if (terminal.observedUnit==null || mini.name.Contains("Surveillance")) terminal.observedUnit=mini;
                FitMobileBody(mini.transform,new Vector2(0.4f,0.32f));
                if (index==0)
                {
                    Control(root,geometry,"Panel_Inspeccion",new Vector2(7.3f,3.3f),EnvironmentalAction.PowerCut,mini,null,20f,"UNIDAD DE INSPECCIÓN — ALIMENTACIÓN EXTERNA. Puedes rodearla usando paredes o cortar su energía durante 20 segundos. Reinicia después de 30 segundos.",GameFlagId.FirstThreatActivated);
                    Control(root,geometry,"Prueba_Inspeccion",new Vector2(9.8f,4f),EnvironmentalAction.Distraction,mini,null,5f,"Envía una señal de prueba a esta consola. La unidad irá a comprobarla. Aprovecha para alejarte.",GameFlagId.FirstThreatActivated);
                }
                else if (mini.name.Contains("Surveillance"))
                    foreach(var terminal in root.GetComponentsInChildren<SecurityTerminalInteractable>(true)) terminal.vigilanceUnit=mini;
                else Control(root,geometry,"Corte_"+mini.name,new Vector2(6.4f,index==3 ? 4.8f : 3.7f),EnvironmentalAction.PowerCut,mini,null,12f,"Esta unidad depende del suministro local. Corta energía durante 12 segundos o evita su cono usando las paredes.");
            }
            var dog=zone.GetComponentInChildren<RobotDogEncounterController>(true);
            if (dog!=null)
            {
                dog.feedback=Feedback(dog.UnitTransform); FitMobileBody(dog.UnitTransform,new Vector2(0.6f,0.38f));
                Control(root,geometry,"Corte_Perro",new Vector2(7.8f,4.4f),EnvironmentalAction.PowerCut,null,dog,12f,"El perro depende de la energía del sector. El corte lo detiene 12 segundos; no repara la red.");
                var station=Control(root,geometry,"Estacion_Carga",dog.UnitPosition+Vector2.right*2f,EnvironmentalAction.ChargingLock,null,dog,0f,"ESTACIÓN DE CARGA. Atrae al perro hacia esta luz amarilla y activa BLOQUEAR ESTACIÓN cuando esté cerca. Quedará desconectado.");
                station.destination=station.transform;
                var light=Eyes(station.transform,station.transform.position,"ChargingLight"); light.color=new Color(1f,0.8f,0.18f); light.pointLightOuterRadius=1.8f; light.intensity=0.32f;
                var decoy=Control(root,geometry,"Prueba_Carga",(Vector2)station.transform.position+Vector2.up*1.1f,EnvironmentalAction.Distraction,null,dog,5f,"Envía una señal de prueba a la estación amarilla. Atrae allí a la unidad y utiliza el cierre de la estación."); decoy.destination=station.transform;
            }
            var exo=zone.GetComponentInChildren<ExolegEncounterController>(true);
            if (exo!=null)
            {
                exo.feedback=Feedback(exo.transform); Set(exo,"telegraphSeconds",0.7f); Set(exo,"pauseSeconds",0.8f);
                var nodes=zone.GetComponentsInChildren<ExolegOverloadInteractable>(true).OrderBy(n=>n.name).ToArray();
                for(int i=0;i<nodes.Length;i++) { nodes[i].nodeIndex=i; Set(nodes[i],"prompt","[E] "+(i==0 ? "Desconectar actuador izquierdo" : i==1 ? "Desconectar actuador derecho" : "Desconectar CONTROL")); }
            }
            var arms=zone.GetComponentInChildren<RobotArmsEncounterController>(true);
            if (arms!=null)
            {
                arms.emergencyStop=true; arms.feedback=Feedback(arms.transform); Set(arms,"telegraphSeconds",0.7f); Set(arms,"pauseSeconds",0.8f);
                var stop=zone.GetComponentInChildren<RobotArmsPanelInteractable>(true); stop.transform.position=Free(geometry,new Vector2(20.3f,5.8f)); Set(stop,"prompt","[E] PARADA DE EMERGENCIA / EMERGENCY STOP");
                Set(stop,"requirementMessage","Primero aísla Seguridad en el terminal. Necesitas también la tarjeta de laboratorio.");
            }
            foreach(var hazard in zone.GetComponentsInChildren<TimedHazardArea2D>(true)) ConfigureDangerArea(hazard);
            foreach(var mover in zone.GetComponentsInChildren<PatrolMover2D>(true))
            { if (mover.GetComponent<MiniAutoController>()==null && (dog==null || mover.transform!=dog.UnitTransform)) FitMobileBody(mover.transform,new Vector2(0.5f,0.35f)); }
            if (index==3) { var fuse=NewPuzzle(root,geometry,"fuse_simulation",new Vector2(11.8f,4.2f)); ConfigureTerminal(fuse,puzzles["fuse_simulation"]); Effect(root,zone,GameFlagId.SimulationFuseReplaced,null); }
            if (index==1) { var cabinet=root.Find("Panel_maintenance_hall"); if (cabinet!=null) Label(cabinet,"GABINETE DE COMUNICACIONES\nTARJETA DE MANTENIMIENTO",new Color(1f,0.8f,0.3f),new Vector3(0f,1.6f)); }
            foreach(var terminal in root.GetComponentsInChildren<SecurityTerminalInteractable>(true))
            {
                var local=terminal.feeds.First(f=>f.camera!=null).camera;
                terminal.feeds=new[]{
                    new SecurityFeed{name="CAM 01 — ENTRADA",schematic="Puerta de acceso: necesita energía y un código. La unidad depende del panel de alimentación local.",camera=local},
                    new SecurityFeed{name="CAM 02 — HALL",schematic=index==0 ? "PLACA DEL LECTOR: 3174. Guarda el código y úsalo en el lector del acceso." : "Gabinete de comunicaciones: requiere tarjeta de mantenimiento. Dentro hay un módulo para Tecnología.",clue=index==0 ? clues["cctv_codigo"] : null},
                    new SecurityFeed{name="CAM 03 — AULA",schematic="Una ruta segura usa cobertura. Consulta el estado del robot local antes de salir."},
                    new SecurityFeed{name="CAM 04 — SIN SEÑAL",schematic="La cámara perdió la conexión. Reiniciar un enlace no reemplaza un cable retirado.",noSignal=true}};
            }
        }
        private static EnvironmentalControlInteractable Control(Transform root,Geometry g,string name,Vector2 position,EnvironmentalAction action,MiniAutoController mini,RobotDogEncounterController dog,float seconds,string text,GameFlagId flag=GameFlagId.None)
        {
            var obj=WorldObject(root,g,name,position,Sprite(Art+"Environment/Props/desk_pc.png")); var control=obj.AddComponent<EnvironmentalControlInteractable>();
            control.action=action; control.mini=mini; control.dog=dog; control.disabledSeconds=seconds; control.cooldownSeconds=action==EnvironmentalAction.Distraction ? 15f : 30f; control.explanation=text; control.destination=control.transform;
            Set(control,"prompt","[E] "+(action==EnvironmentalAction.PowerCut ? "Cortar energía del robot" : action==EnvironmentalAction.ChargingLock ? "Estación de carga" : "Señal de prueba"));
            if (flag!=GameFlagId.None) Set(control,"requiredFlags",new[]{flag}); Track(obj,"control_"+name,ScanCategory.Terminal); return control;
        }
        private static ThreatFeedback2D Feedback(Transform actor)
        {
            var feedback=Ensure<ThreatFeedback2D>(actor.gameObject); var old=actor.Find("ThreatMarker"); if (old!=null) Object.DestroyImmediate(old.gameObject);
            feedback.marker=Label(actor,"",Color.white,new Vector3(0f,1.4f)); feedback.marker.name="ThreatMarker";
            feedback.bodyRenderers=actor.GetComponentsInChildren<SpriteRenderer>(true).Where(r=>r.enabled && r.GetComponent<TimedHazardArea2D>()==null && (r.name=="GroupVisual" || r.name=="Art")).ToArray();
            feedback.led=actor.GetComponentsInChildren<Light2D>(true).FirstOrDefault(l=>l.name=="RobotLED" || l.name=="ThreatLED"); if (feedback.led==null) feedback.led=Eyes(actor,actor.position,"ThreatLED"); return feedback;
        }
        private static TMP_Text Label(Transform root,string text,Color color,Vector3 position)
        {
            var obj=new GameObject("WorldLabel"); obj.transform.SetParent(root,false); obj.transform.localPosition=position;
            var label=obj.AddComponent<TextMeshPro>(); label.font=EscapeUNIFRANZ.UI.PresentationTheme.Font; label.fontSize=2.3f; label.text=text; label.color=color; label.alignment=TextAlignmentOptions.Center; label.rectTransform.sizeDelta=new Vector2(5f,1f);
            var renderer=obj.GetComponent<MeshRenderer>(); renderer.sortingLayerName="Effects"; renderer.sortingOrder=300; return label;
        }
        private static void FitContact(HazardContact2D hit,Vector2 size)
        {
            var col=hit.GetComponent<Collider2D>(); if (col is BoxCollider2D box) { box.size=size; box.offset=new Vector2(0f,size.y*0.5f); }
            else if (col is CircleCollider2D circle) { circle.radius=size.x*0.5f; circle.offset=new Vector2(0f,size.y*0.5f); }
            else if (col is CapsuleCollider2D capsule) { capsule.size=size; capsule.offset=new Vector2(0f,size.y*0.5f); }
        }
        private static void FitMobileBody(Transform actor,Vector2 size)
        {
            foreach(var contact in actor.GetComponentsInChildren<HazardContact2D>(true))
                if (contact.GetComponent<TimedHazardArea2D>()==null) { if (contact.transform!=actor) contact.transform.localPosition=Vector3.zero; FitContact(contact,size+Vector2.one*0.04f); }
            foreach(var collider in actor.GetComponents<Collider2D>()) if (!collider.isTrigger)
            {
                if (collider is BoxCollider2D box) { box.size=size; box.offset=new Vector2(0f,size.y*0.5f); }
                if (collider is CapsuleCollider2D capsule) { capsule.size=size; capsule.offset=new Vector2(0f,size.y*0.5f); }
            }
        }
        private static void ConfigureDangerArea(TimedHazardArea2D hazard)
        {
            var collider=hazard.GetComponent<Collider2D>(); Vector2 size=new Vector2(0.65f,2.4f),offset=collider.offset;
            if (collider is CapsuleCollider2D existingCapsule) size=existingCapsule.size;
            var box=collider as BoxCollider2D; if (box!=null) size=box.size;
            var capsule=Ensure<CapsuleCollider2D>(hazard.gameObject); if (box!=null) Object.DestroyImmediate(box);
            capsule.isTrigger=true; capsule.size=size; capsule.offset=offset; capsule.direction=size.y>=size.x ? CapsuleDirection2D.Vertical : CapsuleDirection2D.Horizontal;
            var sr=hazard.GetComponent<SpriteRenderer>(); if (sr!=null) sr.enabled=false;
            var old=hazard.transform.Find("DangerHalo"); if (old!=null) Object.DestroyImmediate(old.gameObject);
            var root=new GameObject("DangerHalo"); root.transform.SetParent(hazard.transform,false); var line=root.AddComponent<LineRenderer>();
            line.useWorldSpace=false; line.loop=true; line.startWidth=line.endWidth=0.045f; line.sortingLayerName="Effects"; line.sortingOrder=200;
            string materialPath=Art+"Levels/DangerHalo.mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath); if (material==null) { material=new Material(Shader.Find("Sprites/Default")); AssetDatabase.CreateAsset(material,materialPath); }
            line.sharedMaterial=material;
            var points=new Vector3[32]; bool vertical=size.y>=size.x; float r=Mathf.Min(size.x,size.y)*0.5f; float stem=Mathf.Max(size.x,size.y)*0.5f-r;
            for(int i=0;i<32;i++) { float angle=(i<16 ? i/15f*180f : 180f+(i-16)/15f*180f)*Mathf.Deg2Rad; Vector2 p=new Vector2(Mathf.Cos(angle)*r,Mathf.Sin(angle)*r+(i<16 ? stem : -stem)); if (!vertical) p=new Vector2(p.y,p.x); points[i]=p+offset; }
            line.positionCount=points.Length; line.SetPositions(points); line.enabled=false; hazard.dangerHalo=line;
        }
        private static NarrativeObjectiveStep[] ClearObjectives(int zone)
        {
            switch(zone)
            {
                case 0:return new[]{Step("entrada_bateria","Encuentra energía para el panel. Busca una batería en la zona dental.",GameFlagId.PowerEntranceRestored,"battery"),Step("entrada_nota","Lee la nota junto al panel de acceso.",GameFlagId.FirstClueFound),Step("entrada_cables","Conecta ENERGÍA y DATOS para restablecer el acceso.",GameFlagId.PowerEntranceRestored),Step("entrada_cctv","Evita el robot o corta su energía. Usa CCTV para buscar el código.",GameFlagId.EntranceCodeObserved),Step("entrada_codigo","Introduce el código observado en el lector del acceso.",GameFlagId.EntranceCodeAccepted),Step("entrada_salir","Cruza al Hall; allí tendrás un checkpoint.")};
                case 1:return new[]{Step("hall_llave","Busca la llave de ARCA en la cafetería.",item:"key"),Step("hall_arca","Abre ARCA y recupera sus interruptores.",GameFlagId.ArcaAccessRestored),Step("hall_simulacion","Accede a Simulación. El gabinete del Hall necesita una tarjeta de mantenimiento.")};
                case 2:return new[]{Step("arca_interruptores","Reinicia PRINCIPAL, CONTROL y SEGURIDAD de izquierda a derecha.",GameFlagId.ArcaAccessRestored),Step("arca_volver","Recoge la tarjeta emitida y vuelve al Hall.")};
                case 3:return new[]{Step("sim_fusible","Encuentra el fusible de respaldo y reemplaza el dañado.",GameFlagId.SimulationFuseReplaced),Step("sim_arranque","Enciende los cuatro componentes siguiendo los números.",GameFlagId.SimulationPowerRestored),Step("sim_cables","Une las cuatro señales del controlador por sus nombres.",GameFlagId.HardwareRoutingRestored),Step("sim_exoleg","Desconecta ambos actuadores del Exoleg y después CONTROL.",GameFlagId.ExolegsDisabled),Step("sim_salida","La tarjeta abre el gabinete del Hall. Recupera allí el módulo para Tecnología.",GameFlagId.MaintenanceAccessRestored),Step("sim_tecnologia","Accede al sector de Tecnología.")};
                case 4:return new[]{Step("tec_red","Usa el módulo y conecta SERVIDOR con LAB por una ruta segura.",GameFlagId.TechnologyNetworkRestored),Step("tec_laboratorio","Abre el laboratorio con la tarjeta emitida por ARCA.")};
                case 5:return new[]{Step("lab_seguridad","Aísla SEGURIDAD; conserva el núcleo y los accesos.",GameFlagId.SecurityNodeDisabled),Step("lab_stop","Cruza cuando el halo no esté rojo y alcanza EMERGENCY STOP.",GameFlagId.ArmsDisabled),Step("lab_nucleo","Accede al núcleo para frenar el incidente.")};
                default:return new[]{Step("core_red","Aísla cámaras y puertas del servidor.",GameFlagId.FrancisNetworkPhaseComplete),Step("core_hardware","Corta energía, actuadores y seguridad en ese orden.",GameFlagId.FrancisHardwarePhaseComplete),Step("core_software","Aísla, verifica y ejecuta el apagado seguro.",GameFlagId.GameCompleted)};
            }
        }
    }
}
#endif
