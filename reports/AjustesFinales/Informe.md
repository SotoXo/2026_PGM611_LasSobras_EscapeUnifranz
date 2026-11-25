# Escape UNIFRANZ · Ajustes finales de Limber

24 de noviembre de 2026 · Unity 6000.3.16f1 · Rama `feature/limber-ajustes-finales`

La revisión posterior encontró 35 errores de URP registrados durante la auditoría de escenas. Las 257 pruebas aprobadas y los resultados de build que se informan aquí correspondían a comprobaciones distintas de la consola. La corrección y la nueva verificación se documentan en el [informe de estabilización](Estabilizacion/Informe.md).

## Punto de partida

La base fue `83bfae7`, con la integración de Marco ya hecha. Se creó la rama local de respaldo `codex/respaldo-ajustes-finales-2026-11-24-214118`. La guía de trabajo se contrastó con `Tareas_Limber.html`.

`Bootstrap` contiene el jugador, la sesión, el flujo de pantallas y el flujo de zonas. `PlayerMovement2D` y `PlayerInputReader` reciben movimiento, y `PlayerInteraction` resuelve candidatos por alcance y línea de visión. `GameState` contiene inventario, vidas, objetivos y progreso. `CheckpointSave` escribe la partida en puntos de control. `NarrativeController` usa ese mismo estado para el inventario, el diario, los puzles y la linterna. Los mandos táctiles llegan a las mismas acciones mediante Input System.

Antes de los ajustes, el menú principal ya disponía de seis buses, reducción de destellos y opciones de sincronización. Le faltaban brillo, silencio, restauración y acceso desde pausa. El inventario ya conservaba cantidades y selección, pero las latas compartían icono y las acciones cubrían casillas. El tutorial de Entrada ofrecía avisos contextuales, sin exigir que se terminara la secuencia completa. Los pisos ya tenían iluminación por habitaciones, secuencias de fallos y audio por mezclador. El ascensor abría y cargaba entre zonas, pero guiaba al jugador desde fuera y comprobaba el pivote del personaje.

## Cumplimiento de la guía

| Apartado | Clasificación | Resultado |
| --- | --- | --- |
| 01 · Ajustes | Implementado, pendiente de prueba de jugabilidad | Seis buses, silencio, restauración, brillo de zona, destellos, VSync y FPS. Panel compartido entre menú y pausa. Persistencia probada en modo de edición. |
| 02 · Ambientación | Implementado, pendiente de prueba de jugabilidad | Once sprites originales en cinco escenas. Tres escaleras de planta baja tienen escombros con colisión. Se comparó el acceso a objetivos y spawns con y sin los obstáculos nuevos. |
| 03 · Inventario | Implementado, pendiente de prueba de jugabilidad | Detalles y acciones junto al objeto, selección y cantidad visibles, dos sprites de lata y controles para Windows y Android. |
| 04 · Interfaz de 8 bits | Implementado, pendiente de prueba de jugabilidad | Marcos escalonados, fuente pixelada, paneles centrados dentro de la zona segura y cuatro tamaños de captura de Ajustes. |
| 05 · Tutorial | Implementado, pendiente de prueba de jugabilidad | Siete etapas en una sala de calibración. Acciones reales, reposición de latas, reinicio y paso a Entrada con una sesión de campaña limpia. |
| 06 · Limpieza | Implementado y verificado estáticamente | Retiro de un armario inactivo y su hijo visual. Llave duplicada desactivada solo en Entrada. Objetos, spawns y accesos auditados. |
| 07 · Elevador | Implementado, pendiente de prueba de jugabilidad | Abordaje con la cápsula completa, cierre y transición, llegada con apertura y rearme después de salir. Probados ambos puntos de aparición. |
| 08 · Verificación | Implementado, pendiente de prueba de jugabilidad | Suite completa y auditoría de escenas, prefabs, referencias y GUID en modo de edición. La guía detalla la partida de aceptación. |

## Implementación

