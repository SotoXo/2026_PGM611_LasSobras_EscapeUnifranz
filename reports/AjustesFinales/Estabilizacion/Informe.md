# Estabilización de los ajustes finales

24 de noviembre de 2026 · Unity 6000.3.16f1 · `feature/limber-ajustes-finales` · Base `c915aa5`

## Diagnóstico

La consola nativa de Unity registraba **35 errores, 10 advertencias y 2 mensajes informativos**. El lector estándar del MCP omitía los 35 errores de URP y clasificaba las advertencias de build como errores. La captura se realizó desde el MCP usando el contador y las entradas nativas de la consola, antes de limpiarla. [Captura original con mensajes y trazas](Consola-inicial.json)

Los 35 errores pertenecían a una sola causa. La auditoría de audio de la entrega anterior abrió cinco escenas mediante `EditorSceneManager.OpenPreviewScene` mientras ya estaba abierta `Z00_Entrada`. URP registra las luces globales por capa y estilo de mezcla dentro de la etapa de edición. Cada escena abierta en esa etapa intentó registrar siete capas que ya tenían una luz global. Las entradas de las 22:33:07 y el orden de apertura del registro del Editor corresponden a los cinco grupos de la tabla. La práctica añadió un grupo durante esa auditoría y su luz resultó válida al cargarla sola.

Las 35 entradas son errores del registro de luces 2D en modo de edición. Entre ellas hubo **0 errores de compilación de C#, 0 errores de importación y 0 excepciones de ejecución del juego**. La auditoría posterior detectó además seis referencias de depuración de sondas obsoletas en `Renderer2D.asset` y una dependencia huérfana de la plantilla Lit 2D. Ambas configuraciones ya estaban en la base `83bfae7`. Se retiraron los enlaces obsoletos del renderer y se conservaron los recursos de depuración vigentes de la configuración global de URP. La plantilla conservó su escena y su miniatura.

## Corrección

`SceneInspection` carga una escena iluminada por vez, restaura la configuración del editor al terminar y conserva la escena temporal del Test Runner cuando carece de luces globales 2D activas. Las auditorías de campaña y las pruebas de escenas usan este flujo. La prueba del ascensor toma las dimensiones del jugador de Bootstrap y luego inspecciona la zona de destino. Las escenas, las luces de autor y los scripts de jugabilidad permanecieron intactos.

## Registro de los 35 errores

En cada fila, `Light2DManager.cs:49` corresponde a `Library/PackageCache/com.unity.render-pipelines.universal@97f77d4dda9a/Runtime/2D/Light2DManager.cs`. Las 35 entradas comparten la traza completa de `OpenPreviewScene → MCPDynamicCode.Execute → MCPForUnity`. Cada entrada conserva el índice de esa traza en el archivo de evidencia. [Traza completa y detalle por entrada](Consola-inicial.json)

