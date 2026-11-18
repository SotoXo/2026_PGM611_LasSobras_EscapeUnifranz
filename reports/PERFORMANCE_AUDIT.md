# Escape UNIFRANZ · auditoría de rendimiento

## Estado de partida

La intervención parte de `d255a6a` en `feat/terror-final`. `git fetch origin --prune` confirmó que `origin/feature/marco` sigue en `5427e06` y sus commits están en la rama de campaña. El trabajo se hizo en la copia aislada `feat/terror-rendimiento`. El worktree original conserva sus cambios locales.

**Objetivo Windows** · 1920 × 1080, 60 FPS estables y cerca de 16,7 ms por frame en el Ryzen 7 9700X con NVIDIA RTX 5060. Se hizo inspección de componentes y dos compilaciones Development comparables. Los datos de esta sección son inventario de escena, formato de textura y comportamiento verificado en código. La caída reportada todavía requiere una captura del jugador para distinguir CPU, GPU, espera de presentación y carga de recursos.

## Hallazgos y correcciones

| Sistema | Evidencia de partida | Corrección |
| --- | --- | --- |
| Texturas del menú | Cuatro imágenes de 1672 × 941 sin mipmaps ni copia legible, importadas en RGB24. En total, 18.880.224 bytes de datos de textura de nivel base. | Cuatro recortes centrados de 1664 × 936, 16:9, referenciados por las dos capas. BC7 verificado en Direct3D 11 sobre RTX 5060. Total de nivel base 6.230.016 bytes, un **67 %** menos. Se conservaron los originales y la resolución visual de la escena. |
| Canvas del menú | Un Canvas contenía tanto la interfaz interactiva como el fondo móvil y los cambios de color de la capa de vigilancia. El crossfade escribía el color de la imagen en cada frame de transición. | Canvas hijo para el pasillo. Sigue habiendo un solo GraphicRaycaster. El crossfade escribe la opacidad en CanvasRenderer. El paralaje se limita aproximadamente a 30 Hz y deja de escribir cuando el puntero se estabiliza. La escena pasó de 25 a 15 componentes Graphic activos en la vista principal. |
| Cámara y URP | El menú se dibujaba con HDR permitido a pesar de ser UI sobre un fondo. La configuración URP usa escala de render 1 y textura de luz 2D al 50 %, con SRP Batcher activado. Las zonas usan la única cámara de Bootstrap. | HDR y MSAA se deshabilitan en la cámara del menú. Se conserva la escala y el perfil de la cámara de la partida. Sin cambios globales de URP. |
| Iluminación | Las siete zonas sumaban 164 luces activas con `shadowsEnabled`. El indicador describe la configuración del componente, incluidos casos que URP puede descartar. Varias luminarias escribían intensidad y sprite constantes en cada Update. | 37 luces conservan la marca en las siete escenas, con sombra principal en los rellenos por sala. Las zonas de luz se evalúan con margen de cámara cada 0,16 s, conservando la secuencia temporal fuera de vista. Las luces de estado fijo salen del cálculo de brillo y las lámparas inestables aplican el brillo a unos 15 Hz. La linterna direccional conserva sus sombras. |
| Físicas y búsqueda | El prefab compartido de planta baja tiene 1.058 cajas de origen, combinadas en 204 contornos físicos. En las otras zonas se observaron 55–68 Collider2D activos, incluidos sensores. El movimiento y los eventos usan un solo jugador persistente. | Se conservaron las colisiones y los puntos de llegada. La linterna omite el raycast del cono apagado y reduce las consultas cuando el jugador está quieto. Los candidatos de interacción se actualizan a unos 15 Hz y se revalidan inmediatamente al pulsar E. El texto solo cambia cuando cambia el aviso. |
| Partículas y persistencia | No se encontraron sistemas de partículas activos en las nueve escenas. Bootstrap aporta la cámara, la UI y el listener de la partida. MainMenu se carga aparte. | La auditoría sigue comprobando los enlaces de escena, un jugador y un Canvas de partida, y un AudioListener por escena de entrada. |
| Flujo y memoria | SceneFlow usa cargas asíncronas de escena. Las actualizaciones anteriores del prompt y de las lámparas tenían costes evitables. El guardado y los puntos de control hacen E/S al cambiar estado. | Se conservaron la transición y la persistencia. Los picos de `LoadSceneAsync`, guardado, física y `GC.Alloc` se verifican durante el recorrido de A/B. La inspección de scripts no sustituye esos tiempos. |
| Audio | Los 64 eventos ya tenían referencias locales, con música original, ambiente y distintas ganancias por clip. Faltaba un mezclador de grupos y había casos de golpes repetidos en una ventana breve. | 38 WAV originales y 71 eventos de campaña. Ocho pistas largas se importan como Vorbis en streaming. El AudioMixer tiene Master, Music, Ambience, SFX, UI y Voices, cada uno con volumen persistente. Hay límite corto por evento, un búfer de ocho fuentes posicionales y crossfade sin repetir un arranque si la pista ya se está reproduciendo. |

