#if UNITY_EDITOR
using System.Linq;
using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.Narrative;
using EscapeUNIFRANZ.Puzzles;
using UnityEditor;
namespace EscapeUNIFRANZ.EditorTools
{
    public static partial class NarrativeCampaignBuilder
    {
        private static void BuildAccessibleData()
        {
            Rewrite("entrada_manual","Para abrir el acceso hace falta la <b>batería</b> de la zona dental. Después une <b>ENERGÍA con ENERGÍA</b> y <b>DATOS con DATOS</b>. Son solo dos cables. La energía volverá parcialmente.");
            Rewrite("power_confirm","El hueco de la <b>batería está vacío</b>. Dos etiquetas siguen visibles: <b>ENERGÍA</b> y <b>DATOS</b>. El cierre necesita ambas líneas para responder.");
            Rewrite("hall_admision","La <b>llave</b> del gabinete ARCA quedó en una mesa de la <b>cafetería</b>. Allí podrás recuperar el control local del edificio.");
            Rewrite("arca_protocolo","Reinicia los interruptores <b>de izquierda a derecha</b>: <b>PRINCIPAL → CONTROL → SEGURIDAD</b>. PRINCIPAL alimenta el equipo; CONTROL coordina sus órdenes; SEGURIDAD comprueba que el acceso sea seguro.");
            Rewrite("arca_confirm","Tres interruptores numerados: <b>1 PRINCIPAL / 2 CONTROL / 3 SEGURIDAD</b>. El aviso dice: reiniciar de izquierda a derecha.");
            Rewrite("hardware_circuito","Primero reemplaza el <b>fusible dañado</b>: deja pasar energía de forma segura. Arranca <b>ENERGÍA → PROCESADOR → RED → ACTUADOR</b>, como indican los números. Después une las cuatro etiquetas iguales. El actuador es la pieza que mueve el equipo.");
            Rewrite("hardware_confirm","El zócalo del <b>fusible</b> está vacío. PWR = ENERGÍA; DATA = DATOS; CTRL = CONTROL; GND = TIERRA. Tierra protege el circuito; control indica qué debe hacer. Une cada nombre con su pareja.");
            Rewrite("hardware_alerta","<b>Verte no es tocarte.</b> La luz amarilla y el signo <b>!</b> anuncian una persecución. Usa paredes como cobertura. Los paneles de mantenimiento pueden cortar la alimentación de una unidad durante unos segundos.");
            Rewrite("hall_mantenimiento","<b>GABINETE DE COMUNICACIONES</b> — requiere <b>TARJETA DE MANTENIMIENTO</b>. El personal la dejó en Simulación. Dentro hay un <b>MÓDULO DE RED</b>: una pieza que falta en el distribuidor de Tecnología.");
            Rewrite("red_topologia","El <b>SERVIDOR</b> envía información al edificio. La ruta A está <b>COMPROMETIDA</b>. Usa <b>SWITCH-02</b>, que mantiene la conexión al <b>LAB</b>. Un switch une lugares: busca la ruta azul que evita el nodo rojo.");
            Rewrite("network_confirm","La ruta A muestra una advertencia roja. El cartel de <b>SWITCH-02</b> apunta al <b>LAB</b>. Falta un <b>MÓDULO DE RED</b> en el hueco del distribuidor.");
            Rewrite("tecnologia_proyecto","Software creó una <b>IA experimental</b> para coordinar el campus. El proyecto ganó reconocimiento. Hardware sintió que sus equipos y su trabajo quedaban olvidados. Correo de Francis: «Quiero revisar el rack. Esto también depende de nosotros».");
            Rewrite("tecnologia_francis","Francis pidió acceso al equipo para demostrar que podía detener la IA. Software advirtió que debían coordinar el apagado. Francis decidió intervenir físicamente. Su siguiente registro dice: «<b>Yo podía detenerlo</b>».");
            Rewrite("software_escalada","<b>INTERRUPCIÓN DE HARDWARE DETECTADA</b>. Autor registrado: Francis. Un cable fue retirado a mano. La IA interpretó el corte como amenaza y tomó cámaras, puertas y robots. Francis continúa defendiendo su decisión: «Puedo arreglarlo».");
            Rewrite("software_servicios","<b>No apagues el núcleo.</b> Necesitamos que puertas y luces sigan respondiendo. Aísla únicamente <b>SEGURIDAD</b>, el servicio que envía órdenes hostiles. Después cruza el patrón de los brazos y alcanza <b>EMERGENCY STOP</b>.");
            Rewrite("software_confirm","El monitor muestra: <b>SEGURIDAD — órdenes hostiles</b>; NÚCLEO — coordinación; PUERTAS — accesos. Esto confirma que debes detener solo <b>SEGURIDAD</b>.");
            Rewrite("core_protocolo","Apagado seguro: <b>1 RED</b>, separa cámaras y puertas del servidor. <b>2 HARDWARE</b>, corta energía, actuadores y seguridad. <b>3 SOFTWARE</b>, AISLAR → VERIFICAR → APAGAR. Los pasos terminados quedan registrados.");
            Rewrite("core_confirm","Los conectores dicen <b>CÁMARAS / PUERTAS / SERVIDOR / AISLADO</b>. PUBLIC sigue conectado al campus: evita esa salida. La consola permite apagar solo después de aislar y verificar.");
            Rewrite("core_francis","Francis: «Software construyó algo que no podía controlar. <b>Yo podía detenerlo</b>». Eligió cortar equipos por orgullo. Ahora debes frenar su intervención y la respuesta destructiva de la IA.");
            Rewrite("cctv_codigo","<b>Código observado: 3174.</b> La placa aparece en CAM 02 — Hall. Vuelve al lector del acceso e introduce esos cuatro números.");
            clues["cctv_codigo"].associatedFlag=GameFlagId.EntranceCodeObserved;

            var first=puzzles["power_entrada"];
            first.ports=new[]{Port("IN_PWR","ENERGÍA",0),Port("IN_DATA","DATOS",1),Port("OUT_DATA","DATOS",0,true),Port("OUT_PWR","ENERGÍA",1,true)};
            first.connections=new[]{new CableConnection("IN_PWR","OUT_PWR"),new CableConnection("IN_DATA","OUT_DATA")};
            first.connectInOrder=false; first.matchingColumns=true; first.successMessage="ENERGÍA RESTAURADA.";
            first.inspectionText="PANEL DE ACCESO: falta una batería. Busca en la zona dental. La nota junto al panel explica sus dos conexiones.";
            first.instructions="Conecta las dos líneas con el mismo nombre. Arrastra de izquierda a derecha o selecciona origen y destino.";
            first.environmentalClue=clues["power_confirm"];
            puzzles["hardware_routing"].connectInOrder=false;
            puzzles["hardware_routing"].instructions="Ahora hay cuatro señales. Cada abreviatura aparece junto a su nombre completo. Une las etiquetas iguales; el orden no importa.";
            foreach(var port in puzzles["hardware_routing"].ports) port.label=port.label.Replace(" / ","\n");
            Commands("fuse_simulation","Caja de fusibles","Arrastra el fusible nuevo hasta el hueco vacío.","hardware_circuito",new[]{"IN_FUSE","OUT_SLOT"},new string[0],new[]{GameFlagId.SimulationFuseReplaced},"FUSIBLE REEMPLAZADO. Una lámpara vuelve a encenderse.");
            var fuse=puzzles["fuse_simulation"]; fuse.kind=PuzzleKind.FuseReplacement; fuse.matchingColumns=true;
            fuse.ports=new[]{Port("IN_FUSE","FUSIBLE NUEVO",0),Port("OUT_SLOT","HUECO VACÍO",0,true)};
            fuse.ports[0].icon=Sprite(Art+"Items/bateria.png"); fuse.connections=new[]{new CableConnection("IN_FUSE","OUT_SLOT")};
            fuse.requiredItem="fuse"; fuse.consumeItem=true; fuse.prerequisiteClues=new[]{"hardware_confirm"};
            var energy=puzzles["simulation_power"]; energy.requiredItem=""; energy.consumeItem=false; energy.requiredFlags=new[]{GameFlagId.SimulationFuseReplaced};
            energy.ports=new[]{Port("POWER","1  ENERGÍA",0),Port("CPU","2  PROCESADOR",0),Port("NETWORK","3  RED",0),Port("ACTUATOR","4  ACTUADOR",0)};
            energy.instructions="Enciende los componentes siguiendo los números de la ficha: 1, 2, 3 y 4. Cada paso correcto queda verde.";
            energy.successMessage="SISTEMA RECUPERADO. Tarjeta de mantenimiento obtenida. También sirve en el gabinete del Hall.";
            var arca=puzzles["arca_access"]; arca.ports=new[]{Port("PRINCIPAL","1  PRINCIPAL",0),Port("CONTROL","2  CONTROL",0),Port("SEGURIDAD","3  SEGURIDAD",0)};
            arca.commandOrder=new[]{"PRINCIPAL","CONTROL","SEGURIDAD"}; arca.preserveCommandProgress=true;
            arca.instructions="Reinicia los tres interruptores de izquierda a derecha, como indica la etiqueta.";
            SetLabels("maintenance_hall",new[]{"EXTRACT:RETIRAR MÓDULO","AUTH:USAR TARJETA","DIAGNOSE:REVISAR GABINETE","FORCE:FORZAR CIERRE"});
            SetLabels("technology_network",new[]{"BACKBONE:SERVIDOR","SW-01:RUTA A\nCOMPROMETIDA","SW-02:SWITCH-02","LAB:LAB","ADMIN:HALL"});
            puzzles["technology_network"].prerequisiteClues=new[]{"network_confirm"};
            puzzles["technology_network"].instructions="Lleva la conexión del SERVIDOR al LAB. El nodo rojo está comprometido: busca la otra ruta. La nota destaca el switch útil.";
            puzzles["technology_network"].ports.First(p=>p.id=="SW-01").blocked=true;
            puzzles["technology_network"].diagramConnections=new[]{new CableConnection("BACKBONE","SW-01"),new CableConnection("SW-01","ADMIN"),new CableConnection("BACKBONE","SW-02"),new CableConnection("SW-02","LAB")};
            SetLabels("software_security",new[]{"CORE_AI:APAGAR NÚCLEO","SECURITY_CAM:AISLAR SEGURIDAD","DOORS:ABRIR PUERTA","LIGHTS:REINICIAR CÁMARAS"});
            puzzles["software_security"].prerequisiteClues=new[]{"software_confirm"}; puzzles["francis_shutdown"].prerequisiteClues=new[]{"core_confirm"};
            puzzles["software_security"].instructions="Un servicio envía órdenes hostiles. Selecciona la acción que lo detiene sin apagar el resto del edificio.";
            SetLabels("francis_network",new[]{"CAMERAS:CÁMARAS","ACCESS:PUERTAS","SERVER:SERVIDOR","ISOLATED:AISLADO","PUBLIC:PUBLIC / CAMPUS"});
            SetLabels("francis_shutdown",new[]{"ISOLATE:AISLAR","VERIFY:VERIFICAR","SHUTDOWN:APAGAR","FORCE_OFF:FORZAR CORTE"});
            foreach(var p in puzzles.Values)
            {
                p.objective=p.id=="power_entrada" ? "RESTABLECER ENERGÍA" : p.id=="hardware_routing" ? "CONECTAR EL EQUIPO" : p.id=="fuse_simulation" ? "REEMPLAZAR EL FUSIBLE" : p.id=="technology_network" ? "CONECTAR SERVIDOR CON LAB" : p.id=="software_security" ? "DETENER ÓRDENES HOSTILES" : p.id=="entrance_code" ? "ABRIR EL ACCESO" : p.id=="arca_access" ? "RECUPERAR CONTROL LOCAL" : p.title.ToUpperInvariant();
                if (p.id!="power_entrada") p.inspectionText="Revisa la nota junto al equipo y el objeto requerido. Registro de pistas: J; objetos: I.";
                string env=p.prerequisiteClues.FirstOrDefault(c=>c.EndsWith("confirm"));
                if (env!=null) p.environmentalClue=clues[env];
            }
            fuse.inspectionText="FUSIBLE DAÑADO. Busca el fusible de respaldo en Simulación y colócalo en el hueco vacío.";
            puzzles["technology_network"].inspectionText="Falta el módulo de red. La tarjeta de mantenimiento abre el gabinete del Hall; la etiqueta SWITCH-02 señala el LAB.";
            puzzles["maintenance_hall"].inspectionText="GABINETE DE COMUNICACIONES. Requiere tarjeta de mantenimiento: se obtiene en Simulación.";
            foreach(var clue in clues.Values) EditorUtility.SetDirty(clue);
        }
        private static CablePort Port(string id,string label,int row,bool output=false)=>new CablePort{id=id,label=label,row=row,output=output};
        private static void Rewrite(string id,string text) { clues[id].text=text; }
        private static void SetLabels(string id,string[] labels)
        { foreach(var entry in labels) { var pair=entry.Split(':'); var p=puzzles[id].ports.First(x=>x.id==pair[0]); p.label=pair[1]; } }
    }
}
#endif
