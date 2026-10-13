# EscapeUNIFRANZ — gameplay narrativo

Implementación guardada en `feature/integracion-assets`, sobre los mapas y sistemas existentes. Sin Play Mode, recorrido manual, revisión estética, descargas, staging, commit, push, merge o rebase. Se ejecutaron solamente tests de lógica en EditMode durante la implementación y comprobaciones de compilación/Console.

## 1. Historia implementada

El inicio presenta a un estudiante nuevo de Ingeniería de Sistemas, un fallo eléctrico y un acceso cerrado. Las tres pantallas breves de introducción no revelan a Francis ni explican todo el incidente. La investigación avanza mediante documentos, registros, paneles y servicios del campus.

En Tecnología se descubre el reconocimiento obtenido por el proyecto experimental de Software, la rivalidad con Hardware y la decisión de Francis de intervenir físicamente. El laboratorio registra cómo la IA clasificó esa intervención como amenaza y extendió el control a la infraestructura. Francis se justifica por orgullo y rivalidad; conserva su responsabilidad y no está poseído ni controlado mentalmente.

El final mantiene las tres fases existentes: aislar la red, cortar físicamente energía/actuadores/seguridad y ejecutar el apagado seguro. Francis usa un sprite humano propio ya disponible, con un tinte distinto, en lugar del visual de sombra anterior. No se añadieron armas, ataques ni barra de vida.

## 2. Pistas agregadas

Los doce ClueData están en `Assets/_Project/Data/Narrative/Clues/`. Cada uno contiene ID, título, texto, categoría y flag opcional. El registro de lectura se guarda en el GameState de la partida, sin un inventario de documentos grande. Los documentos se pueden volver a leer con E.

| Zona | Pistas |
|---|---|
| Entrada | Nota del personal de acceso: circuito ENERGIA → PANEL → PUERTA y batería de respaldo |
| Hall | Mostrador sin atención: llave del gabinete en cafetería y referencia al control ARCA |
| ARCA | Protocolo de acceso local: AUTH → NETWORK → ACCESS |
| Piso2 | Ficha de simulación; Incidente de laboratorio |
| Piso3 / Tecnología | Proyecto de IA experimental; Registro de laboratorio 03; Pizarra de enlaces de Tecnología |
| LabSoftware | Servicios del laboratorio; Respuesta automática del sistema |
| NúcleoIA / aulas | Apagado coordinado; Último mensaje de Francis |

El puzzle de Tecnología requiere leer tanto los registros de Software/Francis como la topología. El terminal de servicios requiere también el log de escalada. La solución se obtiene investigando; el Scanner no muestra las conexiones correctas.

## 3. Interacciones y objetos

Se mantienen IInteractable, InteractableBehaviour, InteractionSensor, PlayerInteraction y sus prompts. E lee documentos, recoge objetos, usa terminales, activa puntos físicos y atraviesa accesos habilitados. Los objetos nuevos se reconocen con el Scanner; no tienen texto permanente flotante.

Los PNG reales de batería, llave y linterna que ya estaban colocados según Items_guia ahora se pueden recoger. El inventario es una lista ligera de IDs en GameState. Un identificador de pickup compartido por plano y coordenadas evita recoger dos veces el mismo objeto al pasar entre Entrada/Hall/ARCA o Piso3/LabSoftware.

- La batería se consume al recuperar la alimentación del primer panel.
- La llave permite entrar al control ARCA y se conserva.
- ARCA emite la credencial `lab_card` tras validar el terminal; no se inventó un sprite de tarjeta inexistente.
- La linterna mejora de forma estable la intensidad del foco en 0,12 y su ángulo interior en 4°, sin añadir parpadeo ni alcance infinito.

## 4. Puertas y progresión

ZoneExitInteractable conserva SceneFlowController y añade estados de cierre, requisitos de items/flags y feedback. DoorAccessRules es reutilizable y exige todas las condiciones configuradas. El minimapa consulta la disponibilidad real, incluyendo las credenciales.

| Acceso | Condición |
|---|---|
| Entrada → Hall | Recuperar energía mediante el primer puzzle |
| Hall → ARCA | Tener la llave del gabinete |
| Hall → Piso2 | Validar el terminal ARCA; conserva Piso2Unlocked |
| Piso2 → Piso3 | Restaurar el circuito y desactivar EXOLEG mediante sus puntos físicos |
| Piso3 → LabSoftware | Recuperar la red, aislar el dispositivo móvil y portar la tarjeta |
| LabSoftware → NúcleoIA | Detener SECURITY y desactivar los brazos; conserva NucleoUnlocked |

Los motivos de bloqueo indican falta de energía, llave, tarjeta o sistema recuperado. La progresión se aplica a los accesos entre escenas; los planos compartidos completos se conservan.

## 5–7. Puzzles de cables, ubicación y sistemas

Los siete PuzzleData están en `Assets/_Project/Data/Narrative/Puzzles/`. Puertos, conexiones dirigidas, secuencias, pistas requeridas, consumos, recompensas y flags se definen mediante datos editables.

