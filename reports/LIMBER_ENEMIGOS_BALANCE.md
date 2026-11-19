# Balance de enemigos

Parámetros en unidades Unity y segundos. Fecha: 19/11/2026.

| Sistema | Valores |
|---|---|
| MiniAuto Entrada | patrulla 1,35; persecución 3,2 |
| MiniAuto Piso2 | patrulla 2; persecución 3,5 |
| MiniAuto Piso3 | patrulla 2; persecución 3,5 |
| Vigilancia Piso3 | patrulla 1,1; persecución 2,6 |
| Perro | patrulla 1,8; persecución 4; reacción 0,65; búsqueda 2 |
| MiniAuto | sospecha 0,25; reacción 0,75 en escenas; persecución máxima 9 |
| Navegación | celdas 0,4; 61 × 61; hasta 1800 expansiones; recálculo mínimo 0,6; sin ruta 1,5 |
| Patrulla | pausa en punto 0,25; destino bloqueado marcado tras 2 |
| Sensores | intervalo 0,1; FOV y radio existentes por escena; sin detección a través de sólidos |
| Latas Bootstrap | alcance 4,5; vuelo 9; ruido 10; investigación 5; cooldown 0,6 |
| Investigación | MiniAuto 1,4; perro 2; próxima distracción después de duración + 2 |
| Reaparición | gracia de contacto 1,25 |
| Alarmas de WorldHorrorDirector | bloqueo 5; permisos permanentes intactos |
| Exoleg | aviso inicial 1,25; telegraph de escena 0,7; peligro 1; pausa 0,55; sobrecarga 0,8 |
| Brazos | telegraph de escena 0,7; golpe 0,75; pausa 0,35; vulnerable 3; parada de emergencia existente conservada |
| Francis | intervalo 4; aviso NETWORK al menos 1,45, HARDWARE 1,1, SHUTDOWN 0,95; exposición final 0,72 |

Justificación: el aviso anterior a contacto permite reaccionar; las latas no anulan visión confirmada; el intervalo entre distracciones deja recuperar la patrulla. La gracia evita contactos inmediatos al reintentar. Las ventanas anuncian el collider activo y los actuadores desconectados dejan de atacar. La persecución tiene límite y la navegación imposible no se consulta cada FixedUpdate.

La navegación está limitada al entorno local y puede rechazar caminos que exijan rodeos mayores. La cuadrícula no es una prueba de que todos los mapas sean transitables; hace falta recorrerlos. La búsqueda más costosa puede concentrar consultas físicas en un frame: medir en build Windows antes de certificar el presupuesto de FPS. No se ha afirmado 60 FPS ni se han añadido paquetes.
