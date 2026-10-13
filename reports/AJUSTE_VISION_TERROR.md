# Ajuste de visión y ambientación de terror

Aplicado sobre la integración de assets existente. Sin Play Mode, prueba manual ni revisión estética. Sin staging, commit, push, merge o rebase.

## Orientación y sincronización

La configuración anterior trataba el foco como si emitiera hacia el eje local -Y. En la versión de URP instalada, el shader Light2D y su lookup angular iluminan el eje local +Y. Se corrigió PlayerVisionController para rotar +Y hacia la dirección compartida con PlayerVisualController:

| Facing del sprite | Rotación local Z del foco |
|---|---|
| Arriba / Back | 0° |
| Abajo / Front | 180° |
| Izquierda | 90° |
| Derecha | -90° |

El foco consulta CurrentDirection de PlayerVisualController en LateUpdate. Su orden de ejecución queda después del controlador visual. La misma dirección define tanto la rotación como el desplazamiento del emisor: no se calculan direcciones desde el input por separado. La resolución diagonal y la orientación al detenerse siguen siendo las del sprite.

El origen del emisor está a (0; 0,30; 0) respecto a los pies, más 0,18 unidades hacia delante. La luz inmediata se centra en ese origen. Se mantienen las sombras del foco y el material iluminado de los sprites.

## Valores de visión

| Parámetro | Antes | Ahora |
|---|---|---|
| Alcance del foco | 4,4 | **6,2 unidades** |
| Radio inmediato | 1,25 | **1,45 unidades** |
| Ángulo exterior | 84° | **88°** |
| Ángulo interior | 56° | **60°** |
| Intensidad del foco | 1,05 | **1,15** |
| Falloff del foco | 0,72 | **0,55** |

Incremento de alcance: **1,8 unidades, aproximadamente 41 %**. La intensidad inmediata sigue en 0,72. Radio, alcance, ángulos, intensidad, falloff, origen y offset quedan serializados para editar desde Inspector. La luz global oscura se conserva en 0,16.

## Luces del escenario

Se reutiliza GroupLampFlicker2D y los cuatro frames de luz_parpadeo existentes. Cada instancia tiene su propio generador aleatorio, fase y próximo instante de cambio. No se utiliza un reloj común de parpadeo ni se modifica el generador aleatorio global del juego.

- AuthoredLamp_0: encendida **2,8–7,2 s**, apagada completamente **0,12–0,45 s**, variación suave de hasta 8 % mientras está encendida.
- AuthoredLamp_1: permanece encendida, intensidad 0,32.
- AuthoredLamp_2: permanece apagada, donde existe.
- AuthoredLamp_3: donde existe, encendida **8–15 s**, apagada **0,08–0,18 s**, variación suave de hasta 4 %.

Las fases cambian según mapa y lámpara; cada instancia comienza con una espera aleatoria. Las pausas dominan sobre los cortes breves para evitar un efecto de estrobo constante. La lámpara con sprite utiliza los frames claros durante encendido y el último durante los cortes.

La configuración está guardada en los cuatro prefabs compartidos:

- nivel1_planta_baja: Z00_Entrada, Z01_Hall, Z02_ARCA.
- nivel2_piso1_cocina_simulacion: Z03_Piso2.
- nivel3_tecnologia: Z04_Piso3, Z05_LabSoftware.
- nivel4_aulas: Z06_NucleoIA.

No se reconstruyeron escenas, arte, colisiones ni posiciones de objetos. Las escenas heredan el cambio desde sus prefabs. Bootstrap guarda los valores nuevos del Player; MainMenu/HallwayFlicker conservan su configuración.

## Archivos de esta tarea adicional

Scripts modificados:

- Assets/_Project/Scripts/Player/PlayerVisionController.cs
- Assets/_Project/Scripts/World/GroupLampFlicker2D.cs
- Assets/_Project/Scripts/Editor/GroupArtIntegrationBuilder.cs
- Assets/_Project/Scripts/Editor/PlayerAndMainMenuBuilder.cs
- Assets/_Project/Tests/EditMode/PlayerVisionDirectionTests.cs: expectativas de rotación actualizadas y comprobación geométrica de alineación con +Y. No se ejecutaron tests ni el juego.

Otros archivos modificados:

- Assets/_Project/Scenes/Bootstrap/Bootstrap.unity
- Assets/_Project/Art/Levels/nivel1_planta_baja/nivel1_planta_baja.prefab
- Assets/_Project/Art/Levels/nivel2_piso1_cocina_simulacion/nivel2_piso1_cocina_simulacion.prefab
- Assets/_Project/Art/Levels/nivel3_tecnologia/nivel3_tecnologia.prefab
- Assets/_Project/Art/Levels/nivel4_aulas/nivel4_aulas.prefab

Archivo nuevo: este informe. No se creó otro sistema de visión ni un nuevo script de iluminación.

El menú de editor Escape UNIFRANZ/Art/Apply Horror Vision and Lighting Only aplica únicamente esta configuración, sin regenerar niveles ni modificar menús. Los builders existentes usan la configuración compartida para conservar estos valores si se regeneran los assets.

## Compilación

Comprobación mediante compilación y Console de Unity 6000.3.16f1: **0 errores y 0 advertencias** al finalizar. Bootstrap y los cuatro prefabs se guardaron correctamente. La verificación visual y jugable queda a cargo del usuario.
