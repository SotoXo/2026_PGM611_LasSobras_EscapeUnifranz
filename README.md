# Escape UNIFRANZ

Survival horror 2D en pixel art. Un corte de energía deja a un estudiante dentro de la universidad. FRANCIS toma los accesos y usa las voces del campus para atraerlo. La campaña recorre siete zonas desde recepción hasta el apagado del núcleo y la huida por la puerta del laboratorio.

## Ejecutar

Compilación de la campaña **1.0.1**.

- **Unity 6000.3.16f1.** Añade esta carpeta desde Unity Hub y abre `Assets/_Project/Scenes/UI/MainMenu.unity`. Pulsa Play manualmente para jugar en el editor.
- **Windows x64.** Compila con **Escape UNIFRANZ → Campaña → Compilar Windows x64**. Ejecuta `Builds/Windows/EscapeUNIFRANZ.exe` junto con su carpeta de datos y las DLL.
- **Android.** Usa **EscapeUNIFRANZ → Build → Build Android APK** con los módulos Android Build Support, SDK, NDK y OpenJDK de Unity. El perfil del equipo usa ARM64, IL2CPP, OpenGLES3, Android 7.1 o superior y orientación horizontal. Incluye joystick y botones táctiles. Consulta el informe de validación antes de distribuir.

El menú ofrece Nueva partida, Continuar cuando existe un checkpoint válido, Controles, Opciones, Créditos y Salir. Opciones permite ajustar seis canales de sonido, reducir los destellos y elegir VSync o un límite de 60, 120 FPS o sin límite en Windows. VSync usa la frecuencia de la pantalla cuando está activo. El guardado local conserva objetos, pistas, controles restaurados y fases del núcleo. Continuar parte del último checkpoint.

## Controles

| Acción | Windows | Android |
| --- | --- | --- |
| Moverse | WASD o flechas | Joystick |
| Interactuar | E, mantener cuando se indique | Botón de interacción |
| Inventario | I | Inventario |
| Linterna | F | Linterna |
| Diario | J | Diario |
| Escáner | Tab | Escáner |
| Mapa, tras conseguir el plano | M | Mapa |
| Lanzar lata | G | Lanzar, al llevar una lata |
| Pausa y cerrar paneles | Esc | Pausa o botón del panel |

Las expendedoras de Simulación y Tecnología entregan latas con dos interacciones. El golpe al aterrizar hace que los carritos cercanos investiguen. Recoge la lata del suelo con E o con el botón táctil.

## Equipo Las Sobras

| Integrante | Función |
| --- | --- |
| **Limber Soto Cazu** ([@SotoXo](https://github.com/SotoXo)) | Programación, arquitectura, mecánicas y navegación entre escenas. |
| **Abril Michelle Cayllagua Marca** ([@abrilmichelle460-jpg](https://github.com/abrilmichelle460-jpg)) | Menú principal y arte de escenarios y objetos. |
| **Marco Antonio Calderon Rojas** ([@MarcariusHD](https://github.com/MarcariusHD)) | Personaje principal, Game Over y Victoria, arte de amenazas, inventario y expendedoras. |
| **Edson Teddy Tito Chuquimia** ([@edsontitochuquimia-coder](https://github.com/edsontitochuquimia-coder)) | Menú de pausa y navegación. |

La música y los efectos de campaña se distribuyen en 38 WAV originales creados con `Tools/generate-campaign-audio.cjs`. Las transmisiones de FRANCIS usan señales sintetizadas y subtítulos en español. Se mantienen las atribuciones de las tipografías y recursos existentes en sus carpetas.

## Documentación

- [Auditoría de rendimiento y protocolo A/B](reports/PERFORMANCE_AUDIT.md)
- [Auditoría técnica de campaña](reports/AUDITORIA_FINAL.md)
- [Diseño y dirección artística](reports/GAME_DESIGN_FINAL.md)
- [Recorrido y requisitos por zona](reports/PROGRESION_COMPLETA.md)
- [Validación y checklist manual](reports/VALIDACION_FINAL.md)
