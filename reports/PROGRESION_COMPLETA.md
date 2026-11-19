# Recorrido completo

La ruta normal empieza en `MainMenu`, carga `Bootstrap` y entra en `Z00_Entrada` después del prólogo. Esta guía incluye soluciones.

| Escena | Acciones y requisitos | Resultado |
| --- | --- | --- |
| `Z00_Entrada` | Recoge la linterna junto a recepción. Inspecciona la terminal y busca la batería de respaldo en la sala dental. Lee la nota e inspecciona los bornes. Instala la batería; une ENERGÍA con ENERGÍA y DATOS con DATOS. Tras la secuencia de restauración, abre CCTV y visita la cámara del Hall. Introduce **3174** en el lector. | Se registra el código observado, se recupera el plano y se autoriza el Hall. |
| `Z01_Hall` | Localiza la llave en la cafetería. Lee el aviso y utiliza el acceso de ARCA. El gabinete queda a la espera de la tarjeta de Simulación. | La llave abre ARCA. Las cámaras pueden cerrar brevemente el acceso a la escalera. |
| `Z02_ARCA` | Lee el protocolo y la pantalla. Reinicia **PRINCIPAL → CONTROL → SEGURIDAD**. Regresa por la puerta del Hall. | Se emite la tarjeta de laboratorio y se habilita la escalera hacia Simulación. |
| `Z03_Piso2` | Recoge el fusible, lee la ficha e inspecciona los conectores. Instálalo; ejecuta **ENERGÍA → PROCESADOR → RED → ACTUADOR**. Empareja **TIERRA, ENERGÍA, DATOS y CONTROL**. Neutraliza los dos actuadores del Exoleg y valida CONTROL tras la ventana de peligro. | Se emite la tarjeta de mantenimiento y se habilita la ruta a Tecnología. |
| `Z01_Hall` de vuelta | Vuelve desde Simulación, usa la tarjeta de mantenimiento, lee el documento y sigue **USAR TARJETA → REVISAR GABINETE → RETIRAR MÓDULO** en el gabinete. Regresa a Simulación y sigue a Tecnología. | Se recupera el módulo de red y queda liberada la salida a Tecnología. |
| `Z04_Piso3` | Lee la topología e inspecciona el rack. Instala el módulo y conecta **SERVIDOR → SWITCH-02 → LAB**. Usa la tarjeta de ARCA en el acceso a Software cuando la red y el encuentro estén resueltos. | Quedan restaurados la red y el permiso al laboratorio. |
| `Z05_LabSoftware` | Lee las fichas y consulta el monitor. Aísla **SEGURIDAD**. Respeta el halo de los brazos y alcanza **EMERGENCY STOP**. | Se desactivan brazos y seguridad y se habilita el núcleo. |
| `Z06_NucleoIA` | Lee el protocolo y la consola. En RED usa **CÁMARAS → PUERTAS → SERVIDOR → AISLADO**. En HARDWARE usa **Alimentación → Actuadores → Seguridad**. En APAGADO usa **AISLAR → VERIFICAR → APAGAR**. Cruza el corredor de vuelta y usa **Salir por la vía de emergencia**. | Quedan registrados los checkpoints de fase. Aparece el epílogo, Victoria y el acceso a créditos o menú. |

El inventario viaja entre escenas. Si un dispositivo indica que el objeto seleccionado es incompatible, quita la selección o elige el objeto solicitado. La linterna se activa después de obtenerla. El diario muestra documentos leídos y ofrece una segunda indicación en **Ayuda del objetivo**. En Simulación, el mostrador de cocina ofrece cobertura mientras el jugador esté quieto. Una unidad cercana aún puede localizarlo. Las zonas se pueden volver a visitar siguiendo las conexiones de retorno habilitadas.

## Enlaces y checkpoints

| Origen → destino | Punto de llegada |
| --- | --- |
| Entrada → Hall | `from_entrada` |
| Hall → Entrada | `from_hall` |
| Hall → ARCA / ARCA → Hall | `from_hall` / `from_arca` |
| Hall → Simulación / Simulación → Hall | `from_hall` / `from_piso2` |
| Simulación → Tecnología / Tecnología → Simulación | `from_piso2` / `from_piso3` |
| Tecnología → Software / Software → Tecnología | `from_piso3` / `from_lab` |
| Software → Núcleo / Núcleo → Software | `from_lab` / `from_nucleo` |

Los checkpoints del núcleo son `francis_inicio`, `francis_fase2` y `francis_fase3`. Después del apagado, el mismo acceso usado para volver al laboratorio completa la extracción. Continuar recupera el checkpoint guardado y conserva el avance de las fases concluidas.
