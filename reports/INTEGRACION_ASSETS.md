# Integración de assets reales de EscapeUNIFRANZ

Rama: `feature/integracion-assets`. Implementación en Edit Mode. Sin Play Mode, pruebas manuales, revisión estética, staging, commit, push, merge o rebase.

## Recursos y organización

Los originales en Packages y el RAR se conservan. El juego referencia copias importadas en Assets/_Project/Art:

- **Levels/**: cuatro mapas originales Aseprite, nueve PNG por mapa en Layers/, LevelGeometry.json y un prefab reutilizable por mapa. Sin rasterizar nuevos planos ni rediseñar las habitaciones.
- **Environment/Props/**: bench, chair, conf_table, dental_chair, desk_admision, desk_mac y desk_pc; PNG y Aseprite.
- **Environment/Tiles/**: pared_blanca, pared_gris, pared_naranja, pared_tapa, pared_vertical y los cinco suelos; PNG y Aseprite.
- **Environment/Decals/**: alfombra_logo_piso, grietas, huellas_sangre, papeles y sangre_1. El último se entregó como Aseprite, sin PNG independiente.
- **Items/**: batería, linterna y llave; PNG y Aseprite.
- **Lighting/**: luz_parpadeo, con spritesheet de cuatro frames y Aseprite.
- **Characters/Enemies/Shadow/**: sombra, PNG de doce frames y Aseprite. El primer frame sustituye la apariencia de los patrulleros existentes; no se cambió su IA. No hay sprites específicos de robots en la entrega actual.
- **Characters/Player/Official/**: cuatro Aseprite extraídos del RAR oficial; Generated/ contiene ocho clips y PlayerOfficial.controller. Se conservan los sprites y clips anteriores.

Suelo y Decals conservan sus capas completas. Objetos se divide en sprites de mobiliario, conservando coordenadas originales; paredes se dividen en franjas horizontales para compartir profundidad por Y con el personaje. Los props, tiles y decals ya dibujados en los mapas no se colocaron otra vez encima. Se añadieron los visuales de los terminales existentes con desk_pc y los nodos con batería.

Colisiones, Triggers, Items_guia y Personajes no se renderizan. Luz_Oscuridad se usa como referencia para zonas de luz. Los items reconocidos por comparación con los PNG reales se colocan en las posiciones de la guía: tres en planta baja, dos en cocina, dos en tecnología y tres en aulas. Son integración visual; no se añadieron mecánicas de inventario ni recogida.

## Correspondencia de escenas

| Escena | Mapa visual |
|---|---|
| Z00_Entrada | nivel1_planta_baja |
| Z01_Hall | nivel1_planta_baja |
| Z02_ARCA | nivel1_planta_baja |
| Z03_Piso2 | nivel2_piso1_cocina_simulacion |
| Z04_Piso3 | nivel3_tecnologia |
| Z05_LabSoftware | nivel3_tecnologia |
| Z06_NucleoIA | nivel4_aulas |

Entrada/Hall/ARCA comparten el plano completo de planta baja, con accesos y funciones diferentes; Piso3/LabSoftware comparten tecnología. No son mapas recortados ni siete pisos distintos. Los cuatro planos suministrados no tienen equivalencia exacta con las siete zonas narrativas, especialmente NúcleoIA: se conserva su lógica y objetivo final dentro del plano de aulas disponible, sin inventar un mapa dedicado. Las escenas comparten prefabs y texturas.

Nueva Partida mantiene MainMenu → Bootstrap → entrada/entrada_inicio, ahora con planta baja y spawn tomado de la marca de jugador del plano. No se modificó MainMenu ni HallwayFlicker. Se preservaron las ediciones sin guardar del menú abierto.

## Player, movimiento, animaciones y cámara

- PlayerInputReader conserva WASD y flechas. PlayerMovement2D establece velocidad física en FixedUpdate, normaliza diagonales y ahora usa **4,5 unidades/s**, serializadas en Bootstrap e Inspector. Sin aceleración añadida ni inercia; al soltar input la velocidad se hace cero en el siguiente paso físico.
- Rigidbody2D dinámico: gravedad cero, rotación congelada, damping lineal cero, interpolación y detección continua. Material PlayerFeet sin fricción ni rebote.
- CapsuleCollider2D inferior: tamaño **0,38 × 0,22**, offset **(0; 0,13)**. Tag y layer conservados. Visual a escala 1, con pivote inferior; el root representa los pies.
- Oficial: tres frames por dirección; Idle Down/Up/Left/Right con primer frame y Walk Down/Up/Left/Right a **8 fps**. Los nombres internos Front/Back equivalen a Down/Up. Transiciones sin mezcla ni espera. El idle retiene orientación; diagonal usa eje dominante y, en empates, conserva la orientación anterior compatible.
- PlayerMovement2D y PlayerVisualController tienen orden de ejecución explícito. **PlayerVisionController se conserva** y continúa leyendo CurrentDirection de PlayerVisualController; no se añadió un segundo cálculo de orientación. Luz cercana de 1,25 unidades y foco de 4,4 unidades, sincronizados con el sprite.
- PlayerCameraFollow2D se agregó a la única cámara persistente existente: seguimiento en LateUpdate con SmoothDamp de **0,06 s**, salto inmediato cuando detecta teletransporte superior a 3 unidades. Tamaño ortográfico **4,5**. No se importaron los sistemas de movimiento/cámara de Packages/unity.

## Colisiones, profundidad e iluminación

Las máscaras de Colisiones.png se convierten en rectángulos continuos sin alterar su ocupación de píxeles. El proceso de preparación reconstruye la máscara y exige igualdad exacta. Resultado por prefab: **44 / 20 / 34 / 35** colliders para planta baja/cocina/tecnología/aulas. Cada collider tiene un ShadowCaster2D con contorno rectangular explícito. No se colocó un collider del tamaño de todo el mapa.

Los spawns se colocan sobre espacio libre para el collider inferior. Se mantienen IDs, checkpoints, exits, progresión, encounters, sensores, peligros por contacto, minimapa, HUD y respawn. Las áreas de encounter y los visuales de telegraph existentes se adaptan a las nuevas posiciones. No se cambió detectar → perseguir/contacto por detectar → muerte.

Sorting Layers: Ground, Decals, World, Characters, Foreground y Effects. Los actores y mobiliario usan **World** para poder intercalarse; una capa Characters siempre encima impediría la profundidad por Y. WorldYSort2D utiliza `-Y × 100` con referencia en los pies. Paredes usan el mismo orden en sus franjas. Pixel art: Point, sin mipmaps ni compresión en PNG, proporciones originales, mapas/PNG a **64 PPU**. Oficial a **32 PPU** porque sus dibujos tienen la mitad de resolución respecto al arte exportado del grupo, conservando escala física coherente sin escalar el Transform.

Material común GroupWorldLit de URP. Luz global fría con intensidad **0,16**, luces locales según las zonas más claras de Luz_Oscuridad; solo una por mapa usa los cuatro frames de parpadeo y otra queda apagada cuando existe una tercera posición de luz. El foco del jugador mantiene sombras sobre el mapa.

## Grayboxes y archivos

Se sustituyó el escenario principal de **las siete zonas**. Environment, WorldCollision y PresentationDecor anteriores se desactivaron para recuperación, sin eliminar sus datos. Los grandes placeholders visuales de encuentros se ocultaron; los indicadores geométricos de peligro conservan su función de aviso. No existen assets específicos de EXOLEG/brazos en los recursos entregados.

Archivos de código nuevos:

- Assets/_Project/Scripts/Editor/GroupArtIntegrationBuilder.cs
- Assets/_Project/Scripts/Editor/EscapeUNIFRANZ.Editor.asmdef
- Assets/_Project/Scripts/Player/PlayerCameraFollow2D.cs
- Assets/_Project/Scripts/World/WorldYSort2D.cs
- Assets/_Project/Scripts/World/GroupLampFlicker2D.cs

Archivos modificados por la implementación: Bootstrap.unity, las siete escenas de Zones/, PlayerMovement2D.cs, PlayerVisualController.cs y ProjectSettings/TagManager.asset. ProjectSettings/ProjectSettings.asset ya tenía un cambio del usuario antes de comenzar y se conservó.

Otros archivos nuevos: recursos y .meta en las carpetas anteriores; cuatro prefabs, cuatro LevelGeometry.json, GroupWorldLit.mat, PlayerFeet.physicsMaterial2D, ocho clips oficiales, controlador oficial; reports/inspect_group_art.py, reports/prepare_group_art.py, reports/source_maps.png (análisis de fuentes), reports/group-art-placement.txt, este informe e inventario group-art-files.json.

El integrador se puede volver a ejecutar desde Escape UNIFRANZ/Art/Integrate Group Assets (No Play Mode); actualiza sus assets y conserva los IDs de sprites existentes.

## Compilación

Comprobación en Unity 6000.3.16f1, sin ejecutar el juego. Los errores iniciales de referencias del código de editor se resolvieron separando su assembly. Console final: **0 errores y 0 advertencias**. La apariencia, el recorrido, los encounters y la sensación de controles quedan pendientes de tu revisión manual.
