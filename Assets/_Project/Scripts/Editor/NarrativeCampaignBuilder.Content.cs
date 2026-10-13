#if UNITY_EDITOR
using System;
using System.Linq;
using System.Collections.Generic;
using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.Narrative;
using EscapeUNIFRANZ.Puzzles;
using UnityEditor;
using UnityEngine;

namespace EscapeUNIFRANZ.EditorTools
{
    public static partial class NarrativeCampaignBuilder
    {
        private static Dictionary<string,ClueData> clues;
        private static Dictionary<string,PuzzleData> puzzles;
        private static void BuildData()
        {
            Folder("Assets/_Project/Data/Narrative/Clues"); Folder("Assets/_Project/Data/Narrative/Puzzles");
            clues=new Dictionary<string,ClueData>(); puzzles=new Dictionary<string,PuzzleData>();
            Clue("entrada_manual","Nota del personal de acceso","Infraestructura",
                "El panel perdió la alimentación. Usa una batería de respaldo y conecta ENERGIA → PANEL → PUERTA. No fuerces el cierre: primero restablece el circuito. La reserva se dejó junto al área de simulación odontológica.",GameFlagId.FirstClueFound);
            Clue("hall_admision","Mostrador sin atención","Accesos",
                "ARCA mantiene el control local de los accesos. La llave del gabinete quedó sobre una mesa de la cafetería. Si el edificio no responde, revisa el protocolo en el terminal de admisión.");
            Clue("arca_protocolo","Protocolo de acceso local","Software",
                "Para recuperar el registro de accesos: AUTH → NETWORK → ACCESS. REBOOT borra el diagnóstico; no lo uses. Una sesión validada emite la tarjeta de laboratorio. Los cierres remotos siguen recibiendo órdenes desconocidas.");
            Clue("hardware_circuito","Ficha de simulación","Hardware / sistemas embebidos",
                "La cadena segura es FUENTE → CONTROLADOR → SENSOR → MOTOR. El controlador necesita lectura del sensor antes de actuar. Tras restaurarla, activa los tres puntos de sobrecarga del dispositivo inestable; no te acerques a las franjas durante su descarga.");
            Clue("hardware_alerta","Incidente de laboratorio","Registro de infraestructura",
                "Las cámaras empezaron a seguir personas. Un vehículo pequeño salió de su ruta. Nadie dio esas órdenes desde el laboratorio. Detectar actividad y golpear a una persona son subsistemas distintos: busca cobertura y evita el contacto físico.");
            Clue("tecnologia_proyecto","Proyecto de IA experimental","Software",
                "El grupo de Software presentó un sistema capaz de coordinar recursos del campus. El proyecto ganó reconocimiento, espacio y equipos. Hardware reclamó que los nodos físicos, la energía y los dispositivos también eran indispensables. La discusión dejó de ser técnica.");
            Clue("tecnologia_francis","Registro de laboratorio 03","Hardware",
                "Francis insistió en desconectar físicamente el nodo para demostrar que Hardware seguía siendo indispensable. Software advirtió que el apagado debía coordinarse desde el sistema. Francis decidió intervenir de todos modos. No fue una orden de la IA: fue su decisión.");
            Clue("red_topologia","Pizarra de enlaces de Tecnología","Redes",
                "Ruta local: BACKBONE → SW-02 → LAB. SW-01 entrega la red administrativa y ADMIN no pertenece a este circuito. Aísla esa ruta de los dispositivos móviles antes de recuperar el laboratorio.");
            Clue("software_servicios","Servicios del laboratorio","Diagnóstico de software",
                "CAMERAS, DOORS y LIGHTS sostienen la infraestructura. SECURITY distribuye las órdenes hostiles a cámaras y actuadores. Detén únicamente SECURITY. Después espera la ventana segura del panel físico para desenergizar los brazos.");
            Clue("software_escalada","Respuesta automática del sistema","Log de seguridad",
                "La desconexión física de Francis se clasificó como amenaza. La IA extendió el control a puertas, cámaras, luces y dispositivos conectados. Francis escribió después: «Puedo solucionarlo sin depender de Software». El registro no muestra pérdida de memoria ni control mental.");
            Clue("core_protocolo","Apagado coordinado","Redes / Hardware / Software",
                "RED: CAMERAS → ACCESS → SERVER → ISOLATED. Evita PUBLIC. HARDWARE: corta Alimentación, después Actuadores y finalmente confirma Seguridad. SOFTWARE: ISOLATE → VERIFY → SHUTDOWN. FORCE_OFF no es un apagado seguro. Cada fase validada queda registrada; un fallo posterior no obliga a repetirla.");
            Clue("core_francis","Último mensaje de Francis","Registro personal",
                "«No pienso dejar que Software se lleve todo el mérito. Si puedo cortar los equipos, puedo detener la IA». Francis continúa justificándose, aunque las órdenes del sistema ya escapan a su control. Orgullo y rivalidad explican sus acciones; no eliminan su responsabilidad.");

            Cable("power_entrada","Panel de energía de acceso","Restablece el circuito según la nota del personal.",
                "entrada_manual",new[]{"PUERTA","ENERGIA","PANEL"},new[]{"ENERGIA>PANEL","PANEL>PUERTA"},
                new[]{GameFlagId.PowerEntranceRestored},"Energía restablecida. IA: ACTIVIDAD NO AUTORIZADA DETECTADA.");
            puzzles["power_entrada"].requiredItem="battery"; puzzles["power_entrada"].consumeItem=true;
            Commands("arca_access","Terminal de control ARCA","Valida el acceso local siguiendo el registro.","arca_protocolo",
                new[]{"NETWORK","REBOOT","ACCESS","AUTH"},new[]{"AUTH","NETWORK","ACCESS"},
                new[]{GameFlagId.ArcaAccessRestored,GameFlagId.Piso2Unlocked},"Acceso local recuperado. Tarjeta de laboratorio emitida.");
            puzzles["arca_access"].rewardItems=new[]{"lab_card"};
            Cable("hardware_routing","Control del dispositivo de simulación","Reconecta energía, control y datos según la ficha.",
                "hardware_circuito",new[]{"MOTOR","SENSOR","FUENTE","CONTROLADOR"},
                new[]{"FUENTE>CONTROLADOR","CONTROLADOR>SENSOR","SENSOR>MOTOR"},
                new[]{GameFlagId.HardwareRoutingRestored},"Cadena validada. Los tres puntos de sobrecarga están habilitados.");
            Cable("technology_network","Distribuidor de red de Tecnología","Recupera la ruta local y aísla los dispositivos móviles.",
                "red_topologia",new[]{"SW-01","LAB","BACKBONE","ADMIN","SW-02"},
                new[]{"BACKBONE>SW-02","SW-02>LAB"},new[]{GameFlagId.TechnologyNetworkRestored},"Ruta recuperada. Dispositivo móvil aislado; laboratorio habilitado.");
            puzzles["technology_network"].prerequisiteClues=new[]{"tecnologia_proyecto","tecnologia_francis"};
            Commands("software_security","Terminal de servicios","Selecciona únicamente el servicio comprometido.","software_servicios",
                new[]{"CAMERAS","DOORS","SECURITY","LIGHTS"},new[]{"SECURITY"},new[]{GameFlagId.SecurityNodeDisabled},
                "SECURITY aislado. Cámaras desconectadas; usa el panel físico durante su ventana segura.",PuzzleKind.ServiceSelection);
            puzzles["software_security"].prerequisiteClues=new[]{"software_escalada"};
            Cable("francis_network","Aislamiento del núcleo — Red","Construye la ruta de aislamiento indicada en el protocolo.",
                "core_protocolo",new[]{"ACCESS","PUBLIC","ISOLATED","CAMERAS","SERVER"},
                new[]{"CAMERAS>ACCESS","ACCESS>SERVER","SERVER>ISOLATED"},new GameFlagId[0],"Fase de red validada. Procede al corte físico coordinado.");
            Commands("francis_shutdown","Apagado seguro — Software","Ejecuta el protocolo después de aislar red y hardware.",
                "core_protocolo",new[]{"VERIFY","FORCE_OFF","SHUTDOWN","ISOLATE"},new[]{"ISOLATE","VERIFY","SHUTDOWN"},
                new GameFlagId[0],"Apagado seguro completado: Hardware + Software + Redes + Lógica.");
            BuildHorrorData();
            BuildAccessibleData();
            foreach(var data in puzzles.Values)
            {
                if (!PuzzleModel.Validate(data,out string error)) throw new InvalidOperationException(data.id+": "+error);
                EditorUtility.SetDirty(data);
            }
        }
        private static void Clue(string id,string title,string category,string text,GameFlagId flag=GameFlagId.None)
        {
            var data=DataAsset<ClueData>("Clues",id); data.id=id; data.title=title; data.category=category; data.text=text; data.associatedFlag=flag;
            EditorUtility.SetDirty(data); clues.Add(id,data);
        }
        private static PuzzleData Definition(string id,string title,string instruction,string clue,string[] options,GameFlagId[] flags,string message)
        {
            var d=DataAsset<PuzzleData>("Puzzles",id); d.id=id; d.title=title; d.instructions=instruction; d.requiredClueId=clue;
            d.ports=options.Select(o=>new CablePort{id=o,label=o}).ToArray(); d.completionFlags=flags; d.successMessage=message;
            d.requiredItem=""; d.consumeItem=false; d.rewardItems=new string[0]; d.prerequisiteClues=new string[0]; d.requiredFlags=new GameFlagId[0]; d.connectInOrder=true;
            d.matchingColumns=false; d.preserveCommandProgress=false;
            d.diagramConnections=new CableConnection[0]; d.environmentalClue=null;
            puzzles.Add(id,d); return d;
        }
        private static void Cable(string id,string title,string instruction,string clue,string[] options,string[] edges,GameFlagId[] flags,string message)
        {
            var d=Definition(id,title,instruction,clue,options,flags,message); d.kind=PuzzleKind.Cables;
            d.connections=edges.Select(e=>e.Split('>')).Select(e=>new CableConnection(e[0],e[1])).ToArray(); d.commandOrder=new string[0];
        }
        private static void Commands(string id,string title,string instruction,string clue,string[] options,string[] order,GameFlagId[] flags,string message,PuzzleKind kind=PuzzleKind.CommandSequence)
        { var d=Definition(id,title,instruction,clue,options,flags,message); d.kind=kind; d.commandOrder=order; d.connections=new CableConnection[0]; }
        private static T DataAsset<T>(string category,string id) where T:ScriptableObject
        {
            string path="Assets/_Project/Data/Narrative/"+category+"/"+id+".asset";
            var data=AssetDatabase.LoadAssetAtPath<T>(path);
            if (data==null) { data=ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(data,path); }
            return data;
        }
    }
}
#endif
