# Audio faltante

Se inspeccionaron `Assets` y `Packages`: no hay archivos WAV, OGG, MP3, AIFF o FLAC. Por ello todos los clips siguientes faltan. No se creó ni descargó sonido.

`ProjectAudioHooks`, en el GameRuntimeRoot de Bootstrap, contiene 48 entradas con nombre, clip y volumen y AudioSources de efectos, ambiente y música. Los sonidos principales tienen llamadas en sus acciones; los matices y pistas opcionales quedan como entradas invocables. También se conservan los hooks locales anteriores. Sin un clip asignado, permanecen silenciosos.

| Sonido faltante | Dónde | Prioridad |
|---|---|---|
| Zumbido eléctrico | Ambiente del edificio | MEDIA |
| Ventilación/HVAC | Salas/pasillos; catálogo preparado | MEDIA |
| Ambiente lejano del edificio | Exploración; catálogo preparado | MEDIA |
| Interferencia electrónica | Sistemas comprometidos; catálogo preparado | MEDIA |
| Crujidos metálicos | Escenario; catálogo preparado | MEDIA |
| Pasos suelo normal | PlayerMovement2D | ALTA |
| Pasos baldosa | PlayerMovement2D, variante de superficie configurable | ALTA |
| Interacción | InteractableBehaviour | MEDIA |
| Recoger objeto | ItemPickupInteractable | MEDIA |
| Respiración/tensión opcional | Jugador; catálogo preparado | BAJA |
| Fluorescente | Luminarias; catálogo preparado | MEDIA |
| Flicker | GroupLampFlicker2D | ALTA |
| Apagón | LightSequenceTrigger | ALTA |
| Electricidad | Restauración de energía | MEDIA |
| Bloqueo de puerta | Accesos | ALTA |
| Desbloqueo de puerta | Accesos recuperados | MEDIA |
| Apertura de puerta | Transición de zona | ALTA |
| Cierre de puerta | Cierre de estación/puertas; catálogo preparado | MEDIA |
| Puerta electrónica | Variante de acceso; catálogo preparado | MEDIA |
| Servo de robot | Preparación mecánica; catálogo preparado | MEDIA |
| Ruedas de robot | Desplazamiento; catálogo preparado | MEDIA |
| Patrulla de robot | Móviles; catálogo preparado | MEDIA |
| Detección de robot | Sensores; catálogo preparado | ALTA |
| Alerta de robot | Aviso previo a persecución | ALTA |
| Persecución de robot | MiniAutoController | ALTA |
| Apagado de robot | Corte de energía | ALTA |
| Reinicio de robot | Vuelta desde desactivación | MEDIA |
| Motor de cámara | Rotación CCTV; catálogo preparado | MEDIA |
| Cambio de feed | SecurityTerminalInteractable | MEDIA |
| Detección CCTV | Cámara; catálogo preparado | ALTA |
| Señal perdida | Feed NO SIGNAL | MEDIA |
| Conectar cable | Cables y reemplazo de fusible | ALTA |
| Cable incorrecto | Error de conexión | ALTA |
| Puzzle correcto | Confirmación final | ALTA |
| Botón | Controles UI; catálogo preparado | MEDIA |
| Switch | Secuencias de interruptores | MEDIA |
| Terminal boot | Arranque/recuperación; catálogo preparado | MEDIA |
| Terminal error | Operación incorrecta | MEDIA |
| Golpe distante | Eventos puntuales; catálogo preparado | MEDIA |
| Ruido metálico | Amenazas/escenario; catálogo preparado | MEDIA |
| Interferencia de terror | ScreenGlitchController | ALTA |
| Alarma roja | WorldHorrorDirector | ALTA |
| Stinger puntual | Apariciones; catálogo preparado | MEDIA |
| Música de menú | Catálogo/música preparados; falta pista | MEDIA |
| Música de exploración | Entrada a zonas | MEDIA |
| Música de tensión | Alarmas | ALTA |
| Música de persecución | MiniAutoController | ALTA |
| Música de núcleo/final | Entrada al núcleo | ALTA |

El catálogo permite asignar clips propios después. No se ha mezclado ni revisado audio en Play Mode. La selección detallada de superficies, variaciones de ruedas/servos y ambientación incidental queda para cuando existan los clips.
