# Escape UNIFRANZ

Videojuego de terror, supervivencia y exploración 2D en pixel art, desarrollado en Unity y C# por el grupo **Las Sobras** como proyecto integrador de Ingeniería en Sistemas de UNIFRANZ, gestión 2026.

Un corte de energía deja a un estudiante dentro de la universidad. FRANCIS toma los accesos y usa las voces del campus para atraerlo. La campaña base recorre siete zonas desde recepción hasta el apagado del núcleo y la huida por la puerta del laboratorio. La ampliación incorpora un mapa independiente de aulas conectado con planta baja mediante un ascensor.

**Docente:** Ing. Ruben Gonzalo Soria Soria.

## Descargas de la entrega

- [Ejecutable para Windows y APK para Android en Google Drive](https://drive.google.com/drive/folders/1dLn3Wdapo5AHEx9H0LALqf1XsUoNcHr-?usp=sharing).
- [Repositorio GitHub del proyecto](https://github.com/SotoXo/2026_PGM611_LasSobras_EscapeUnifranz).
- [Rama de integración feature/marco](https://github.com/SotoXo/2026_PGM611_LasSobras_EscapeUnifranz/tree/feature/marco).

Para Windows, descarga y descomprime el paquete completo; conserva el ejecutable junto a su carpeta de datos y las bibliotecas. Para Android, descarga el archivo APK e instálalo en un dispositivo compatible. Los paquetes de entrega se distribuyen desde la carpeta de Drive compartida por el equipo.

## Avance y mecánicas principales

- Introducción narrativa de tres tarjetas sobre el último turno, el corte de energía y el control de FRANCIS.
- Tutorial contextual de movimiento, interacción, inventario, linterna, diario, escáner, mapa y evasión de amenazas.
- Exploración, pistas, puzles de circuitos y secuencias, inventario, accesos condicionados, amenazas y recuperación desde checkpoints.
- Ascensor habilitado al resolver su circuito: entrada en cabina, cierre de puertas, transición al mapa de aulas y apertura para salir. El retorno se habilita después de abandonar la cabina.
- Mapa independiente de Aula 2 (`Z07_Aula2`) con correcciones de colisiones y profundidad en escaleras, marcos, paredes y mobiliario.
- Tres robots de demostración con los diseños garra, rampa y sierra de Limber, colocados en el pasillo del mapa adicional. Estas unidades de prueba no causan daño por contacto.
- Expendedora de prueba de Abril con animación de entrega, compra y recogida de latas mediante interacciones separadas.
- Mejoras recientes de Abril: orientación y poses del personaje al interactuar, contorno pulsante de objetos disponibles, efectos de uso y recogida, sombra, rebote, polvo y sonido de las latas, y ajustes de iluminación de habitaciones.

La campaña base y el mapa adicional se documentan por separado. La llegada al piso de aulas no activa por sí misma la victoria. Los informes de pruebas registran los casos comprobados y el recorrido manual pendiente de cada revisión.

## Ejecutar

La configuración de compilación de la campaña utiliza la versión **1.0.1**.

- **Unity 6000.3.16f1.** Añade esta carpeta desde Unity Hub y abre `Assets/_Project/Scenes/UI/MainMenu.unity`. Pulsa Play manualmente para jugar en el editor.
- **Windows x64.** Compila con **Escape UNIFRANZ → Campaña → Compilar Windows x64**. Ejecuta `Builds/Windows/EscapeUNIFRANZ.exe` junto con su carpeta de datos y las DLL.
- **Android.** Usa **EscapeUNIFRANZ → Build → Build Android APK** con los módulos Android Build Support, SDK, NDK y OpenJDK de Unity. El perfil del equipo usa ARM64, IL2CPP, OpenGLES3, Android 7.1 o superior y orientación horizontal. Incluye joystick y botones táctiles. Consulta el informe de validación antes de distribuir.

El configurador Android enumera las nueve escenas de la campaña base. Para generar un paquete que incluya el mapa adicional de aulas, incorpora también `Z07_Aula2` a la lista de escenas usada por el constructor antes de compilar.

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

Los nombres corresponden a la carátula del informe actualizado del equipo.

| Integrante | Rol y aportes |
| --- | --- |
| **Limber Soto Cazu** ([@SotoXo](https://github.com/SotoXo)) | Programación, arquitectura, mecánicas y navegación entre escenas; recursos y comportamientos de los robots garra, rampa y sierra. |
| **Abril Michelle Callyagua** ([@abrilmichelle460-jpg](https://github.com/abrilmichelle460-jpg)) | Menú principal y arte de escenarios y objetos; animación de la expendedora, poses de interacción, efectos de uso y recogida, lanzamiento de latas y ajustes de iluminación. |
| **Marco Antonio Calderon Rojas** ([@MarcariusHD](https://github.com/MarcariusHD)) | Diseño y funcionamiento del juego; personaje principal, pantallas Game Over y Victoria, inventario; integración del mapa de aulas, ascensor, robots y expendedora; corrección de colisiones y visibilidad, pruebas e informe. |
| **Edson Teddy Tito Chuquimia** ([@edsontitochuquimia-coder](https://github.com/edsontitochuquimia-coder)) | Menú de pausa y navegación de sus opciones. |

## Créditos de recursos

La música y los efectos de campaña se distribuyen en 38 WAV originales creados con `Tools/generate-campaign-audio.cjs`. Las transmisiones de FRANCIS usan señales sintetizadas y subtítulos en español. Se mantienen las atribuciones de las tipografías y recursos existentes en sus carpetas.

## Documentación

- [Auditoría de rendimiento y protocolo A/B](reports/PERFORMANCE_AUDIT.md)
- [Auditoría técnica de campaña](reports/AUDITORIA_FINAL.md)
- [Diseño y dirección artística](reports/GAME_DESIGN_FINAL.md)
- [Recorrido y requisitos por zona](reports/PROGRESION_COMPLETA.md)
- [Validación y checklist manual](reports/VALIDACION_FINAL.md)
- [Integración de robots, expendedora y accesos de Aula 2](reports/AULA2_EQUIPO_Y_ACCESOS.md)
- [Integración del mapa independiente de Aula 2](reports/AULA2_INTEGRACION.md)
- [Visibilidad del personaje en la cabina y el mobiliario](reports/CABINA_VISIBILIDAD.md)

## Anexos y enlaces de entrega

| Anexo | Contenido | Enlace |
| --- | --- | --- |
| Informe | Informe del proyecto con carátula, índices, tablas, figuras, bibliografía y anexos. | [Descargar informe en Word](reports/InformeEntrega/Informe_UNIFRANZ_actualizado.docx) |
| B | Código fuente y documentación del proyecto. | [Repositorio GitHub](https://github.com/SotoXo/2026_PGM611_LasSobras_EscapeUnifranz) |
| B | Rama utilizada para las integraciones descritas. | [feature/marco](https://github.com/SotoXo/2026_PGM611_LasSobras_EscapeUnifranz/tree/feature/marco) |
| C | Descargas del ejecutable de Windows y del APK de Android. | [Carpeta compartida en Google Drive](https://drive.google.com/drive/folders/1dLn3Wdapo5AHEx9H0LALqf1XsUoNcHr-?usp=sharing) |
