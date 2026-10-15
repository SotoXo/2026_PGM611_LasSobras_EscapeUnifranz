#if UNITY_EDITOR
using System.Linq;
using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.Hazards;
using EscapeUNIFRANZ.Interaction;
using EscapeUNIFRANZ.Map;
using EscapeUNIFRANZ.Narrative;
using EscapeUNIFRANZ.Player;
using EscapeUNIFRANZ.UI;
using EscapeUNIFRANZ.World;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using Object=UnityEngine.Object;
namespace EscapeUNIFRANZ.EditorTools
{
    public static partial class NarrativeCampaignBuilder
    {
        private static ItemData[] items;
        private static void BuildInteractiveData()
        {
            Folder("Assets/_Project/Data/Narrative/Items");
            items=new[]{
                Item("battery","Batería portátil","Alimentación auxiliar para el panel de Entrada.","Batería de respaldo. Sus contactos encajan en el panel de acceso. Instálala primero; después tendrás que reparar ENERGÍA y DATOS.","bateria.png",true,ItemCategory.Consumable,"BAT"),
                Item("key","Llave de ARCA","Abre el acceso mecánico desde el Hall.","Llave del gabinete de control. La etiqueta dice ARCA. Puede usarse varias veces; no se consume al abrir el acceso.","llave.png",false,ItemCategory.KeyItem,"KEY"),
                Item("flashlight","Linterna","Ilumina en la dirección en que miras. F — Encender / apagar.","Linterna del personal. El foco frontal permite explorar más allá del pequeño radio cercano. Energía ilimitada en esta versión. Apagada: conservas visión cercana y las luces del escenario.","linterna.png",false,ItemCategory.Tool,"F"),
                Item("fuse","Fusible de respaldo","Reemplaza el fusible dañado en Simulación.","Fusible compatible con la caja de Simulación. Consulta la ficha del circuito y arrástralo al hueco vacío. Una vez instalado deja de estar disponible.","bateria.png",true,ItemCategory.Consumable,"FUS"),
                Item("lab_card","Tarjeta de laboratorio","Autoriza el laboratorio y su parada de emergencia.","Tarjeta emitida por ARCA. Destino: LABORATORIO DE HARDWARE / SOFTWARE. Solo autoriza accesos; no repara la red ni desactiva por sí sola el servicio Seguridad.",null,false,ItemCategory.KeyItem,"LAB"),
                Item("maintenance_card","Tarjeta de mantenimiento","Autoriza el gabinete de comunicaciones del Hall.","Tarjeta amarilla. Nivel de acceso: Mantenimiento. Detrás dice: PISO 1 — SERVICIO TÉCNICO. Úsala en el gabinete del Hall para recuperar el módulo de red.",null,false,ItemCategory.KeyItem,"MNT"),
                Item("network_module","Módulo de red","Instálalo en el distribuidor de Tecnología.","Módulo extraído del gabinete de comunicaciones. Permite recuperar una ruta local. La nota marca SW-02: revisa también la topología y los registros de Tecnología antes de conectar.",null,true,ItemCategory.Consumable,"RED")};
            items.First(i=>i.id=="flashlight").immediateUse=ImmediateItemUse.Flashlight; items.First(i=>i.id=="fuse").placeholderIcon=true;
            ItemData.Register(items);
            var power=puzzles["power_entrada"]; power.installBeforePuzzle=true; power.installedFlag=GameFlagId.EntranceBatteryInstalled; power.consumeItem=false;
            foreach(var item in items) EditorUtility.SetDirty(item);
        }
        private static ItemData Item(string id,string name,string description,string examination,string image,bool consumable,ItemCategory category,string symbol)
        {
            var item=DataAsset<ItemData>("Items",id); item.id=id; item.displayName=name; item.description=description; item.examineText=examination; item.consumable=consumable; item.category=category; item.fallbackIcon=symbol;
            item.icon=image==null ? null : Sprite(Art+"Items/"+image); item.placeholderIcon=image==null; item.immediateUse=ImmediateItemUse.None; return item;
        }
        private static T Ref<T>(Object owner,string field) where T:Object => new SerializedObject(owner).FindProperty(field).objectReferenceValue as T;
        private static void BuildInteractiveUI(GameRuntimeRoot runtime,Transform ui,NarrativeController controller,PlayerVisionController vision)
        {
            SetObjectArray(controller,"itemCatalog",items); Set(vision,"immediateRadius",1.1f); Set(vision,"immediateIntensity",.38f); vision.SetFlashlightState(false,false);
            var flow=runtime.GetComponentInChildren<GameFlowView>(true); var hud=Ref<GameObject>(flow,"gameplayHudRoot").transform;
            foreach(string name in new[]{"EquipmentStatus","PickupNotice","PickupIcon"}) { var old=hud.Find(name); if(old!=null) Object.DestroyImmediate(old.gameObject); }
            var equipment=Text(hud,"EquipmentStatus",new Vector2(0,-380f),new Vector2(820,36),15f); Set(controller,"equipmentLabel",equipment);
            var toast=Text(hud,"PickupNotice",new Vector2(0,-305),new Vector2(620,94),18f); Set(controller,"pickupLabel",toast); toast.gameObject.SetActive(false);
            var icon=Image(hud,"PickupIcon",Color.white,new Vector2(-355,-305),new Vector2(50,50)).GetComponent<Image>(); icon.preserveAspect=true; icon.gameObject.SetActive(false); Set(controller,"pickupIcon",icon);
            var pause=Ref<GameObject>(flow,"pauseRoot").transform.Find("PauseCard");
            string[] names={"Resume","Inventory","Map","Journal","Controls","Menu"}; string[] captions={"CONTINUAR","INVENTARIO","MAPA","PISTAS","CONTROLES","MENÚ PRINCIPAL"};
            string[] fields={"resumeButton","pauseInventoryButton","pauseMapButton","pauseJournalButton","pauseControlsButton","pauseMenuButton"};
            for(int i=0;i<names.Length;i++)
            { var old=pause.Find(names[i]); var button=old!=null ? old.GetComponent<Button>() : Button(pause,names[i],captions[i],Vector2.zero); button.GetComponent<RectTransform>().anchoredPosition=new Vector2(0,170-i*65); button.GetComponentInChildren<TMP_Text>(true).text=captions[i]; Set(flow,fields[i],button); }
            var restart=Ref<Button>(flow,"pauseRestartButton"); if(restart!=null) restart.gameObject.SetActive(false);
            var controls=Ref<GameObject>(flow,"controlsRoot").transform.Find("ControlsCard");
            foreach(Transform child in controls.Cast<Transform>().ToArray()) if(child.name.StartsWith("Row_",System.StringComparison.Ordinal)) Object.DestroyImmediate(child.gameObject);
            string[] keys={"WASD / FLECHAS","E","F","I","J","TAB","M","ESC"};
            string[] actions={"Moverse","Interactuar / usar objeto","Linterna — requiere recogerla","Inventario: seleccionar / examinar","Pistas leídas","Scanner con línea de visión","Mapa — requiere recuperar plano","Pausa / cerrar / volver"};
            for(int i=0;i<keys.Length;i++)
            { var row=Image(controls,"Row_"+i,new Color(.04f,.09f,.12f),new Vector2(0,155-i*50),new Vector2(700,44)); var key=Text(row,"Key",new Vector2(-215,0),new Vector2(245,40),16f); key.text=keys[i]; key.alignment=TextAlignmentOptions.Left; var action=Text(row,"Action",new Vector2(135,0),new Vector2(410,40),16f); action.text=actions[i]; action.alignment=TextAlignmentOptions.Right; }
            var map=runtime.GetComponentInChildren<CampusMapView>(true); var mapRoot=Ref<GameObject>(map,"root").transform;
            foreach(string name in new[]{"SectorDetail","MapObjective","CloseInteractive","MapLegend"}) { var old=mapRoot.Find(name); if(old!=null) Object.DestroyImmediate(old.gameObject); }
            foreach(var node in mapRoot.GetComponentsInChildren<CampusMapNodeView>(true))
            { var rect=node.GetComponent<RectTransform>(); rect.anchoredPosition+=Vector2.left*320f; Ensure<Button>(node.gameObject); node.GetComponent<Image>().raycastTarget=true; }
            // Reset node positions so rebuilding remains deterministic.
            string[] ids={"entrada","hall","arca","piso2","piso3","lab_software","nucleo_ia"}; var positions=new[]{new Vector2(-330,260),new Vector2(-330,165),new Vector2(-65,165),new Vector2(-330,65),new Vector2(-330,-35),new Vector2(-330,-135),new Vector2(-330,-235)};
            for(int i=0;i<ids.Length;i++) mapRoot.Find("MapNode_"+ids[i]).GetComponent<RectTransform>().anchoredPosition=positions[i];
            string[] lines={"LineEntradaHall","LineHallArca","LineHallPiso2","LinePiso2Piso3","LinePiso3Lab","LineLabNucleo"}; var linePositions=new[]{new Vector2(-330,213),new Vector2(-195,165),new Vector2(-330,115),new Vector2(-330,15),new Vector2(-330,-85),new Vector2(-330,-185)};
            var images=new Image[lines.Length]; for(int i=0;i<lines.Length;i++) { var rect=mapRoot.Find(lines[i]).GetComponent<RectTransform>(); rect.anchoredPosition=linePositions[i]; images[i]=rect.GetComponent<Image>(); rect.SetAsFirstSibling(); } SetObjectArray(map,"connectionLines",images);
            var detail=Text(mapRoot,"SectorDetail",new Vector2(330,55),new Vector2(450,465),18f); detail.alignment=TextAlignmentOptions.TopLeft; Set(map,"detail",detail);
            var goal=Text(mapRoot,"MapObjective",new Vector2(0,-340),new Vector2(1030,72),18f); Set(map,"objective",goal);
            Text(mapRoot,"MapLegend",new Vector2(305,-225),new Vector2(460,50),15f).text="VERDE: disponible • ROJO: bloqueo\nAMARILLO: condición • GRIS: desconocido";
            Set(map,"closeButton",Button(mapRoot,"CloseInteractive","CERRAR / M / ESC",new Vector2(335,-280)));
        }
        private static void BuildInteractiveZone(int index,Transform root,ZoneContext zone,Geometry geometry)
        {
            foreach(var pickup in zone.GetComponentsInChildren<ItemPickupInteractable>(true))
            { pickup.item=items.FirstOrDefault(i=>i.id==pickup.itemId); if(pickup.itemId=="flashlight" || index!=0 && pickup.itemId=="battery") pickup.gameObject.SetActive(false); }
            if(index==0)
            {
                var spawn=zone.GetComponentsInChildren<SpawnPoint>(true).First(s=>s.SpawnId==zone.DefaultCheckpointSpawnId).Position;
                var obj=WorldObject(root,geometry,"Pickup_Linterna_Entrada",spawn+new Vector2(2.3f,.8f),Sprite(Art+"Items/linterna.png"));
                if(Vector2.Distance(obj.transform.position,spawn)<1.25f) throw new System.InvalidOperationException("Flashlight must require exploration.");
                var pickup=obj.AddComponent<ItemPickupInteractable>(); pickup.itemId="flashlight"; pickup.pickupId="entrada_linterna_personal"; pickup.item=items.First(i=>i.id=="flashlight"); Track(obj,"entrada_linterna_personal",ScanCategory.Interactable);
                var power=root.Find("Panel_power_entrada"); Set(power.GetComponent<EscapeUNIFRANZ.Puzzles.PuzzleInteractable>(),"prompt","[E] Reparar conexiones"); var status=power.GetComponent<TerminalStatus2D>(); status.powerFlag=GameFlagId.EntranceBatteryInstalled; status.credentialItem=""; status.diagnostic="Alimentación auxiliar. Revisa la nota y repara ENERGÍA / DATOS.";
                var lamp=Eyes(power,power.position,"BackupPanelLight"); lamp.color=new Color(1,.8f,.3f); lamp.pointLightOuterRadius=1.6f; lamp.intensity=0;
                var effect=root.gameObject.AddComponent<WorldStateEffect2D>(); effect.flag=GameFlagId.EntranceBatteryInstalled; effect.restoredLights=new[]{lamp}; effect.monitors=new[]{VisibleRenderer(power.gameObject)};
                root.GetComponent<NarrativeZoneObjectives>().steps=new[]{
                    Step("entrada_linterna","Explora el entorno. Encuentra la linterna del personal.",item:"flashlight"),
                    Step("entrada_bateria","Encuentra una batería en la zona dental para alimentar el panel.",GameFlagId.EntranceBatteryInstalled,"battery"),
                    Step("entrada_instalar","Regresa al panel de acceso e instala la batería.",GameFlagId.EntranceBatteryInstalled),
                    Step("entrada_cables","Lee las notas y repara las conexiones de ENERGÍA y DATOS.",GameFlagId.PowerEntranceRestored),
                    Step("entrada_cctv","Evita el robot o corta su energía. Recupera el plano y el código en CCTV.",GameFlagId.EntranceCodeObserved),
                    Step("entrada_codigo","Valida el código observado en el lector del acceso.",GameFlagId.EntranceCodeAccepted),Step("entrada_salir","Abre el acceso y cruza al Hall.")};
                Inspect(root,geometry,"PC_Recepcion",spawn+new Vector2(.8f,1.8f),"[E] Revisar computadora","SIN CONEXIÓN. El panel de acceso necesita alimentación auxiliar.",false);
                Inspect(root,geometry,"Armario_Personal",spawn+new Vector2(3.1f,1.4f),"[E] Abrir armario","Armario vacío. Falta la linterna del personal; revisa cerca de recepción.",false);
            }
            if(index==1) Inspect(root,geometry,"Monitor_Comprometido",new Vector2(17,7.5f),"[E] Revisar computadora","Sin conexión con el servidor de admisión.",true);
            if(index==4) Inspect(root,geometry,"Pizarra_Red",new Vector2(12.5f,4.2f),"[E] Examinar pizarra","Alguien marcó SW-02 varias veces. La ruta local está indicada en la nota de topología.",false);
            foreach(var interactive in zone.GetComponentsInChildren<InteractableBehaviour>(true)) if(interactive.enabled) Ensure<InteractableFeedback2D>(interactive.gameObject);
            foreach(var marker in zone.GetComponentsInChildren<MinimapTrackable2D>(true))
            {
                Set(marker,"initiallyDiscovered",false);
                var pickup=marker.GetComponent<ItemPickupInteractable>(); var puzzle=marker.GetComponent<EscapeUNIFRANZ.Puzzles.PuzzleInteractable>(); var exit=marker.GetComponent<ZoneExitInteractable>();
                marker.displayName=pickup!=null ? ItemData.Label(pickup.ItemId) : puzzle!=null ? puzzle.puzzle.title : exit!=null ? "Acceso a "+ZoneLabel(exit.TargetZoneId) : marker.GetComponent<SecurityTerminalInteractable>()!=null ? "Terminal CCTV" : marker.GetComponent<SecurityCamera2D>()!=null ? "Cámara de seguridad" : marker.GetComponent<ClueInteractable>()!=null ? "Documento" : !string.IsNullOrEmpty(marker.CheckpointId) ? "Checkpoint" : "Punto de inspección";
                marker.pointKind=pickup!=null ? KnownPointKind.Item : puzzle!=null ? KnownPointKind.Panel : exit!=null ? KnownPointKind.Door : marker.GetComponent<SecurityTerminalInteractable>()!=null ? KnownPointKind.CCTV : marker.GetComponent<SecurityCamera2D>()!=null ? KnownPointKind.Camera : marker.GetComponent<ClueInteractable>()!=null ? KnownPointKind.Document : !string.IsNullOrEmpty(marker.CheckpointId) ? KnownPointKind.Checkpoint : KnownPointKind.Panel;
            }
            foreach(var exit in zone.GetComponentsInChildren<ZoneExitInteractable>(true))
            {
                var led=exit.transform.Find("AccessLED"); exit.statusLight=led!=null ? Ensure<Light2D>(led.gameObject) : Eyes(exit.transform,exit.transform.position,"AccessLED"); exit.statusLight.lightType=Light2D.LightType.Point; exit.statusLight.pointLightOuterRadius=.6f; exit.statusLight.intensity=.25f; exit.statusLight.targetSortingLayers=SortingLayer.layers.Select(s=>s.id).ToArray();
                exit.energyRequirement=index==3 ? GameFlagId.SimulationPowerRestored : GameFlagId.PowerEntranceRestored;
                exit.networkRequirement=index==4 ? GameFlagId.TechnologyNetworkRestored : index==5 ? GameFlagId.SecurityNodeDisabled : index==1 && exit.TargetZoneId=="piso2" ? GameFlagId.ArcaAccessRestored : GameFlagId.None;
                exit.authorizationRequirement=index==0 ? GameFlagId.EntranceCodeAccepted : index==3 && exit.TargetZoneId=="piso3" ? GameFlagId.MaintenanceAccessRestored : GameFlagId.None;
            }
            foreach(var lamp in zone.transform.Find("GroupLevelArt").GetComponentsInChildren<Light2D>(true))
            {
                if(lamp.lightType==Light2D.LightType.Global) { lamp.intensity=index==0 ? .045f : .065f; continue; }
                var flicker=Ensure<GroupLampFlicker2D>(lamp.gameObject); flicker.enabled=true;
                var lampSprite=Ensure<SpriteRenderer>(lamp.gameObject); lampSprite.sharedMaterial=lit; lampSprite.sortingLayerName="World"; lampSprite.sortingOrder=WorldYSort2D.Order(lamp.transform.position.y);
                Set(flicker,"lamp",lamp); Set(flicker,"lampRenderer",lampSprite); Set(flicker,"intensity",lamp.name=="AuthoredLamp_3" ? .22f : .32f);
                var frames=AssetDatabase.LoadAllAssetsAtPath(Art+"Lighting/luz_parpadeo_spritesheet_4x1.png").OfType<Sprite>().OrderBy(s=>s.name).ToArray(); SetObjectArray(flicker,"frames",frames);
                Set(flicker,"onSeconds",new Vector2(4.2f+index*.4f,11f+index)); Set(flicker,"offSeconds",new Vector2(.06f,.08f)); Set(flicker,"shimmer",.015f);
                Set(flicker,"failureProbability",lamp.name=="AuthoredLamp_0" ? .65f : 0f); Set(flicker,"minimumIntensity",0f);
                Set(flicker,"startsOff",lamp.name=="AuthoredLamp_2");
                if(lamp.name=="AuthoredLamp_2") { flicker.SetExternalPower(0f); lampSprite.sprite=frames[2]; }
                else if(lamp.name=="AuthoredLamp_3") { flicker.SetExternalPower(.22f); lampSprite.sprite=frames[1]; }
                else lampSprite.sprite=frames[1];
            }
            var objective=root.GetComponent<NarrativeZoneObjectives>(); foreach(var step in objective.steps) { if(step.id=="sim_salida" || step.id=="arca_volver") step.sectorId="hall"; if(step.id=="hall_arca") step.sectorId="arca"; } Set(zone,"objectiveText",objective.steps[0].text); Set(zone,"objectiveId",objective.steps[0].id);
        }
        private static string ZoneLabel(string id)=>id=="lab_software" ? "Laboratorio" : id=="nucleo_ia" ? "Aulas / Núcleo" : id=="piso2" ? "Simulación" : id=="piso3" ? "Tecnología" : id=="arca" ? "ARCA" : id=="hall" ? "Hall" : "Entrada";
        private static void Inspect(Transform root,Geometry g,string name,Vector2 position,string prompt,string text,bool corrupt)
        {
            var obj=WorldObject(root,g,name,position,Sprite(Art+"Environment/Props/desk_pc.png")); var inspect=obj.AddComponent<EnvironmentalInspectInteractable>(); inspect.explanation=text; inspect.corruptedMonitor=corrupt; inspect.eventId="inspect_"+name; inspect.monitor=VisibleRenderer(obj); Set(inspect,"prompt",prompt); Track(obj,"inspect_"+name,ScanCategory.Interactable);
        }
    }
}
#endif
