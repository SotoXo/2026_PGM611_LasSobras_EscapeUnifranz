# Implementación de enemigos — Limber

Fecha: 19/11/2026. Unity 6000.3.16f1. Rama: feat/limber-enemigos-final.

## Integración y diagnóstico

Se ejecutaron status, branch -vv y fetch origin --prune. Remotos comprobados: main fba14c5, feature/marco db327d7, feat/enemigos-y-arreglos 120fa0e. No apareció un commit posterior a db327d7 en feature/marco. Esta rama contiene los cinco commits anteriores de Limber. La copia estaba en feat/diseno 4e0a607 con cambios extensos sin commit.

Los cambios locales de Assets y reports se conservaron en 1d9062e; el lock local en 62ec865. Merge de feature/marco: 3845bd1. Se revisaron conflictos de escenas, geometría, sprites, UI y scripts. Se eligió la geometría reciente de Marco con sus metadatos compatibles; la variante local previa permanece recuperable por commit. Los conflictos de C# conservaron los bloques recientes de campaña y las modificaciones locales que Git pudo combinar. No se fusionó main ni se publicó la rama. Los ejecutables y archivos temporales preexistentes se dejaron fuera de los commits.

Problemas encontrados: movimiento directo que se detiene frente a paredes; reinicios con temporizadores de suspensión/distracción pendientes; latas capaces de cancelar una detección confirmada; perro sin recepción de ruido de lata; checkpoint de Francis sobrescrito al restaurar una fase; HUD de SHUTDOWN reiniciado aunque la secuencia conservaba pasos; waypoint de vigilancia dentro de Solid_017; posibles amenazas móviles persistentes después de SHUTDOWN. La victoria inmediata de la copia antigua ya estaba corregida en la integración remota y se conserva la extracción.

## Cambios funcionales

- PatrolMover2D conserva el movimiento Rigidbody2D y su cast físico. ObstacleRoute2D calcula una cuadrícula local acotada para rodear obstáculos. Reutiliza buffers y rutas, limita exploración y frecuencia de planificación. No añade otra máquina de IA.
- Las patrullas pausan en waypoints y saltan un destino persistentemente inaccesible. Persecución e investigación usan la misma navegación. MiniAuto y perro abandonan un seguimiento prolongado o bloqueado y buscan antes de retomar la patrulla.
- DetectionSensor2D conserva FOV, cobertura y raycasts sin triggers. Cachea su propietario y evalúa cada 0,1 s.
- MiniAuto conserva sprites de garra, rampa y sierra, dirección por movimiento real, reposo y desgaste. El impacto válido presenta inmediatamente el fotograma de impacto. MiniAutoSpriteSet comparte el recorte por atlas con conteo de referencias, sin crear sprites por frame.
- ThreatMotionView2D anima únicamente el arte existente del perro, Exoleg y brazos. El perro cambia orientación con el movimiento; advertencia y ataque modifican discretamente la pose. Exoleg y cada brazo reciben las ventanas reales de TimedHazardArea2D. No mueve colliders ni inventa sprites.
- Las latas conservan G, inventario, trayectoria y recogida. El consumo sigue después de validar espacio disponible. Ahora revisan colisiones también durante el vuelo y se pausan al bloquearse la jugabilidad. El impacto notifica a MiniAuto y perro dentro del radio con línea acústica libre; el enemigo investiga ese punto. Una detección visual confirmada tiene prioridad y hay un intervalo contra distracciones consecutivas.
- HazardContact2D conserva el sistema de derrota y los filtros de cuerpo físico. Reiniciar aplica 1,25 s de gracia; el impacto sigue usando EncounterContext para evitar derrotas duplicadas.
- ZoneExitInteractable limpia el bloqueo temporal tras una reaparición exitosa y conserva permisos permanentes. La extracción requiere los tres flags de fase, además de las condiciones propias del acceso.
- Francis conserva las secuencias y encuentros existentes. Restaurar HARDWARE/SHUTDOWN mantiene el SpawnId de fase. Un error en SHUTDOWN refleja el progreso realmente retenido. Completar/restaurar el apagado cancela áreas, contactos, sensores y movimiento del encuentro; GameCompleted sigue activándose en la puerta de extracción.

## Conexiones reales de escenas

| Zona | Enemigos comprobados en el editor |
|---|---|
| Z00 Entrada | MiniAuto_Inspection, garra, ruta de 3 puntos |
| Z01 Hall | Sin enemigos móviles; accesos y cámara |
| Z02 ARCA | Sin enemigos móviles; accesos |
| Z03 Piso2 | MiniAuto rampa y Exoleg, tres áreas de peligro |
| Z04 Piso3 | MiniAuto sierra, perro con dos nodos, robot de vigilancia garra |
| Z05 LabSoftware | Brazos con tres áreas y panel |
| Z06 NúcleoIA | Francis, nueve nodos, interferencia y móvil FrancisPlaceholder |

Se serializaron navegación/reacción en las escenas existentes. Vigilancia: waypoint 1 de (10,63; 6,00) a (10,701537; 6,184776), elegido con huella libre de 0,65 m. Se conectaron cinco vistas de movimiento (perro, Exoleg y tres brazos). Los cambios posteriores al merge son puntuales; se retiró el ruido de reserialización de mallas producido por Unity al guardar. No se reconstruyeron mapas ni se cambiaron GUID existentes.

Inventario, expendedoras, GameState, flujo de Game Over/victoria, audio y encuentros Exoleg/perro/brazos se reutilizan. Los campos opcionales de AudioSource/alertSound pueden estar vacíos porque se emiten los AudioCue existentes; BossPhaseView se obtiene de GameSessionController al inicializar la zona.

## Archivos principales

Scripts: Hazards/PatrolMover2D, ObstacleRoute2D, DetectionSensor2D, MiniAutoController, MiniAutoSpriteSet, HazardContact2D, ThreatFeedback2D, ThreatMotionView2D; Encounters/RobotDogEncounterController y FrancisBossController; Player/ThrowableCanController; World/ZoneExitInteractable. Escenas: Z00, Z03, Z04, Z05 y Z06. Tests: EnemyFinalIntegrationTests y EnemySceneWiringTests. Evidencia: LIMBER_SCENE_AUDIT.txt y LIMBER_ROUTE_AUDIT.txt (esta última conserva el hallazgo anterior a corregir el waypoint).

## Límites de aceptación

Esta entrega incluye implementación y comprobaciones de editor. El recorrido humano completo, sincronización visual percibida, equilibrio de encuentros, seguridad de checkpoints en juego y rendimiento a 1080p siguen sujetos a Play Mode/build manual. No se certifica transitabilidad completa ni 60 FPS a partir de pruebas unitarias. Consultar LIMBER_ENEMIGOS_PRUEBAS.md para la lista pendiente.
