# Pruebas de enemigos y progresión

Fecha: 19/11/2026. Unity 6000.3.16f1. No se entró en Play Mode.

## Resultados verificables

- Suite previa a ampliar pruebas: 187/187 aprobadas.
- Suite ampliada inicial: 196/196 aprobadas.
- Suite final: **205/205 aprobadas, 0 fallidas, 0 omitidas**, duración reportada 5,1097754 s. Job MCP: 21a92992fdb94ad7acedad0cbb5da19c. Evidencia íntegra: LIMBER_EDITMODE_RESULT.json.
- Una solicitud intermedia de tests no inició dentro del timeout mientras el editor recargaba; no se contabilizó como ejecución aprobada. La ejecución final se realizó después de terminar la importación.
- Consola Unity al finalizar: 0 entradas de error. Estado final: Unity 6000.3.16f1, compiling=false, playMode=false, una escena abierta.
- Compilación auxiliar .NET con referencias locales pasó. Primero faltaban recursos de restore; después del restore pasó. Se corrigió la advertencia inicial del temporizador de gracia sin asignar. La compilación e importación finales se validaron mediante Unity y sus pruebas.
- Auditoría MCP de las siete escenas: sin scripts ausentes encontrados, componentes principales y rutas referenciados; nueve nodos de Francis con phase/step coherentes. LIMBER_SCENE_AUDIT.txt registra conexiones y campos opcionales.

## Pruebas añadidas

EnemyFinalIntegrationTests (11): pared sólida oculta y trigger no oculta; navegación rodea una pared sin entrar en ella; objetivo sellado no provoca teletransporte; lata no cancela objetivo visible; investigación usa el origen y rechaza spam; reinicio permite otra distracción; robot desactivado ignora ruido; reinicio termina suspensión; gracia evita contacto al reaparecer; bloqueo temporal se limpia conservando autorización; error SHUTDOWN conserva progreso interno y mostrado; extracción exige todas las fases.

EnemySceneWiringTests (7 casos): abre cada escena en modo aditivo y la cierra sin guardar. Comprueba scripts, Rigidbody2D, referencias de rutas/waypoints, ausencia de sólidos a radio 0,18 en waypoints, arte MiniAuto y referencias de vistas. El waypoint inicialmente obstruido de vigilancia se corrigió y los siete casos pasan.

Las pruebas existentes cubren máquinas de Exoleg/perro/brazos, secuencias completas de Francis, persistencia de flags, guardados, resolución de checkpoints, filtros de contacto, flujo de presentación, inventario y puertas. Pasar la lógica no sustituye un recorrido del mapa.

## Correcciones validadas

Navegación local con obstáculos sintéticos; pérdida del objetivo e investigación mediante lógica existente; prioridad de visión sobre ruido; segunda distracción después de reset; suspensión temporal; contacto seguro tras reset; requisitos finales de victoria; conservación de progreso de apagado; rutas y referencias físicas de las siete escenas. Los valores y justificación están en LIMBER_ENEMIGOS_BALANCE.md.

## Pendientes de Play Mode y build Windows

- [ ] Recorrer Z00 → Z06 y volver hasta la extracción real, activar GameCompleted, ver victoria y regresar al menú.
- [ ] Verificar consumo y recuperación de lata en inventario real; tecla G/Android; puerta cerrándose durante vuelo; impacto y radio acústico cercano/lejano.
- [ ] Revisar a velocidad real los sprites en cuatro direcciones, reposo, ataque, impacto, aturdimiento, OFF y retorno.
- [ ] Verificar perro en patrulla/alarma/persecución/búsqueda, refugio, dos nodos y acceso al laboratorio.
- [ ] Completar Exoleg con ACTUATOR 1 → ACTUATOR 2 → CONTROL, comprobar interacción alcanzable en ventana segura.
- [ ] Completar brazos con advertencia, golpe, ventana vulnerable y panel; observar coincidencia de arte/área real.
- [ ] Completar NETWORK → HARDWARE → SHUTDOWN; provocar error en cada fase; morir/reintentar en cada checkpoint; salir y cargar partida.
- [ ] Comprobar alarma y desbloqueo tras cinco segundos; reintentar durante alarma; conservar permisos permanentes.
- [ ] Verificar muertes únicas por contacto válido, ninguna por sensor y ausencia de bucles de muerte al reaparecer.
- [ ] Perseguir y escapar por puertas/pasillos/mobiliario; probar obstáculos móviles y objetivos inaccesibles. La cuadrícula tiene alcance local finito.
- [ ] Comprobar que la vuelta al laboratorio queda libre después de apagar Francis y que no se activa victoria antes.
- [ ] Medir FPS/frame time, Physics2D, GC y picos de navegación en build Windows a 1920×1080, especialmente varias unidades recalculando. Comparar con una build base bajo el mismo recorrido.

Estos puntos continúan pendientes: no se certifica la aceptación completa ni el rendimiento estable de la campaña a partir de EditMode.

## Cómo reproducir

Abrir este proyecto con Unity 6000.3.16f1; esperar a terminar importación y compilación. Window → General → Test Runner → EditMode → Run All. Se esperan 205 casos. Para ver únicamente el trabajo nuevo, filtrar EnemyFinalIntegrationTests y EnemySceneWiringTests. La suite abre escenas de forma aditiva, sin Play Mode. Guardar cambios de escena propios antes de ejecutar pruebas de integración. Con MCP, run_tests(mode=EditMode) y consultar get_test_job hasta completar.

Las comprobaciones manuales deben realizarse expresamente en Play Mode o build Windows por el equipo y registrar resultados, capturas y profiler antes de marcar los criterios finales como completados.