- **Ajustes.** `GameOptions` conserva ganancias y demás preferencias en `PlayerPrefs`. El silencio corta la salida y guarda los niveles anteriores. El brillo modifica las luces globales de las zonas entre valores acotados, sin aumentar la cantidad de luces ni añadir una pasada de posprocesado. El valor medio conserva cada intensidad de autor. Windows ofrece 30, 60, 120 e ilimitado con VSync apagado. Android usa 30 o 60 FPS como objetivo sujeto a la pantalla y su cadencia. [Luces 2D de URP](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@17.0/manual/2DLightProperties.html) · [Application.targetFrameRate](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Application-targetFrameRate.html)
- **Interfaz.** El tono de terminal del puzle se extendió a los paneles de objetivos, instrucciones e inventario, al HUD y a la pausa. Los marcos nuevos usan sprites escalonados. `SafeAreaPanel` ajusta los paneles centrados. Pixelify Sans se incorporó desde el proyecto original y se adjuntó su licencia SIL OFL 1.1. Se conservó el arte del título y de la pausa. [Fuente original](https://github.com/eifetx/Pixelify-Sans)
- **Inventario.** Una lata llena inmoviliza al robot y derrama un charco. Se puede recuperar del suelo como lata vacía. Una vacía acertada causa daño y se consume. Una vacía que falla se puede recuperar. Son dos sprites con nombre, descripción y cantidades propios. El lanzamiento respeta la lata seleccionada cuando el jugador la tiene. Se conserva la selección de respaldo que ya usaba Marco cuando no hay una lata seleccionada.
- **Tutorial.** `Z00_Practica` usa el mismo jugador, HUD, inventario, interacción y combate. Detecta desplazamiento, recogida real de linterna, apertura y cierre del inventario después de examinar y seleccionar, encendido y apagado, impactos con ambas latas seleccionadas y confirmación en una terminal. La bandeja repone latas y permite recuperar el blanco derrotado. La pausa ofrece reiniciar la práctica. El guardado previo queda disponible durante la práctica y la campaña comienza en la aparición de Entrada con inventario y vidas nuevos.
- **Ambientación.** Grietas, muro dañado, rocas, papeles, equipo roto, cables, suciedad, mueble dañado, advertencia, vidrio y restos de robot. Se colocaron en Entrada, Hall, ARCA, Piso 2 y Piso 3, usando capas y material existentes. Los obstáculos nuevos coinciden con el pie visual de las rocas. Se emplean las revelaciones, apagones y luces de habitación existentes. El piso 3 conserva su trazado y objetivos.
- **Audio.** Se reutilizaron la música de tensión, el zumbido, la ventilación y el sonido del edificio. El mezclador interpola sus ganancias relativas por zona y utiliza el fundido de música existente. Taller y Piso 3 tienen mezcla más tensa. El elevador usa sonidos existentes de apertura y cierre. Los WAV de campaña se conservaron.
- **Limpieza.** Se eliminaron `Armario_Personal` y su hijo `Art` en Entrada. El conjunto estaba inactivo, su inspección carecía de progresión y la comprobación de referencias encontró únicamente el vínculo padre-hijo de la jerarquía. Se desactivó por instancia `ItemVisual_llave`, una recogida aislada en Entrada. La llave del Hall y los prefabs compartidos se conservaron.
- **Elevador.** `ElevatorDoorAnimation` controla apertura, espera, cierre, viaje, llegada y espera de salida. El umbral queda abierto mientras el jugador cruza. La transición empieza al confirmar la cápsula completa dentro de la cabina. En destino primero se abre la puerta y luego vuelve el control. El jugador debe salir antes de que se rearme el viaje.

## Rendimiento y plataformas

- Ninguna escena de campaña recibió luces, partículas, animadores ni materiales nuevos por objeto. Los adornos son sprites estáticos pequeños que comparten el material existente. Los únicos sólidos nuevos de ambientación son los escombros de las escaleras.
- Los lanzamientos usan las referencias de robots y perros registradas al cargar la zona. La detección reutiliza el componente de visión del jugador. Los indicadores de puertas aplican colores solo cuando cambian, y las ganancias de ambiente se actualizan durante el fundido.
- El inventario se vuelve a dibujar por interacción. Los botones táctiles omiten cambios de sprite redundantes y detienen la animación decorativa cuando se reducen los destellos. No se cambiaron las configuraciones globales de URP ni los perfiles gráficos existentes.
- Windows y Android comparten las 11 escenas habilitadas. El configurador de Android usa esa lista y conserva ARM64, IL2CPP, OpenGLES 3 y orientación horizontal. Las dos builds locales se completaron. Windows tuvo 0 errores y 0 advertencias. Android tuvo 0 errores y 19 advertencias del paquete de renderizado y los símbolos de diagnóstico. [Registro de compilaciones](Evidencias/Compilaciones.txt)
- La auditoría estática registra luces, sombras, colliders, audio, Canvas, animadores y partículas por escena. Los FPS, el sobrepintado, las llamadas de dibujo, la memoria, el tiempo de carga y la mezcla audible requieren mediciones en Windows o en el dispositivo Android. [Inventario del editor](Evidencias/Auditoria-Unity.txt)

## Evidencias y pendientes

- **257 de 257 pruebas de Edit Mode aprobadas**, incluidas las de preferencias, inventario, dos tipos de lata, tutorial, patrullas y ascensor. La prueba que antes entraba en Play Mode se convirtió en una prueba de cápsula y secuencia en modo de edición. [Resultados XML](Evidencias/EditMode.xml)
- Las builds locales están en `.utmp/verificacion-ajustes/`. La licencia tipográfica acompaña a cada build local y está en `StreamingAssets` para las siguientes compilaciones.
- La primera revisión de 13 escenas, cuatro prefabs y 78 ScriptableObjects encontró cero componentes con scripts faltantes y 11 entradas de build con ruta y GUID válidos. Las seis referencias obsoletas de volumen de sondas que se detectaron en `Renderer2D.asset` se corrigieron durante la estabilización.
- El recorrido estático conserva observaciones preexistentes sobre indicadores, enlaces de retorno y contenido de Aula 2. Desactivar la llave aislada de Entrada retiró su aviso. La decoración no añadió observaciones al comparar el recorrido con y sin sus colliders. [Comparación de recorridos](Evidencias/Recorridos.txt)
- Las imágenes se generaron desde el editor y muestran la composición. La resolución de 1560×720 representa un formato horizontal ancho. La zona segura física se comprobará en Android. [1920×1080](Evidencias/limber-ajustes-1920x1080.png) · [1366×768](Evidencias/limber-ajustes-1366x768.png) · [2560×1080](Evidencias/limber-ajustes-2560x1080.png) · [1560×720](Evidencias/limber-ajustes-1560x720.png) · [Sala de práctica](Evidencias/limber-practica-1600x900.png)
- [Guía de pruebas manuales](Guia-pruebas.md) · [Archivos de implementación](Archivos-de-implementacion.txt) · [Historial y estado de Git](Evidencias/Git.txt)