| N.º | Escena abierta durante la auditoría | Mensaje completo | Archivo y línea | Causa | Corrección | Estado |
| ---: | --- | --- | --- | --- | --- | --- |
| 1 | `Z00_Entrada` | `More than one global light on layer Default for light blend style index 0` | `Light2DManager.cs:49` | Dos luces globales registradas para la misma capa y estilo | Abrir la escena de forma aislada | Resuelto |
| 2 | `Z00_Entrada` | `More than one global light on layer Ground for light blend style index 0` | `Light2DManager.cs:49` | Dos luces globales registradas para la misma capa y estilo | Abrir la escena de forma aislada | Resuelto |
| 3 | `Z00_Entrada` | `More than one global light on layer Decals for light blend style index 0` | `Light2DManager.cs:49` | Dos luces globales registradas para la misma capa y estilo | Abrir la escena de forma aislada | Resuelto |
| 4 | `Z00_Entrada` | `More than one global light on layer World for light blend style index 0` | `Light2DManager.cs:49` | Dos luces globales registradas para la misma capa y estilo | Abrir la escena de forma aislada | Resuelto |
| 5 | `Z00_Entrada` | `More than one global light on layer Characters for light blend style index 0` | `Light2DManager.cs:49` | Dos luces globales registradas para la misma capa y estilo | Abrir la escena de forma aislada | Resuelto |
| 6 | `Z00_Entrada` | `More than one global light on layer Foreground for light blend style index 0` | `Light2DManager.cs:49` | Dos luces globales registradas para la misma capa y estilo | Abrir la escena de forma aislada | Resuelto |
| 7 | `Z00_Entrada` | `More than one global light on layer Effects for light blend style index 0` | `Light2DManager.cs:49` | Dos luces globales registradas para la misma capa y estilo | Abrir la escena de forma aislada | Resuelto |
| 8 | `Z01_Hall` | `More than one global light on layer Default for light blend style index 0` | `Light2DManager.cs:49` | Dos luces globales registradas para la misma capa y estilo | Abrir la escena de forma aislada | Resuelto |
| 9 | `Z01_Hall` | `More than one global light on layer Ground for light blend style index 0` | `Light2DManager.cs:49` | Dos luces globales registradas para la misma capa y estilo | Abrir la escena de forma aislada | Resuelto |
| 10 | `Z01_Hall` | `More than one global light on layer Decals for light blend style index 0` | `Light2DManager.cs:49` | Dos luces globales registradas para la misma capa y estilo | Abrir la escena de forma aislada | Resuelto |
| 11 | `Z01_Hall` | `More than one global light on layer World for light blend style index 0` | `Light2DManager.cs:49` | Dos luces globales registradas para la misma capa y estilo | Abrir la escena de forma aislada | Resuelto |
| 12 | `Z01_Hall` | `More than one global light on layer Characters for light blend style index 0` | `Light2DManager.cs:49` | Dos luces globales registradas para la misma capa y estilo | Abrir la escena de forma aislada | Resuelto |
| 13 | `Z01_Hall` | `More than one global light on layer Foreground for light blend style index 0` | `Light2DManager.cs:49` | Dos luces globales registradas para la misma capa y estilo | Abrir la escena de forma aislada | Resuelto |
| 14 | `Z01_Hall` | `More than one global light on layer Effects for light blend style index 0` | `Light2DManager.cs:49` | Dos luces globales registradas para la misma capa y estilo | Abrir la escena de forma aislada | Resuelto |
| 15 | `Z02_ARCA` | `More than one global light on layer Default for light blend style index 0` | `Light2DManager.cs:49` | Dos luces globales registradas para la misma capa y estilo | Abrir la escena de forma aislada | Resuelto |
| 16 | `Z02_ARCA` | `More than one global light on layer Ground for light blend style index 0` | `Light2DManager.cs:49` | Dos luces globales registradas para la misma capa y estilo | Abrir la escena de forma aislada | Resuelto |
| 17 | `Z02_ARCA` | `More than one global light on layer Decals for light blend style index 0` | `Light2DManager.cs:49` | Dos luces globales registradas para la misma capa y estilo | Abrir la escena de forma aislada | Resuelto |
| 18 | `Z02_ARCA` | `More than one global light on layer World for light blend style index 0` | `Light2DManager.cs:49` | Dos luces globales registradas para la misma capa y estilo | Abrir la escena de forma aislada | Resuelto |
| 19 | `Z02_ARCA` | `More than one global light on layer Characters for light blend style index 0` | `Light2DManager.cs:49` | Dos luces globales registradas para la misma capa y estilo | Abrir la escena de forma aislada | Resuelto |
| 20 | `Z02_ARCA` | `More than one global light on layer Foreground for light blend style index 0` | `Light2DManager.cs:49` | Dos luces globales registradas para la misma capa y estilo | Abrir la escena de forma aislada | Resuelto |
| 21 | `Z02_ARCA` | `More than one global light on layer Effects for light blend style index 0` | `Light2DManager.cs:49` | Dos luces globales registradas para la misma capa y estilo | Abrir la escena de forma aislada | Resuelto |
| 22 | `Z07_Aula2` | `More than one global light on layer Default for light blend style index 0` | `Light2DManager.cs:49` | Dos luces globales registradas para la misma capa y estilo | Abrir la escena de forma aislada | Resuelto |
| 23 | `Z07_Aula2` | `More than one global light on layer Ground for light blend style index 0` | `Light2DManager.cs:49` | Dos luces globales registradas para la misma capa y estilo | Abrir la escena de forma aislada | Resuelto |
| 24 | `Z07_Aula2` | `More than one global light on layer Decals for light blend style index 0` | `Light2DManager.cs:49` | Dos luces globales registradas para la misma capa y estilo | Abrir la escena de forma aislada | Resuelto |
| 25 | `Z07_Aula2` | `More than one global light on layer World for light blend style index 0` | `Light2DManager.cs:49` | Dos luces globales registradas para la misma capa y estilo | Abrir la escena de forma aislada | Resuelto |
| 26 | `Z07_Aula2` | `More than one global light on layer Characters for light blend style index 0` | `Light2DManager.cs:49` | Dos luces globales registradas para la misma capa y estilo | Abrir la escena de forma aislada | Resuelto |
| 27 | `Z07_Aula2` | `More than one global light on layer Foreground for light blend style index 0` | `Light2DManager.cs:49` | Dos luces globales registradas para la misma capa y estilo | Abrir la escena de forma aislada | Resuelto |
| 28 | `Z07_Aula2` | `More than one global light on layer Effects for light blend style index 0` | `Light2DManager.cs:49` | Dos luces globales registradas para la misma capa y estilo | Abrir la escena de forma aislada | Resuelto |
| 29 | `Z00_Practica` | `More than one global light on layer Default for light blend style index 0` | `Light2DManager.cs:49` | Dos luces globales registradas para la misma capa y estilo | Abrir la escena de forma aislada | Resuelto |
| 30 | `Z00_Practica` | `More than one global light on layer Ground for light blend style index 0` | `Light2DManager.cs:49` | Dos luces globales registradas para la misma capa y estilo | Abrir la escena de forma aislada | Resuelto |
| 31 | `Z00_Practica` | `More than one global light on layer Decals for light blend style index 0` | `Light2DManager.cs:49` | Dos luces globales registradas para la misma capa y estilo | Abrir la escena de forma aislada | Resuelto |
| 32 | `Z00_Practica` | `More than one global light on layer World for light blend style index 0` | `Light2DManager.cs:49` | Dos luces globales registradas para la misma capa y estilo | Abrir la escena de forma aislada | Resuelto |
| 33 | `Z00_Practica` | `More than one global light on layer Characters for light blend style index 0` | `Light2DManager.cs:49` | Dos luces globales registradas para la misma capa y estilo | Abrir la escena de forma aislada | Resuelto |
| 34 | `Z00_Practica` | `More than one global light on layer Foreground for light blend style index 0` | `Light2DManager.cs:49` | Dos luces globales registradas para la misma capa y estilo | Abrir la escena de forma aislada | Resuelto |
| 35 | `Z00_Practica` | `More than one global light on layer Effects for light blend style index 0` | `Light2DManager.cs:49` | Dos luces globales registradas para la misma capa y estilo | Abrir la escena de forma aislada | Resuelto |

