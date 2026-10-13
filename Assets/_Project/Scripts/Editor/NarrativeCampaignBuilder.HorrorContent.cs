#if UNITY_EDITOR
using System.Linq;
using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.Puzzles;
namespace EscapeUNIFRANZ.EditorTools
{
    public static partial class NarrativeCampaignBuilder
    {
        private static void BuildHorrorData()
        {
            clues["entrada_manual"].text="El acceso perdió energía. Inserta la batería de reserva del área dental y enlaza etiquetas iguales: PWR energía, DATA datos, CTRL control, GND tierra. Los bornes del destino cambiaron de posición. Después del arranque revisa CCTV; el lector requiere autorización local.";
            clues["hardware_circuito"].text="Arranque: POWER → CPU → NETWORK → ACTUATOR. Instala un fusible antes de energizar. El conexionado del equipo se valida GND → PWR → DATA → CTRL. Una CPU activa no implica un actuador seguro. Recupera la tarjeta de mantenimiento y vuelve al gabinete del Hall. Luego usa los tres puntos de sobrecarga durante sus ventanas seguras.";
            clues["software_servicios"].text="SECURITY_CAM distribuye órdenes hostiles a cámaras y actuadores. Detén únicamente SECURITY_CAM. CORE_AI coordina recursos: forzar su apagado ahora compromete puertas y energía. Después usa la tarjeta de laboratorio y espera la ventana física segura de los brazos.";
            clues["tecnologia_proyecto"].text+="\nCorreo de Francis: «Solicito acceso al rack. Quiero comprobar cuánto depende ese reconocimiento de nuestros equipos».";
            clues["tecnologia_francis"].text+="\nLog físico: HARDWARE_INTERRUPT / puerto retirado manualmente / autor Francis. Las decisiones dejaron evidencia en el rack.";
            Clue("cctv_codigo","CAM 1 — placa del Hall","Observación CCTV","En el feed del Hall, una placa junto al lector muestra 3174. La patrulla cruza el pasillo y regresa; espera a que se aleje antes de salir del terminal.");
            Clue("hall_mantenimiento","Gabinete de mantenimiento","Objetos / backtracking","El gabinete del Hall requiere tarjeta de mantenimiento. La dejó el personal en el controlador de Simulación. Regresa aquí cuando la recuperes: el módulo de red del gabinete sirve para reparar Tecnología.");
            Clue("power_confirm","Bornes del acceso","Inspección ambiental","Dos conductores fueron retirados manualmente. Las cuatro etiquetas siguen legibles: PWR, DATA, CTRL y GND. No conectes por color; el destino conserva las etiquetas con otro orden.");
            Clue("hardware_confirm","Etiqueta del controlador","Inspección ambiental","El equipo marca GND → PWR → DATA → CTRL. El LED de NETWORK permanece apagado y el zócalo del fusible está vacío. El motor no debe recibir órdenes antes de validar CPU y red.");
            Clue("network_confirm","Rack abierto","Inspección ambiental","SW-01: ADMIN. SW-02: LAB. Un módulo falta en el distribuidor. La abrazadera está abierta a mano; no fue una sobrecarga automática.");
            Clue("software_confirm","Monitor de procesos","Inspección ambiental","SECURITY_CAM: envío de órdenes elevado. CORE_AI: coordinación. DOORS y LIGHTS: servicios de infraestructura. El monitor confirma el diagnóstico escrito del laboratorio.");
            Clue("arca_confirm","Pantalla de admisión","Inspección ambiental","El terminal conserva AUTH, NETWORK y ACCESS. El aviso de REBOOT advierte pérdida del diagnóstico actual.");
            Clue("core_confirm","Consola del núcleo","Inspección ambiental","Los conectores físicos están rotulados RED, ALIMENTACIÓN y ACTUADORES. La pantalla exige aislar, verificar y apagar. PUBLIC permanece conectado al campus; no lo incluyas en la ruta de aislamiento.");
            Matching("power_entrada",false);
            puzzles["power_entrada"].successMessage="Circuito validado. SYSTEM_PARTIALLY_RESTORED.";
            puzzles["power_entrada"].prerequisiteClues=new[]{"power_confirm"};
            Matching("hardware_routing",true);
            puzzles["hardware_routing"].requiredFlags=new[]{GameFlagId.SimulationPowerRestored};
            puzzles["hardware_routing"].prerequisiteClues=new[]{"hardware_confirm"};
            Commands("simulation_power","Arranque de Simulación","Instala el fusible y valida el arranque según la ficha.","hardware_circuito",
                new[]{"ACTUATOR","CPU","POWER","NETWORK","RESET"},new[]{"POWER","CPU","NETWORK","ACTUATOR"},new[]{GameFlagId.SimulationPowerRestored},"Energía y red recuperadas. Tarjeta de mantenimiento disponible: vuelve al Hall.");
            puzzles["simulation_power"].requiredItem="fuse"; puzzles["simulation_power"].consumeItem=true;
            puzzles["simulation_power"].rewardItems=new[]{"maintenance_card"}; puzzles["simulation_power"].preserveCommandProgress=true;
            puzzles["simulation_power"].prerequisiteClues=new[]{"hardware_confirm"};
            Commands("maintenance_hall","Gabinete del Hall","Autoriza la tarjeta y extrae el módulo después del diagnóstico.","hall_mantenimiento",
                new[]{"EXTRACT","AUTH","DIAGNOSE","FORCE"},new[]{"AUTH","DIAGNOSE","EXTRACT"},new[]{GameFlagId.MaintenanceAccessRestored},"Gabinete abierto. Módulo de red recuperado; regresa a Tecnología.");
            puzzles["maintenance_hall"].requiredItem="maintenance_card"; puzzles["maintenance_hall"].rewardItems=new[]{"network_module"};
            Commands("entrance_code","Lector de autorización","Introduce los cuatro dígitos observados en CCTV. Un error reinicia solo la entrada de código.","cctv_codigo",
                Enumerable.Range(0,10).Select(i=>i.ToString()).ToArray(),new[]{"3","1","7","4"},new[]{GameFlagId.EntranceCodeAccepted},"AUTORIZACIÓN OK. Acceso al Hall recuperado; checkpoint conservado.");
            puzzles["entrance_code"].requiredFlags=new[]{GameFlagId.FirstThreatActivated};
            puzzles["technology_network"].requiredItem="network_module"; puzzles["technology_network"].consumeItem=true;
            puzzles["technology_network"].prerequisiteClues=new[]{"tecnologia_proyecto","tecnologia_francis","network_confirm"};
            puzzles["software_security"].ports=new[]{"CORE_AI","SECURITY_CAM","DOORS","LIGHTS"}.Select(s=>new CablePort{id=s,label=s}).ToArray();
            puzzles["software_security"].commandOrder=new[]{"SECURITY_CAM"}; puzzles["software_security"].prerequisiteClues=new[]{"software_escalada","software_confirm"};
            puzzles["arca_access"].prerequisiteClues=new[]{"arca_confirm"};
            puzzles["francis_network"].prerequisiteClues=new[]{"core_confirm"}; puzzles["francis_shutdown"].prerequisiteClues=new[]{"core_confirm","core_francis"};
        }
        private static void Matching(string id,bool ordered)
        {
            var d=puzzles[id]; string[] sources={"PWR","DATA","CTRL","GND"}; string[] targets={"CTRL","GND","PWR","DATA"};
            d.ports=sources.Select((s,i)=>new CablePort{id="IN_"+s,label=s+" / "+Signal(s),row=i}).Concat(targets.Select((s,i)=>new CablePort{id="OUT_"+s,label=s+" / "+Signal(s),output=true,row=i})).ToArray();
            string[] order=ordered ? new[]{"GND","PWR","DATA","CTRL"} : sources;
            d.connections=order.Select(s=>new CableConnection("IN_"+s,"OUT_"+s)).ToArray(); d.matchingColumns=true; d.connectInOrder=ordered;
            d.instructions="Arrastra desde la columna izquierda y suelta sobre el destino. También puedes seleccionar ambos puertos. Identifica las etiquetas y consulta la inspección del equipo.";
        }
        private static string Signal(string s)=>s=="PWR" ? "ENERGÍA" : s=="DATA" ? "DATOS" : s=="CTRL" ? "CONTROL" : "TIERRA";
    }
}
#endif
