# Visibilidad y recorrido del ascensor

- Las tres escaleras de planta baja se dibujan en Decals, por debajo de los personajes. Se verificaron los sprites Prop_022, Prop_106 y Prop_120 en el prefab generado.
- Los muebles altos mantienen su profundidad y colisiones, y se atenúan al ocultar la cabeza o el torso del personaje. Se comprueba la transparencia real de los píxeles para ignorar huecos vacíos.
- Tras reparar el circuito, entrar en la cabina activa el viaje. La interacción también guía al personaje hasta el interior; las puertas se cierran únicamente después de confirmar que está dentro.
- La entrada guiada usa la física 2D, tiene un límite de tiempo y cancela el viaje si hay un obstáculo. El controlador normal se suspende durante ese movimiento y se restaura al terminar.
- En el destino, el personaje aparece dentro de la cabina con las puertas cerradas. Tras la transición visual, se abre la puerta antes de devolver el control para salir.
- El viaje de regreso no se rearma hasta abandonar la cabina. Se eliminó de las capas generadas la puerta fija y la franja que bloqueaba el paso al pasillo del nuevo piso.

## Verificación

Unity 6000.3.16f1 generó las escenas con salida 0 y comprobó los destinos, los puntos de aparición dentro de la cabina y el camino entre la cabina y el pasillo.
Las tres pruebas nuevas pasaron, incluida la simulación física explícita de entrada libre y entrada bloqueada en Play Mode. Las otras 187 pruebas también pasaron durante la comprobación de la suite.
Los recursos generados se copiaron al proyecto principal y se verificaron mediante SHA-256. No se realizó una revisión visual interactiva del recorrido.