## Verificación

- **260 de 260 pruebas de Edit Mode aprobadas**. El XML conserva las 257 pruebas originales y añade tres que revisan luces, scripts, referencias, prefabs y escenas habilitadas. [Resultados XML](EditMode.xml)
- Las 13 escenas se abrieron desde el editor mediante el nuevo flujo y generaron **0 errores nuevos**. Se comprobaron las 11 escenas habilitadas para build, los cuatro prefabs del proyecto y 122 recursos de tipo ScriptableObject en Assets. Se obtuvieron **0 Missing Scripts y 0 referencias rotas** en ese alcance.
- La comprobación adicional confirmó las 71 referencias de audio de Bootstrap, el mezclador y los controles de ajustes en pausa y menú, las dos estaciones de práctica, las cabinas de Entrada y Aula 2, los seis recursos activos de depuración de sondas, el atlas y material de Pixelify Sans y la importación de los 11 sprites nuevos.
- Se limpió la consola tras guardar la evidencia, se forzó la compilación y, después de ejecutar las pruebas, se refrescaron de nuevo todos los recursos. La lectura final por MCP confirmó **0 errores activos y 0 advertencias en la consola**. Unity quedó en `Z00_Entrada`, con Android como plataforma activa y fuera de Play Mode.
- La build local de Windows x64 terminó con **0 errores y 0 advertencias**. La build de Android ARM64 terminó con **0 errores y 1 advertencia** sobre símbolos de diagnóstico. Ambas se generaron en `.utmp/verificacion-estabilizacion/`. Unity guardó resultados de Edit Mode de **260 de 260** con cada plataforma activa. [Datos de la verificación final](Verificacion-Unity.json)
- Se comprobaron por SHA-256 los 209 archivos locales originales. Los 209 conservan su contenido. Se trabajó únicamente en la rama indicada.
