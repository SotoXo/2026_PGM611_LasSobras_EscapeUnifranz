# Corrección de escaleras y muro

- Las tres escaleras de planta baja conservan sus sprites y no generan colisión de mobiliario.
- El desembarco de la escalera pequeña omite dos píxeles aislados de la capa de muros en la colisión; el dibujo original se mantiene completo.
- Se eliminó el recorte artificial de 17 × 17 píxeles en el muro de la sala dental. Los sprites del muro vuelven a cubrir todo el ancho de la imagen original.
- Unity 6000.3.16f1 regeneró el prefab y las tres escenas de planta baja con salida 0. La validación comprobó escaleras, muro y puntos de aparición sobre suelo conectado.
- Los archivos generados se trasladaron al proyecto principal y se verificaron por SHA-256. No se realizó una revisión visual en Play Mode.
