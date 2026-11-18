# Validación de Escape UNIFRANZ

Las cifras de esta primera validación corresponden a la campaña de `feat/terror-final`. Para las compilaciones y las pruebas de la corrección de menú y rendimiento, consulta [Performance Audit](PERFORMANCE_AUDIT.md).

## Resultados automatizados

- Unity **6000.3.16f1** importó y compiló el proyecto. Se trabajó en EditMode y por lotes.
- La suite inicial de `feature/marco` dio **176 de 176**. La suite integrada dio **182 de 182** y cero fallos. Incluye guardado, copia de recuperación, inicio mediante Continuar, colisiones con puerta, contacto físico y restauración de la fase final.
- Se auditaron nueve escenas, doce transiciones con destinos y retornos válidos, 34 puntos de aparición y 25 grupos de luz activos. La comprobación cargó los once puzles de escena, encontró las fuentes de sus pistas y completó sus soluciones en el modelo de datos. El flujo de Entrada tiene una batería y una linterna activas y un feed con el código. La cuadrícula alcanzó las interacciones esenciales, incluidos los controles físicos de los encuentros.
- Se verificaron ocho objetos en el catálogo, sesenta y cuatro entradas de audio con clips y un AudioListener en menú y partida. Se comprobó la presencia de los atlas y referencias de los cuatro carritos.
- El jugador de **Windows x64** se compiló con las nueve escenas. El ZIP contiene el ejecutable, los datos y las bibliotecas de Unity. Se comprobó que admite lectura como archivo ZIP.
- El APK **Android ARM64** se compiló con IL2CPP y OpenGLES3. `aapt dump badging` confirmó el paquete `com.unifranz.escape`, versión `1.0.1`, API mínima 25 y biblioteca nativa `arm64-v8a`. La compilación generó el APK de prueba en `Builds/Android/EscapeUNIFRANZ.apk`.
- Para repetir la auditoría, usa **Escape UNIFRANZ → Campaña → Auditar recorrido**. El informe se genera en `Logs/campaign-audit.txt`. Las pruebas se ejecutan desde **Window → General → Test Runner → EditMode**.

## Pasada manual pendiente

1. Abre el ejecutable de Windows o entra manualmente en Play desde `MainMenu`. Revisa navegación por teclado, ratón y controles a 1280 × 720 y 1920 × 1080.
2. Ajusta volumen y activa Reducción de destellos. Sal al menú y vuelve a abrir Opciones. Verifica que ambas preferencias persistan. Repite la secuencia de luz de Entrada con el ajuste activado y desactivado.
3. Comienza una partida y comprueba las tres tarjetas del prólogo, el inicio desde recepción, las indicaciones de movimiento y el efecto de reiluminación. Abre el diario y revisa los dos niveles de ayuda del objetivo. Rodea escritorios y paredes con la linterna encendida. Revisa el borde visible de la cámara y los pasos estrechos.
4. Sigue [el recorrido completo](PROGRESION_COMPLETA.md), incluidos el regreso al gabinete del Hall y la salida posterior del núcleo. Fuerza la alarma del Hall y verifica que la puerta vuelva a responder tras su intervalo. Abre y cierra la puerta interior, incluyendo el intento con alguien en el paso.
5. En Simulación y Tecnología, compra y recoge una lata, lánzala frente a una pared y al pasillo abierto. Comprueba el aterrizaje, la recuperación y la investigación del carrito. Prueba la cobertura del mostrador de Simulación, permanece quieto y sal moviéndote. Confirma que un muro corta la detección y que la derrota requiere contacto con la amenaza.
6. Provoca una derrota, prueba Reintentar, vuelve al menú y cierra la aplicación. Abre de nuevo y pulsa Continuar. Verifica zona, batería consumida, objetos, pistas, luces y puertas. Repite después de las fases uno y dos de FRANCIS. Completa el apagado, toma el acceso de extracción y comprueba Victoria, créditos y regreso al menú.
7. En un Android ARM64, instala el APK de prueba. Comprueba aspecto horizontal, áreas seguras, toque simultáneo de joystick e interacción, cambio de foco, pausa, lanzamiento y regreso a la partida guardada. Revisa el rendimiento de luces y la claridad del texto en un dispositivo real.

El análisis geométrico verifica la conectividad de cajas de mapa con una cuadrícula conservadora. La lectura de escena, la mezcla en altavoces y las ventanas de evasión se comprueban jugando. No se inició Play Mode ni se ejecutó el juego automáticamente.
