# Accesos y contenido del equipo en el piso nuevo

Se reemplazó la cuadrícula física de cuatro píxeles por geometría de un píxel, fusionada en un solo CompositeCollider2D. Los marcos de once accesos conservan su dibujo original y tienen una holgura física de 16 píxeles. El cálculo de paso utiliza el tamaño y desplazamiento del collider de los pies del jugador.

Los muebles se renderizan como objetos completos con profundidad en su base. Su colisión ocupa la base; los muebles altos utilizan la atenuación de oclusión para mantener visible al personaje. Las paredes siguen usando el arte original.

Los recursos de MiniAutos que llegaron con la integración de Limber (`db327d7`) ya estaban en esta rama. Se reutilizan los scripts y las hojas originales de garra, rampa y sierra. Ahora hay tres robots de prueba visibles, con patrullas cortas en el pasillo central del piso nuevo. Esta muestra no tiene contactos de daño.

La expendedora de prueba de Abril utiliza su VendingMachineInteractable, los seis fotogramas originales, el producto soda, el inventario y el lanzador de latas. Está en el corredor al salir del ascensor, identificada con su nombre. E entrega la lata y otro E la recoge; G permite lanzarla. Se corrigió la pérdida de la referencia al producto durante los cambios de escena del instalador.

## Comprobaciones

- Generación del mapa con Unity 6000.3.16f1: salida 0.
- Conexión de la cabina y ocho puntos de entrada a aulas comprobada con el volumen de los pies del jugador.
- Prueba de puertas de 16/18 píxeles transitables y puerta de 10 píxeles bloqueada.
- Bases de mobiliario, tres robots con sus hojas de movimiento/impacto/verticales y rutas de cuatro puntos, seis fotogramas de expendedora, compra, recogida y registro persistente de stock.
- Suite completa: 193 pruebas aprobadas, cero fallos.
- Recursos trasladados al proyecto principal y comparados mediante SHA-256. La apariencia de las seis capturas no se revisó de forma interactiva en Play Mode.
