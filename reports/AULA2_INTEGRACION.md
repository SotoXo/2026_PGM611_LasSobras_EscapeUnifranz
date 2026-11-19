# Integración del mapa independiente del aula 2

Estado: mapa integrado en `Z07_Aula2.unity`, registrado en el catálogo y en la compilación.

En Unity, guardar las escenas abiertas y ejecutar:
`Escape UNIFRANZ > Art > Integrate Animated Elevators and Aula 2`.

El integrador utiliza las capas originales de
`Assets/_Project/Art/Levels/nivel4_aulas/NuevoMapa/`, genera
`Assets/_Project/Scenes/Zones/Z07_Aula2.unity` y registra la zona en el catálogo
y en las escenas de compilación. El mapa completo de referencia no se superpone
a las tres capas de juego.

El acceso principal es el ascensor de planta baja: reparar su circuito con
la pieza del inventario abre las puertas y habilita el viaje al aula 2.
El elevador del mapa nuevo permite regresar a planta baja.
También conserva el acceso desde Control de red en `Z06_NucleoIA`.
Mantiene el mapa compartido del piso de aulas.

La escala es 32 píxeles por unidad. Las colisiones se extraen de muros,
objetos y del exterior del piso con precisión de un píxel y geometría compuesta.
El integrador verifica las dimensiones y la
conexión entre la llegada y el regreso con margen para el jugador.

Verificado con Unity 6000.3.16f1 en una copia temporal: integración por lotes
con salida 0, destinos y puntos de llegada válidos, diez fotogramas con sus
duraciones JSON en ambos ascensores y 187/187 pruebas EditMode aprobadas.
Los archivos generados se trasladaron al proyecto principal y se compararon
por SHA-256. La revisión visual en Play Mode queda pendiente.