| Puzzle | Zona | Tipo y solución |
|---|---|---|
| power_entrada | Z00_Entrada | Cables: ENERGIA → PANEL → PUERTA; consume batería |
| arca_access | Z02_ARCA | Secuencia de terminal: AUTH → NETWORK → ACCESS; emite tarjeta |
| hardware_routing | Z03_Piso2 | Cables: FUENTE → CONTROLADOR → SENSOR → MOTOR; habilita sobrecargas |
| technology_network | Z04_Piso3 | Red: BACKBONE → SW-02 → LAB; aísla los dos nodos del encuentro existente |
| software_security | Z05_LabSoftware | Selección de servicio: detener únicamente SECURITY |
| francis_network | Z06_NucleoIA | Cables: CAMERAS → ACCESS → SERVER → ISOLATED |
| francis_shutdown | Z06_NucleoIA | Secuencia: ISOLATE → VERIFY → SHUTDOWN; rechaza FORCE_OFF |

El primer panel está en **Z00_Entrada, posición (6; 3,8)**. La nota está en **(3,2; 4,3)**. La batería conserva su posición de guía dentro del plano de planta baja. Los demás objetos nuevos se colocaron usando espacios libres de Colisiones.png; sus coordenadas están en `reports/narrative-placement.txt`.

Al abrir un panel con E se usa GameplayMode.Puzzle. Click en un puerto inicia una línea UI que sigue al cursor; click en el destino valida el enlace. Las conexiones incorrectas, invertidas, duplicadas o prematuras se rechazan sin modificar el mundo. Las secuencias incorrectas reinician el progreso local. Reiniciar y cerrar no aplican recompensas.

La UI bloquea movimiento/interacción normal, muestra cursor y pausa la simulación mientras se lee o resuelve. Esc/Cerrar devuelve el modo previo Explore o Encounter, cursor y tiempo. La UI de pistas utiliza Dialogue. Se evita que Esc abra simultáneamente el menú de pausa.

Los puzzles completados cambian flags, accesos, luces y monitores y producen mensajes breves. Se conservan los encuentros de sobrecarga, dispositivo móvil y brazos. Los brazos requieren diagnóstico de software y su ventana física segura; no se reemplazó su timing por un combate.

La fase 2 de Francis sigue utilizando tres interacciones físicas ordenadas. Las fases 1 y 3 tienen nuevos paneles; sus resultados se entregan al FrancisBossController original. Se conservan sus flags y checkpoints. Al volver al sector final, SceneFlowController resuelve el checkpoint de fase 2/3 según los flags, en lugar de sobrescribirlo con el de entrada.

## 8. Cámaras

SecurityCamera2D utiliza DetectionSensor2D, con alcance 5,5, campo de 75° y barrido suave. Los sensores direccionales revisan raycasts contra colliders sólidos, ignorando triggers y el cuerpo del jugador como obstáculo. Las paredes y muebles de los mapas bloquean su visión. Los sensores antiguos de patrulleros mantienen su comportamiento circular.

Las cámaras están en Entrada, Hall, Piso2, Piso3, LabSoftware y aulas. Detectar al jugador genera alerta, luz roja y un mensaje de IA con cooldown; no causa muerte ni añade HazardContact a la cámara. Los marcadores de amenazas/cámaras comienzan sin descubrir; aparecen al escanearlas o activar su alerta.

SECURITY desconecta las cámaras de los sectores anteriores. El circuito de aulas permanece independiente hasta completar la fase de red final. Hay UnityEvents y AudioSource opcional para conectar respuestas adicionales de alarma posteriormente.

## 9. Luces

Se conserva GroupLampFlicker2D y el spritesheet propio de cuatro frames. Cada instancia mantiene fase, generador aleatorio y temporizador independiente; conserva tiempos diferentes de encendido/apagado e intensidades. Se añadieron probabilidad de fallo y probabilidad de un corte muy breve, editables desde Inspector.

Las luces estables y apagadas se mantienen. WorldStateEffect2D recupera una luz y cambia el estado visual del monitor al completar energía/accesos/red. LightSequenceTrigger realiza apagados escalonados puntuales en Entrada, simulación y laboratorio, restaura sus estados y se registra una sola vez por partida. El foco del jugador permanece estable.

## 10. PlayerVisionController

Se conserva el facing único de PlayerVisualController. El foco de URP usa +Y local: arriba 0°, abajo 180°, izquierda 90° y derecha −90°. Se mantienen el alcance **6,2**, radio cercano **1,45** y ángulos **88°/60°**, ajustables.

Ahora también la luz cercana proyecta sombras. El emisor se limita mediante raycast desde el cuerpo para evitar que su offset entre en una pared. Ambos focos usan los ShadowCaster2D ya configurados en la geometría real. El pickup de linterna aporta la mejora estable indicada arriba. Movimiento 4,5 unidades/s, normalización diagonal, animaciones, collider inferior y cámara existentes se conservan.

## 11–12. Niveles y assets

Se modificaron Bootstrap y las siete escenas de zonas. Correspondencia conservada:

