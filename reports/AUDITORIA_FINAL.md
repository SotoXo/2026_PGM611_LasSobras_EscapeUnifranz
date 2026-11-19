# Auditoría técnica de Escape UNIFRANZ

## Integración

`git fetch origin --prune` actualizó `origin/main` a `fba14c5` y `origin/feature/marco` a `5427e06`. La comparación `origin/main...origin/feature/marco` dio **0 detrás y 12 delante**. `feat/terror-final` se creó desde `5427e06`. El trabajo local en `feat/diseno` incluía cambios sin confirmar en Bootstrap, las siete zonas, un prefab y varios sistemas, además de arte nuevo y ejecutables. La integración se hizo en un worktree. Se recuperaron los controles Android del historial local y se reutilizaron selectivamente el arte de carritos, el panel de puerta y la comprobación de contacto físico de la copia original.

El proyecto importó en Unity 6000.3.16f1. La suite inicial heredada completó 176 pruebas EditMode. Esa suite no detectó la desactivación serializada de elementos necesarios para la campaña.

## Problemas por impacto

| Prioridad | Hallazgo | Corrección comprobable |
| --- | --- | --- |
| Bloqueante | La preparación del mapa completo apagó todos los puzles, baterías, CCTV y grupos de luz de las tres escenas de planta baja. El acceso al Hall exigía flags cuyos productores estaban inactivos. | Restaurados dos puzles en Entrada, uno en Hall y uno en ARCA. Entrada tiene batería, linterna, panel y CCTV. Hall tiene la llave, el gabinete y la escalera al piso siguiente. ARCA vuelve a emitir la tarjeta. La auditoría cuenta elementos activos y comprueba la pista de CCTV. |
| Alta | El plano nuevo agregaba 1.058 cajas de colisión independientes y un global claro de 0,85. La luz de habitaciones nunca se ejecutaba. | Colisión compuesta y ShadowCaster2D de URP en el prefab. Se conservaron las cajas fuente y sus objetos. Unity generó 204 contornos. Se bajó el global y se restauraron ocho grupos de luz, con relleno Freeform, alarmas, recuperación de energía y límites propios. |
| Alta | El menú carecía de persistencia. La caída o salida de la aplicación podía obligar a repetir capítulos. | Guardado versionado al cambiar el estado, escritura con copia anterior, manejo de lectura inválida, carga en checkpoint y recuperación de inventario, diario, flags y fases de Francis. |
| Alta | Francis activaba Victoria directamente en el paso de apagado. | Apagado y extracción son flags distintos. Se vuelve a la puerta del laboratorio. Ese acceso dispara Victoria, epílogo y créditos. |
| Alta | El catálogo tenía eventos y fuentes sin AudioClips. El menú no tenía ambiente conectado. | 30 WAV originales, 64 entradas con referencia en Bootstrap, listener en menú y partida, tres capas ambientales, transición de música y efectos estéreo a distancia. |
| Alta | Los carritos conservaban arte genérico y recibir un trigger de detección podía confundirse con contacto del hitbox. | Cuatro carritos enlazados a las variantes del equipo. Se recuperó la comprobación de solapamiento del hitbox, con cobertura EditMode. Se mantuvieron las máquinas de estados y las rutas existentes. |
| Media | Lanzar latas aplicaba un mínimo de distancia incluso frente a una pared. Una transición podía destruir la lata durante el vuelo. | CircleCast, margen de colisión, gasto tras despejar la trayectoria y comprobación del objeto durante el vuelo. El recogible se desactiva al recogerse para impedir una doble interacción. |
| Media | La cámara centrada en el jugador podía mostrar grandes áreas fuera del mapa en los bordes. | Límites por zona derivados del rectángulo del mapa y del tamaño real del viewport ortográfico. |
| Media | Faltaban opciones de volumen y destellos. | Las opciones persisten. La reducción simplifica las secuencias de luces y desactiva la interferencia de pantalla y de linterna. Los avisos conservan texto y sonido. |
| Media | Las pistas presentaban a Francis como una persona distinta de la IA. | Se revisaron seis registros y el prólogo para contar el incidente de una sola inteligencia que imita voces. Los documentos que contienen soluciones conservan sus identificadores y requisitos. |

## Escenas y recursos

Se modificaron `MainMenu`, `Bootstrap`, `Z00_Entrada`, `Z01_Hall`, `Z02_ARCA`, `Z03_Piso2`, `Z04_Piso3` y `Z06_NucleoIA`. `Z05_LabSoftware` se auditó junto con sus brazos, puzle y accesos. Se editaron assets de pistas y el prefab compartido de planta baja. Los nueve GUID de escena se conservaron.

Los destinos y puntos de llegada se abrieron en EditMode. La cuadrícula aproxima el cuerpo del jugador con una caja de 0,38 × 0,22 y usa las cajas activas del mapa. Los resultados describen conectividad geométrica estática. Las ventanas de peligro, percepción, mezcla y comportamiento a diferentes resoluciones se comprueban con el recorrido manual.
