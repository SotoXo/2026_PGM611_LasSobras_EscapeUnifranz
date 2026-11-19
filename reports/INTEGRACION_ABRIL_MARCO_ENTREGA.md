# Integración de Abril y Marco — entregables

Fecha: 19/11/2026. Unity: 6000.3.16f1.
Rama: codex/integracion-abril-marco-builds.

## Código integrado

- feature/marco: 0d3ad0f — Integra animaciones, efectos y mejoras de interacción.
- abril/animaciones-objetos: 7854a58 — Lata lanzada con sombra, rebote y polvo; recogida con efecto.
- Se comprobó con git merge-base --is-ancestor que Abril ya está completamente incluida en Marco.
- Merge de ambas ramas y los enemigos anteriores: 2a481d2.
- Correcciones de integración: 90abed5.

Incluye ascensores animados, cabina física, cambios de iluminación y mapa, Aula 2, expendedoras actualizadas, orientación/poses del jugador, contorno de objetos y efectos de recoger/usar/lanzar. Se conservaron las colisiones dinámicas de la lata, la distracción del perro y los requisitos de extracción de Francis.

Se resolvieron manualmente dos conflictos: ThrowableCanController y ZoneExitInteractable. No se fusionó main ni se publicó ninguna rama.

## Correcciones durante validación

InteractionFx ya no intenta usar DontDestroyOnLoad ni iniciar efectos de juego durante operaciones de editor. Las tres rutas de robots de muestra de Aula 2 se desplazaron ligeramente a posiciones libres de colisión, incluyendo comprobaciones intermedias de los tramos. Se conservó la estructura, los sprites y la condición sin daño de esta muestra del equipo.

EnemySceneWiringTests incluye Aula 2, comprueba colisiones de la escena correspondiente y respeta escenas ya abiertas. No se entró automáticamente en Play Mode.

## Pruebas

Suite final EditMode: 213/213 aprobadas, cero fallos, cero omitidas. Archivo íntegro: INTEGRACION_ABRIL_MARCO_EDITMODE.xml. Incluye compra/recogida de lata en la expendedora de Aula 2, referencias de los tres robots, colisiones de cabina/ascensor, progresión y pruebas anteriores de enemigos/Francis.

El puente MCP perdió algunos estados de trabajos al recargar el dominio; se utilizó el XML final de Unity como evidencia. Las primeras ejecuciones detectaron el efecto persistente en editor y las rutas obstruidas; ambas causas se corrigieron antes de generar las builds definitivas.

## Android

Build IL2CPP ARM64, APK de instalación directa, com.unifranz.escape 1.0.1 (código 2). Android mínimo API 25 (7.1); destino API 36. Diez escenas incluidas, con MainMenu primero. Firma Android Debug verificada con apksigner, esquema v2 válido. No es una entrega firmada para publicar en tienda.

Ruta: Builds/Integracion_Abril_Marco/Android/EscapeUNIFRANZ.apk.
Informe del build: INTEGRACION_ABRIL_MARCO_ANDROID.json.

## Uso de las salidas

Android: copiar el APK al dispositivo y abrirlo para instalar. Windows: extraer el ZIP completo y ejecutar EscapeUNIFRANZ.exe dentro de la carpeta extraída, conservando EscapeUNIFRANZ_Data, UnityPlayer.dll y los demás archivos junto al ejecutable.

## Alcance de validación

Las pruebas de editor y las compilaciones no certifican un recorrido humano completo. Quedan pendientes pruebas en un teléfono real (instalación, gestos, rendimiento, sonido), revisión visual interactiva de las animaciones y una partida completa hasta la victoria. No se afirma rendimiento de 60 FPS sin medición.

## Windows y comprobación de arranque

Build StandaloneWindows64 completada, cero errores y cero advertencias de build. Informe: INTEGRACION_ABRIL_MARCO_WINDOWS.json.

El ejecutable se abrió fuera del editor con Direct3D 11 en NVIDIA RTX 5060. Permaneció activo y respondiendo durante 53,54 segundos, sin líneas de error o excepción en el log. Se cerró únicamente ese proceso de comprobación. Evidencia: INTEGRACION_ABRIL_MARCO_PC_SMOKE.json. Esta comprobación cubre el arranque, no una partida completa.

Paquete: Builds/Integracion_Abril_Marco/EscapeUNIFRANZ_Windows_x64.zip. Se comprobaron sus 181 entradas, incluyendo ejecutable, UnityPlayer.dll y carpeta de datos. Se excluyó BurstDebugInformation_DoNotShip.

Tamaños finales: APK 45.881.561 bytes; ZIP Windows 49.361.782 bytes. SHA-256 y rutas exactas: INTEGRACION_ABRIL_MARCO_ARCHIVOS.json y ARCHIVOS_SHA256.json junto a las builds. El código jugable corresponde a 90abed5; el commit posterior únicamente agrega documentación y evidencia.