### Sombras configuradas en las escenas

| Zona | Antes | Después |
| --- | ---: | ---: |
| Entrada | 25 | 7 |
| Hall | 24 | 7 |
| ARCA | 15 | 6 |
| Piso 2 | 26 | 4 |
| Piso 3 | 26 | 4 |
| Laboratorio | 23 | 4 |
| Núcleo | 25 | 5 |
| **Total** | **164** | **37** |

Los inventarios se reproducen con `CampaignPerformanceAudit.Baseline` en la revisión anterior y `CampaignPerformanceAudit.FinalSnapshot` en la actual. Los registros se generan en `Logs/performance-before.txt` y `Logs/performance-after.txt`. El tamaño de textura de nivel base es distinto de la memoria residente total del proceso. Los componentes con sombras configuradas son distintos de las pasadas de sombra ejecutadas por frame.

## Menú, audio y documentos

- Los seis comandos ocupan una columna a la izquierda bajo el título original. Jugar, Controles y Salida mantienen las piezas con rótulo incorporado del equipo. Continuar, Opciones y Créditos combinan la placa pixel art existente con el atlas PirataOne SDF. Se verificaron las vocales acentuadas, Ñ, ü y signos en el atlas original. Parecer inactivo y estar deshabilitado son estados del botón Continuar hasta que se valida el guardado.
- Las vistas del menú se capturaron en EditMode a 1920 × 1080 y 1440 × 1080 usando Direct3D 11. Controles y Opciones conservan un retorno visible. Opciones ofrece seis canales de volumen, destellos, VSync y 60, 120 o sin límite con VSync desactivado. Con VSync activado manda la frecuencia de pantalla en Windows.
- Las ocho pistas de menú, exploración, descubrimiento, tensión, persecución, núcleo, Victoria y Game Over y los efectos son originales del proyecto. La nueva composición de menú es un bucle de 48 s con drones, resonancias metálicas y pulsación lenta. El generador reproducible está en `Tools/generate-campaign-audio.cjs`. Las voces de FRANCIS siguen siendo transmisiones sintetizadas con subtítulos, con salida propia en Voices.
- Se revisaron 17 documentos. Los registros de seguridad usan fondo electrónico y las hojas un papel generado con textura discreta. Ambos usan una aparición breve, sonido propio de apertura y lectura desde el diario. Se conservaron los IDs y datos de solución, incluidos 3174, el orden de ARCA, el fusible, la topología y las tres fases. El laboratorio explica que el operador retiró el cable y la cuenta de servicio F.R.A.N.C.I.S. firmó la reclasificación, que fue interpretada como un nombre humano.

## Archivos principales

- Escenas `MainMenu.unity` y `Bootstrap.unity`. Cambios visuales, referencias de TMP, opciones, documento, clips, fuentes y buses.
- Siete escenas `Assets/_Project/Scenes/Zones`. Solo se cambiaron los flags `m_ShadowsEnabled` de los Light2D afectados.
- `MainMenuAmbienceController`, `HallwayFlicker`, `RoomLightingController`, `GroupLampFlicker2D`, `PlayerVisionController`, `PlayerInteraction` e `InteractableFeedback2D`. Ajustes de actualización y render de cada sistema.
- `GameOptions`, `GameOptionsView`, `MenuButtonFeedback`, `ProjectAudioHooks`, `GameFlowController` y la presentación del diario. Gestión de menús, gráficos locales, ganancias, señales y transiciones.
- `Assets/_Project/Audio/EscapeUNIFRANZ.mixer`, los WAV de `Audio/Campaign`, los cuatro sprites de `HallwayCompressed`, `PirataOne Menu.mat` y `DocumentPaper.png`.
- `CampaignPerformanceAudit`, `CampaignWindowsBuild`, `CampaignPolishSetup`, `CampaignReleaseAudit` y `PerformanceBudgetTests`. Reproducción de importadores, capturas, validación y compilaciones.