- Z00_Entrada / Z01_Hall / Z02_ARCA: nivel1_planta_baja.
- Z03_Piso2: nivel2_piso1_cocina_simulacion.
- Z04_Piso3 / Z05_LabSoftware: nivel3_tecnologia.
- Z06_NucleoIA: nivel4_aulas.

Se usan únicamente mapas, props, decals, items, luces y personajes propios ya integrados en Assets/_Project/Art. Los documentos usan papeles.png; los paneles, desk_pc.png; los pickups, sus PNG reales. SecurityCameraHead.asset recorta el monitor de desk_pc.png porque no había un sprite específico de cámara. Francis reutiliza un sprite humano oficial existente. No se descargó ni agregó arte externo.

MainMenu/HallwayFlicker no se reconstruyeron. MainMenu.unity quedó reserializado por Unity durante la ejecución de tests EditMode: cambios de propiedades TMP/Canvas, sin cambios de contenido ni sustitución del fondo. Los cambios previos del proyecto se conservaron.

## 13. Scripts creados

- Assets/_Project/Scripts/Narrative/ClueData.cs
- Assets/_Project/Scripts/Narrative/ClueInteractable.cs
- Assets/_Project/Scripts/Narrative/ItemPickupInteractable.cs
- Assets/_Project/Scripts/Narrative/NarrativeController.cs
- Assets/_Project/Scripts/Narrative/NarrativePanelView.cs
- Assets/_Project/Scripts/Narrative/NarrativeZoneObjectives.cs
- Assets/_Project/Scripts/Narrative/WorldStateEffect2D.cs
- Assets/_Project/Scripts/Puzzles/PuzzleData.cs
- Assets/_Project/Scripts/Puzzles/PuzzleModel.cs
- Assets/_Project/Scripts/Puzzles/PuzzleInteractable.cs
- Assets/_Project/Scripts/Hazards/SecurityCamera2D.cs
- Assets/_Project/Scripts/World/LightSequenceTrigger.cs
- Assets/_Project/Scripts/World/DoorAccessRules.cs
- Assets/_Project/Scripts/Editor/NarrativeCampaignBuilder.cs
- Assets/_Project/Scripts/Editor/NarrativeCampaignBuilder.Content.cs
- Assets/_Project/Tests/EditMode/NarrativePuzzleLogicTests.cs

Otros archivos nuevos: doce ClueData, siete PuzzleData y sus .meta; SecurityCameraHead.asset y su .meta; narrative-placement.txt y este informe. El builder de narrativa tiene una opción de menú propia y queda conectado al rebuild de los assets reales para mantener su integración.

## 14. Scripts modificados en esta tarea

- Core/GameFlagId.cs, GameState.cs, GameFlowController.cs, SceneFlowController.cs.
- Interaction/InteractableBehaviour.cs, InteractionSensor.cs, Scannable2D.cs.
- World/ZoneContext.cs, ZoneExitInteractable.cs, GroupLampFlicker2D.cs.
- Map/MinimapTrackable2D.cs.
- Player/PlayerVisionController.cs.
- UI/GameFlowView.cs.
- Hazards/DetectionSensor2D.cs.
- Encounters/FrancisBossController.cs.
- Editor/GroupArtIntegrationBuilder.cs.

Estas rutas parten de Assets/_Project/Scripts/. Los cambios de PlayerMovement2D, PlayerVisualController y del builder de menú de tareas anteriores no se sustituyeron.

## 15. Flags nuevos

| ID | Flag |
|---|---|
| 21 | FirstClueFound |
| 22 | PowerEntranceRestored |
| 23 | HardwareRoutingRestored |
| 24 | TechnologyNetworkRestored |
| 25 | SecurityNodeDisabled |

Los flags anteriores mantienen sus números. Los IDs de pistas, pickups, puzzles y eventos puntuales se guardan como conocimiento de GameState; los items utilizan el inventario ligero.

## 16. Preparado, sin implementar todavía

- AudioSource/AudioClip opcionales y UnityEvents para paneles, errores/aciertos, puertas, alertas y secuencias; no existen clips propios de audio en los assets disponibles y no se descargaron sonidos.
- Respuestas opcionales adicionales de cámaras, como cerrar temporalmente una puerta o activar otra amenaza, se pueden conectar a onAlert/onClear; la respuesta actual es alerta visual y mensaje.
- No se agregó guardado permanente Save/Load, inventario RPG, simulación de cuerda ni un sistema grande de stealth. La persistencia implementada corresponde a la partida y sus checkpoints.
- No hay sprites exclusivos de cámara ni de Francis distintos del personaje humano disponible; se reutilizan los recursos propios indicados.

## 17. Comprobaciones

Unity 6000.3.16f1: **0 errores y 0 advertencias** en Console al finalizar. Los **31 tests de lógica seleccionados en EditMode pasaron**, incluyendo conexiones, secuencias, pickups compartidos, puertas, cono de detección, dirección de visión y fases de Francis. Bootstrap y las siete zonas se guardaron correctamente.

No se entró en Play Mode ni se hizo una comprobación estética/jugable de las escenas. Esa revisión corresponde al usuario.
