# Assets visuales — segunda pasada

Auditoría de archivos de `Assets/_Project/Art` y `Packages`: 201 archivos visuales, contando fuentes y copias importadas. Inventario exacto: `ASSETS_AUDITADOS_SEGUNDA_PASADA.json`. No se descargó ni sustituyó arte del grupo.

Existen: jugador oficial en cuatro direcciones con tres frames por dirección; sombra de 12 frames; batería, llave y linterna; siete props (desk_pc, desk_mac, desk_admision, dental_chair, conf_table, chair, bench); tiles de paredes/suelos; papeles, grietas, huellas y logo; cuatro mapas por capas y luminaria de cuatro frames. Las notas y correos se presentan con esos papeles/PCs y texto propio. Blanco/amarillo/rojo se producen con Light2D: no son sprites nuevos.

La sombra **es provisional para los robots**. Francis usa temporalmente el jugador con tinte. SecurityCameraHead.asset es un recorte de desk_pc, no un sprite real de cámara. El fusible usa batería como representación provisional. Las tarjetas y el módulo tienen texto funcional en inventario, sin arte propio.

| Asset faltante | Tipo | Dónde se usa | Prioridad |
|---|---|---|---|
| **FALTA ASSET: MINI ROBOT** | Sprite + idle/patrol/alert/chase/disabled, giro y reinicio | Entrada, Simulación y Tecnología | ALTA |
| **FALTA ASSET: ROBOT DE VIGILANCIA** | Sprite + estados idle/patrol/alert/chase/disabled | Tecnología; aislamiento desde CCTV | ALTA |
| **FALTA ASSET: ROBOT DOG** | Sprite + patrulla, alerta, persecución, frenado, carga y disabled | Tecnología; estación de carga | ALTA |
| **FALTA ASSET: EXOLEG** | Sprite + preparación/ataque; actuador izquierdo/derecho apagado y control offline | Simulación | ALTA |
| **FALTA ASSET: BRAZOS ROBÓTICOS** | Sprite + preparación, golpe, pausa y parada | Software | ALTA |
| **FALTA ASSET: FRANCIS** | Cuatro direcciones + movimiento y confrontación | Núcleo/final | ALTA |
| Cámara CCTV activa | Sprite rotatorio + variantes activa/apagada | Entrada, Hall, Simulación, Tecnología, Software y aulas | ALTA |
| Cámara CCTV rota | Variante con daño físico | Feed sin señal; futura reparación visual | MEDIA |
| Fusible | Item de mundo e icono de arrastre | Simulación/caja de fusibles | MEDIA |
| Tarjeta de acceso/laboratorio | Item/icono | ARCA y laboratorio | MEDIA |
| Tarjeta de mantenimiento | Item/icono | Simulación y gabinete del Hall | MEDIA |
| Módulo de red | Item/icono | Gabinete del Hall y Tecnología | MEDIA |
| Panel eléctrico | Prop interactivo + recuperado/sin energía | Entrada y controles de robots; ahora desk_pc | ALTA |
| Breaker/interruptores | Prop con posiciones y estados | ARCA y cortes de alimentación; UI funcional existente | MEDIA |
| Caja de fusibles | Prop + hueco vacío/fusible colocado | Simulación; ahora desk_pc y UI | MEDIA |
| Rack de comunicaciones | Prop abierto/cerrado | Hall y Tecnología; ahora desk_pc | MEDIA |
| Switch/router | Prop + enlace normal/comprometido | Tecnología | MEDIA |
| Pantalla dañada | Variante de PC/terminal | Diagnósticos comprometidos/sin señal | MEDIA |
| Puerta normal interactiva | Sprite + apertura/cierre | Accesos entre zonas; no hay archivo dedicado | MEDIA |
| Puerta electrónica | Sprite + apertura/bloqueo/recuperación | Accesos controlados por IA; señalización funcional existente | ALTA |
| Puerta bloqueada | Variante o animación de cierre | Accesos y alarmas | ALTA |
| Lector de tarjeta/código | Prop + LED/estados | Hall, laboratorio y Entrada | MEDIA |
| Estación de carga | Prop + cierre y unidad atrapada | Tecnología; actualmente consola y luz amarilla | MEDIA |
| Luminaria físicamente rota | Variante dañada | Puntos defectuosos; existe sprite de luz apagada, no daño específico | MEDIA |
| Señal EXIT/emergencia | Señalética dedicada | Accesos recuperados | MEDIA |
| LED rojo dibujado | Sprite opcional; el LED Light2D ya funciona | Robots/cámaras | BAJA |
| Conectores y cables con acabado propio | UI opcional; trazos y puertos ya funcionan | Puzzles de cables/fusible | BAJA |
| Iconos PWR/DATA/CTRL/GND finales | UI opcional; cuatro iconos originales de líneas ya implementados | Puzzles | BAJA |
| Keypad con acabado propio | UI opcional; botones numéricos ya funcionan | Lector 3174 | BAJA |
| Marco CCTV y nodos de red finales | UI opcional; feeds esquemáticos y grafo funcional existentes | CCTV/Redes | BAJA |
| Señalética de paneles/gabinete/estación | Sprites opcionales; rótulos TMP existentes | Tutorial y backtracking | BAJA |
| NPCs | Sprites/animaciones solo si se añaden NPCs | No hay NPC nuevo implementado en esta pasada | BAJA (OPCIONAL) |
| Retratos de diálogo | Ilustraciones solo si se decide usar retratos | Los diálogos actuales son texto; no se necesitan para funcionar | BAJA (OPCIONAL) |

Batería, llave, linterna, jugador, computadora/terminal genérico, documentos y mapas **ya existen**. No se listan como faltantes. El módulo de interferencia/Q no se implementó: se priorizaron defensas ambientales, por lo que su arte no es requisito actual. Las filas BAJA son acabado visual opcional, no mecánicas pendientes.