## Protocolo A/B en el ejecutable

1. Deja Windows a 1920 × 1080, 60 Hz y pantalla completa. Usa los mismos ajustes de energía, driver y GPU para ambos ejecutables y el mismo estado inicial de partida. Usa el mismo backend iniciando ambos con `-force-d3d11 -screen-width 1920 -screen-height 1080 -screen-fullscreen 1`. Conserva VSync activado en el juego nuevo para la primera comparación.
2. Abre Unity 6000.3.16f1 y **Window → Analysis → Profiler**. Ejecuta manualmente `Builds/Diagnostics/Before/EscapeUNIFRANZ.exe` y, en otra pasada, `Builds/Diagnostics/After/EscapeUNIFRANZ.exe`. Selecciona el jugador Windows en el desplegable del Profiler si la conexión automática no lo selecciona. Las dos builds son Development con Autoconnect y sin Deep Profiling inicial. [Guía de Unity para capturar en la plataforma destino](https://docs.unity.com/en-us/engine/6000.0/manual/analysis/profiler/profiling-applications/profiling-target-device)
3. Registra tres rondas de 30 s de menú quieto y de menú con el puntero en movimiento, incluyendo parpadeos. Repite el mismo trayecto en Entrada antes y después de restaurar la luz, y el cruce Entrada–Hall. Añade Simulación con una persecución y Núcleo con un pulso de interferencia. Anota tiempo de calentamiento y descarta los primeros diez segundos de cada captura estable. Conserva por separado el primer arranque y la carga en frío.
4. Guarda los archivos `.data`. Usa CPU Usage en Timeline para el Main Thread y Render Thread, GPU Usage para los tiempos del backend cuando el módulo esté disponible, Rendering y Frame Debugger para batches, draw calls, SetPass y pasadas de luz 2D, UI para reconstrucciones, Physics 2D para las consultas, Memory para texturas y Audio para DSP y voces. Captura en el Frame Debugger una vez por escena tras la prueba de tiempo, porque su pausa altera la ejecución.
5. Registra mediana, P95 y P99 de frame, uso de GPU y picos de transiciones antes y después. Si el frame queda cerca de 16,7 ms con espera dominante de presentación, repite en la build nueva con VSync desactivado y registra por separado 60 FPS, 120 FPS y sin límite. El tiempo en espera de presentación y el tiempo real de CPU/GPU se interpretan por separado.
6. Ejecuta la build de distribución de Windows para una última pasada de guardado, Continuar, menú, música, volumen, legibilidad, alarmas, linterna y transiciones. Entra por MainMenu y sigue [el recorrido de la campaña](PROGRESION_COMPLETA.md). Mantén los procesos de diagnóstico y distribución separados.

## Resultado comprobado y pendiente

- **186/186 pruebas EditMode superadas**. Las pruebas adicionales cubren márgenes de visibilidad de iluminación y la conversión de ganancia de los buses a decibelios.
- Auditoría de nueve escenas completada. Se conservaron doce transiciones y las fuentes de pistas y soluciones de los once puzles. Se comprobó la presencia de los seis botones, sus sprites, PirataOne en los rótulos nuevos y las conexiones del panel de opciones. Las capturas del menú y Opciones se guardaron en `Logs/`.
- Se compilaron los dos diagnósticos y la distribución Windows x64. Ambos diagnósticos tienen `profiler-enable=1` en `boot.config`. La distribución se generó con `BuildOptions.CompressWithLz4` y su `boot.config` carece de activación del Profiler. Cada ZIP incluye el ejecutable, archivos de datos y DLL.
- Quedan por capturar FPS, CPU/GPU, draw calls de juego, Canvas rebuilds, latencia de transición y mezcla audible, y comprobar continuidad en una partida real. Las capturas visuales se hicieron en EditMode.
