#if UNITY_EDITOR
using System;
using System.Linq;
using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.Interaction;
using EscapeUNIFRANZ.Narrative;
using EscapeUNIFRANZ.Player;
using EscapeUNIFRANZ.UI;
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
        private sealed class RoomDefinition
        {
            public string id,name; public Rect bounds;
            public RoomLightingState initial,settled;
            public RoomFailurePattern pattern;
            public bool entry,defective,corridor;
            public GameFlagId restore;
            public Color color;
            public float hold=1.4f,blackout;
        }
        private static RoomDefinition R(string id,string name,Rect bounds,Color color,RoomLightingState initial=RoomLightingState.Dim,bool entry=false,RoomFailurePattern pattern=RoomFailurePattern.Mild,float hold=1.4f,float blackout=0,GameFlagId restore=GameFlagId.None,bool defective=false,bool corridor=false)
            =>new RoomDefinition{id=id,name=name,bounds=bounds,color=color,initial=initial,settled=initial==RoomLightingState.Blackout ? RoomLightingState.Dim : initial,entry=entry,pattern=pattern,hold=hold,blackout=blackout,restore=restore,defective=defective,corridor=corridor};
        private static RoomDefinition[] RoomDefinitions(int index)
        {
            var cold=new Color(.78f,.88f,1f); var amber=new Color(1f,.82f,.45f); var blue=new Color(.5f,.72f,1f); var red=new Color(1f,.16f,.1f);
            var reception=new Rect(1.2f,3.0f,9.85f,5.9f); var dental=new Rect(11.85f,1.2f,12.2f,5.5f); var hall=new Rect(11.85f,8.05f,12.2f,4.65f);
            switch(index)
            {
                case 0:return new[]{R("recepcion","Recepción y panel de acceso",reception,cold,entry:true,hold:1.1f,restore:GameFlagId.PowerEntranceRestored,defective:true),R("dental","Sala dental y corredor de inspección",dental,cold,RoomLightingState.Blackout,true,RoomFailurePattern.Strong,1.5f,.7f,GameFlagId.PowerEntranceRestored),R("admision","Admisión al fondo",hall,cold,RoomLightingState.Blackout)};
                case 1:return new[]{R("hall","Hall de admisión",hall,cold,entry:true,pattern:RoomFailurePattern.Double,hold:2.0f,defective:true),R("recepcion","Recepción",reception,cold,RoomLightingState.Normal),R("dental","Sala dental",dental,cold,RoomLightingState.Blackout)};
                case 2:return new[]{R("arca","Control ARCA",new Rect(1.2f,8.3f,9.85f,4.4f),amber,entry:true,hold:1.6f,restore:GameFlagId.ArcaAccessRestored),R("paso","Acceso de servicio",dental,amber,RoomLightingState.Normal,corridor:true)};
                case 3:return new[]{R("cocina","Cocina de simulación",new Rect(1.2f,1.2f,7.85f,9.55f),cold,entry:true,hold:1.2f,defective:true),R("control","Pasillo y caja de fusibles",new Rect(9.85f,1.2f,4.05f,9.55f),cold,restore:GameFlagId.SimulationPowerRestored,corridor:true),R("simulacion","Sala del dispositivo de simulación",new Rect(14.85f,7.15f,7.95f,3.55f),cold,RoomLightingState.Blackout,true,RoomFailurePattern.Strong,1.0f,1.3f,GameFlagId.SimulationPowerRestored),R("practica","Sala de práctica",new Rect(14.85f,1.2f,7.95f,4.55f),cold,RoomLightingState.Blackout,true,RoomFailurePattern.Double,1.3f,.7f,GameFlagId.SimulationPowerRestored)};
                case 4:return new[]{R("pasillo","Pasillo de Tecnología",new Rect(1.2f,1.2f,27.6f,2.5f),blue,restore:GameFlagId.TechnologyNetworkRestored,corridor:true),R("equipos","Aula de equipos",new Rect(1.2f,5.2f,10.8f,5.5f),blue,RoomLightingState.Normal),R("red","Sala de red",new Rect(12.9f,5.2f,6.1f,5.5f),blue,RoomLightingState.Blackout,true,RoomFailurePattern.Strong,1.25f,.8f,defective:true),R("seguridad","Sector de seguridad",new Rect(19.85f,5.2f,8.9f,5.5f),red,RoomLightingState.Emergency)};
                case 5:return new[]{R("pasillo","Pasillo del laboratorio",new Rect(1.2f,1.2f,27.6f,2.5f),cold,corridor:true),R("pantallas","Laboratorio de pantallas",new Rect(1.2f,5.2f,10.8f,5.5f),blue,entry:true,pattern:RoomFailurePattern.Double,hold:1.3f,defective:true),R("servidor","Cuarto de servicios",new Rect(12.9f,5.2f,6.1f,5.5f),blue,RoomLightingState.Blackout),R("brazos","Zona de brazos y parada de emergencia",new Rect(19.85f,5.2f,8.9f,5.5f),red,RoomLightingState.Emergency,restore:GameFlagId.SecurityNodeDisabled)};
                default:return new[]{R("pasillo","Corredor del núcleo",new Rect(1.2f,1.2f,27.6f,2.5f),red,corridor:true),R("aula1","Aula del extremo",new Rect(1.2f,5.2f,5.85f,5.5f),cold,RoomLightingState.Blackout),R("aula2","Control de red",new Rect(7.85f,5.2f,6.1f,5.5f),red,entry:true,hold:1.0f,restore:GameFlagId.FrancisNetworkPhaseComplete),R("aula3","Aula técnica del núcleo",new Rect(14.85f,5.2f,6.1f,5.5f),red,entry:true,pattern:RoomFailurePattern.Double,hold:1.25f,blackout:.5f),R("aula4","Control de actuadores",new Rect(21.85f,5.2f,6.1f,5.5f),red,RoomLightingState.Emergency)};
            }
        }
        private static void BuildAtmosphereUI(GameRuntimeRoot runtime,Transform ui)
        {
            var tutorial=Ensure<EntranceTutorialController>(runtime.gameObject); Set(tutorial,"session",runtime.GetComponent<GameSessionController>()); Set(tutorial,"mode",runtime.GetComponent<GameplayModeController>());
            Set(tutorial,"player",runtime.GetComponentInChildren<PlayerMovement2D>(true)); Set(tutorial,"interaction",runtime.GetComponentInChildren<PlayerInteraction>(true));
            var flow=runtime.GetComponentInChildren<GameFlowView>(true); var hud=Ref<GameObject>(flow,"gameplayHudRoot").transform;
            var old=hud.Find("ContextTutorial"); if(old!=null) Object.DestroyImmediate(old.gameObject);
            var label=Text(hud,"ContextTutorial",new Vector2(0,-205),new Vector2(620,64),18f); label.color=new Color(1f,.91f,.7f); Set(tutorial,"label",label); label.gameObject.SetActive(false);
            var objective=runtime.GetComponentInChildren<ObjectiveView>(true); var objectiveRoot=Ref<GameObject>(objective,"root").GetComponent<RectTransform>();
            objectiveRoot.sizeDelta=new Vector2(510,76); Ref<TMP_Text>(objective,"label").fontSize=18f;
        }
        private static void BuildAtmosphereZone(int index,Transform root,ZoneContext zone,Geometry geometry)
        {
            var lighting=new GameObject("RoomLighting").transform; lighting.SetParent(root,false);
            var rooms=RoomDefinitions(index).Select(d=>BuildRoom(lighting,geometry,d,index)).ToArray();
            var director=root.GetComponent<WorldHorrorDirector>(); director.powerLights=Array.Empty<Light2D>(); director.alarmLights=Array.Empty<Light2D>();
            director.revealRooms=index==0 ? rooms.Take(2).ToArray() : Array.Empty<RoomLightingController>();
            director.alarmRooms=rooms.Where(r=>r.roomId=="dental" && index==0 || r.roomId=="seguridad" || r.roomId=="brazos" || r.roomId=="pasillo" && index==6).ToArray();
            foreach(var room in director.revealRooms) room.restoredByDirector=true;
            // The existing map art stays in place; the room groups own illumination now.
            foreach(var lamp in zone.transform.Find("GroupLevelArt").GetComponentsInChildren<Light2D>(true))
            {
                if(lamp.lightType==Light2D.LightType.Global) { lamp.intensity=index==0 ? .045f : .06f; continue; }
                lamp.enabled=false; lamp.intensity=0; var flicker=lamp.GetComponent<GroupLampFlicker2D>(); if(flicker!=null) { flicker.SetExternalPower(0); flicker.enabled=false; }
            }
            foreach(var effect in root.GetComponentsInChildren<WorldStateEffect2D>(true)) effect.restoredLights=effect.restoredLights.Where(l=>l!=null && !l.name.StartsWith("AuthoredLamp_",StringComparison.Ordinal)).ToArray();
            foreach(var sequence in root.GetComponentsInChildren<LightSequenceTrigger>(true))
            { sequence.lights=Array.Empty<Light2D>(); sequence.room=rooms.OrderBy(r=>Vector2.Distance(r.roomBounds.center,sequence.transform.position)).First(); }
            foreach(var status in zone.GetComponentsInChildren<TerminalStatus2D>(true))
            { status.diagnostic=status.diagnostic.Replace("Registro: J • Objetos: I.","Consulta la ficha del equipo."); var led=status.transform.Find("MonitorGlow"); status.monitorLight=led!=null ? led.GetComponent<Light2D>() : Eyes(status.transform,status.transform.position,"MonitorGlow"); status.monitorLight.pointLightOuterRadius=1.0f; status.monitorLight.pointLightInnerRadius=.12f; status.monitorLight.intensity=0; }
            if(index==0)
            {
                var pc=root.Find("PC_Recepcion").GetComponent<EnvironmentalInspectInteractable>(); pc.transform.position=Free(geometry,new Vector2(3.95f,4.35f)); pc.explanation="Sin energía. Busca una fuente de alimentación para el panel de acceso. Hay una linterna del personal junto a recepción."; pc.entrancePowerInspection=true; Set(pc,"prompt","[E] Inspeccionar");
                root.Find("Clue_entrada_manual").position=Free(geometry,new Vector2(5.3f,3.5f));
                placement.RemoveAll(line=>line.StartsWith("Z00_Entrada / PC_Recepcion =",StringComparison.Ordinal) || line.StartsWith("Z00_Entrada / Clue_entrada_manual =",StringComparison.Ordinal));
                placement.Add("Z00_Entrada / PC_Recepcion = "+pc.transform.position.ToString("F3")); placement.Add("Z00_Entrada / Clue_entrada_manual = "+root.Find("Clue_entrada_manual").position.ToString("F3"));
                var status=Ensure<TerminalStatus2D>(pc.gameObject); status.powerFlag=GameFlagId.EntranceBatteryInstalled; status.repairedFlag=GameFlagId.PowerEntranceRestored; status.monitor=VisibleRenderer(pc.gameObject); status.diagnostic="El acceso necesita alimentación auxiliar.";
                var glow=Eyes(pc.transform,pc.transform.position,"MonitorGlow"); glow.pointLightOuterRadius=1.0f; glow.intensity=0; status.monitorLight=glow;
                root.Find("Armario_Personal").gameObject.SetActive(false);
                root.GetComponent<NarrativeZoneObjectives>().steps=new[]{Step("entrada_inspeccionar","Acércate a la terminal junto a recepción.",GameFlagId.EntrancePanelInspected,"battery"),Step("entrada_bateria","Busca una fuente de energía para el panel.",GameFlagId.EntranceBatteryInstalled,"battery"),Step("entrada_regresar","Regresa al panel e instala la batería.",GameFlagId.EntranceBatteryInstalled),Step("entrada_conexiones","Lee la nota y repara las conexiones.",GameFlagId.PowerEntranceRestored),Step("entrada_suministro","Las luces responden. Mira alrededor.",GameFlagId.FirstThreatActivated),Step("entrada_cctv","Evita al robot y revisa la terminal CCTV.",GameFlagId.EntranceCodeObserved),Step("entrada_codigo","Introduce el código observado en el lector.",GameFlagId.EntranceCodeAccepted),Step("entrada_salir","Abre el acceso y continúa hacia el Hall.")};
            }
            foreach(var puzzle in puzzles.Values) if(puzzle.id!="power_entrada") { puzzle.inspectionText=puzzle.inspectionText.Replace("Registro de pistas: J; objetos: I.","Consulta la ficha del equipo y el objeto compatible."); EditorUtility.SetDirty(puzzle); }
            var objectives=root.GetComponent<NarrativeZoneObjectives>(); Set(zone,"objectiveText",objectives.steps[0].text); Set(zone,"objectiveId",objectives.steps[0].id);
        }
        private static RoomLightingController BuildRoom(Transform parent,Geometry geometry,RoomDefinition d,int index)
        {
            var obj=new GameObject("Room_"+d.id); obj.transform.SetParent(parent,false); var room=obj.AddComponent<RoomLightingController>();
            room.roomId=d.id; room.displayName=d.name; room.roomBounds=d.bounds; room.initialState=d.initial; room.afterEntry=d.settled; room.entryEvent=d.entry; room.failurePattern=d.pattern;
            room.restoreFlag=d.restore; room.normalColor=d.color; room.revealSeconds=new Vector2(d.hold*.85f,d.hold*1.2f); room.blackoutSeconds=d.blackout; room.enterDelay=.55f+index*.06f;
            if(!accessibleCells.Any(cell=>d.bounds.Contains((Vector2)cell*.125f))) room.entryEvent=false;
            room.normalIntensity=d.corridor ? .28f : .36f; room.dimIntensity=.075f; room.defectiveFixture=d.defective ? 0 : -1;
            room.fillLights=new[]{RoomFill(obj.transform,geometry,d.bounds,"RoomAmbient",d.color)};
            room.mainLights=new Light2D[2]; room.fixtures=new GroupLampFlicker2D[2];
            var frames=AssetDatabase.LoadAllAssetsAtPath(Art+"Lighting/luz_parpadeo_spritesheet_4x1.png").OfType<Sprite>().OrderBy(s=>s.name).ToArray();
            for(int i=0;i<2;i++)
            {
                var desired=d.bounds.center+(d.bounds.width>d.bounds.height ? Vector2.right*d.bounds.width : Vector2.up*d.bounds.height)*(i==0 ? -.24f : .24f);
                Vector2 position=RoomFree(geometry,d.bounds,desired); var fixture=new GameObject("CeilingFixture_"+i); fixture.transform.SetParent(obj.transform,false); fixture.transform.position=position;
                var sprite=fixture.AddComponent<SpriteRenderer>(); sprite.sprite=frames[d.initial==RoomLightingState.Blackout ? 2 : 1]; sprite.sharedMaterial=lit; sprite.sortingLayerName="World"; sprite.sortingOrder=WorldYSort2D.Order(position.y);
                var light=fixture.AddComponent<Light2D>(); light.lightType=Light2D.LightType.Point; light.pointLightOuterRadius=Mathf.Clamp(Mathf.Min(d.bounds.width,d.bounds.height)*.85f,2.3f,4.5f); light.pointLightInnerRadius=.35f; light.falloffIntensity=.68f;
                if(d.corridor) { light.lightType=Light2D.LightType.Freeform; float half=Mathf.Min(3.3f,Mathf.Max(d.bounds.width,d.bounds.height)*.22f); bool horizontal=d.bounds.width>d.bounds.height; light.SetShapePath(Chamfered(horizontal ? new Rect(-half,-.32f,half*2,.64f) : new Rect(-.32f,-half,.64f,half*2),.15f)); light.shapeLightFalloffSize=.85f; }
                ConfigureRoomLight(light,d.color); room.mainLights[i]=light;
                var flicker=fixture.AddComponent<GroupLampFlicker2D>(); Set(flicker,"lamp",light); Set(flicker,"lampRenderer",sprite); SetObjectArray(flicker,"frames",frames);
                Set(flicker,"randomSeed",index*997+i*61+d.id.Length*101); Set(flicker,"onSeconds",new Vector2(12f+index,25f+index)); Set(flicker,"offSeconds",new Vector2(.06f,.05f)); Set(flicker,"failureProbability",.35f); Set(flicker,"minimumIntensity",0f); Set(flicker,"intensity",.36f);
                flicker.failurePattern=d.pattern; room.fixtures[i]=flicker;
            }
            var aux=Eyes(obj.transform,RoomFree(geometry,d.bounds,d.bounds.center),"GuideLight"); aux.color=d.color; aux.pointLightOuterRadius=.75f; aux.intensity=.04f; room.auxiliaryLights=new[]{aux};
            var trigger=obj.AddComponent<BoxCollider2D>(); trigger.isTrigger=true; trigger.offset=d.bounds.center; trigger.size=d.bounds.size;
            float initial=RoomLightingLogic.Level(d.initial,room.normalIntensity,room.dimIntensity); foreach(var fixture in room.fixtures) fixture.SetExternalPower(initial);
            foreach(var fill in room.fillLights) fill.intensity=initial*.6f;
            placement.Add(Scenes[index]+" / "+room.displayName+" bounds="+d.bounds+"; entry="+room.entryEvent+"; reveal="+room.revealSeconds+"; blackout="+d.blackout);
            return room;
        }
        private static Vector2 RoomFree(Geometry geometry,Rect bounds,Vector2 desired)
        {
            // Ceiling sources need an unobstructed anchor, not a walking route from spawn.
            bool FreeAnchor(Vector2 p)=>bounds.Contains(p) && !geometry.collisions.Any(c=>c.Bounds.Overlaps(new Rect(p.x-.12f,p.y-.12f,.24f,.24f)));
            if(FreeAnchor(desired)) return desired;
            for(int r=1;r<100;r++) for(int x=-r;x<=r;x++) for(int y=-r;y<=r;y++)
            { if(Math.Abs(x)!=r && Math.Abs(y)!=r) continue; var point=desired+new Vector2(x,y)*.125f; if(FreeAnchor(point)) return point; }
            throw new InvalidOperationException("No free ceiling source inside room "+bounds);
        }
        private static Light2D RoomFill(Transform root,Geometry g,Rect bounds,string name,Color color)
        {
            var obj=new GameObject(name); obj.transform.SetParent(root,false); obj.transform.position=RoomFree(g,bounds,bounds.center);
            var light=obj.AddComponent<Light2D>(); light.lightType=Light2D.LightType.Freeform;
            var inset=new Rect(bounds.xMin+.75f,bounds.yMin+.75f,bounds.width-1.5f,bounds.height-1.5f);
            light.SetShapePath(Chamfered(inset,.5f).Select(v=>v-obj.transform.position).ToArray()); light.shapeLightFalloffSize=.7f; ConfigureRoomLight(light,color); return light;
        }
        private static Vector3[] Chamfered(Rect r,float corner)
        {
            float c=Mathf.Min(corner,Mathf.Min(r.width,r.height)*.2f);
            return new[]{new Vector3(r.xMin+c,r.yMin),new Vector3(r.xMax-c,r.yMin),new Vector3(r.xMax,r.yMin+c),new Vector3(r.xMax,r.yMax-c),new Vector3(r.xMax-c,r.yMax),new Vector3(r.xMin+c,r.yMax),new Vector3(r.xMin,r.yMax-c),new Vector3(r.xMin,r.yMin+c)};
        }
        private static void ConfigureRoomLight(Light2D light,Color color)
        { light.color=color; light.intensity=0; light.shadowsEnabled=true; light.shadowIntensity=.85f; light.shadowSoftness=.3f; light.targetSortingLayers=SortingLayer.layers.Select(s=>s.id).ToArray(); }
    }
}
#endif
