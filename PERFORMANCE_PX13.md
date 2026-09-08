# Investigación de rendimiento: ASUS ProArt PX13 HN7306WV

7 de septiembre de 2026. Unity 6000.3.5f2, URP 17.3.0.

## Conclusión y límites

La foto identifica una ProArt PX13 HN7306WV con Ryzen AI 9 HX 370 y Radeon 890M. El usuario confirma que estaba conectada al cargador, que Windows empieza con pocos FPS y empeora, y que Chrome se congeló durante una partida. En otras computadoras el juego funciona bien.

La información del procesador en msinfo32 **no identifica la GPU con la que se ejecutó el juego**. No hay todavía un Player.log, un reporte de chrome://gpu ni una captura del Profiler de la PX13. Por ello, la selección de GPU, el modo de energía y un problema de drivers son hipótesis prioritarias, no causas demostradas. La prueba local usa una RTX 4070 Ti; no sustituye una prueba en la PX13.

Se corrigieron problemas verificables del código y del setup que reducen el margen de rendimiento. No se puede garantizar compatibilidad con cualquier hardware o reparar desde Unity un controlador que se bloquea.

## Investigación del equipo

- ASUS identifica las variantes de la familia HN7306 y sus gráficos híbridos en sus [especificaciones de la PX13](https://www.asus.com/es/laptops/for-creators/proart/proart-px13-hn7306/techspec/).
- ProArt Creator Hub permite limitar la potencia de CPU/GPU mediante Whisper; Performance aumenta la potencia y requiere alimentación externa. Estar conectado no demuestra que Performance esté seleccionado. [Documentación oficial de ASUS](https://www.asus.com/us/support/faq/1050476/).
- El modo GPU Eco puede desactivar la NVIDIA. ASUS indica revisar GPU Mode en ProArt Creator Hub y usar Standard para habilitar la detección de la dedicada. [ASUS: GPU no detectada](https://www.asus.com/ca-en/support/faq/1051385/).
- Windows puede imponer la GPU por aplicación por encima de la preferencia del panel NVIDIA. El ejecutable del juego y Chrome tienen preferencias independientes. [NVIDIA: selección del procesador gráfico](https://www.nvidia.com/content/Control-Panel-Help/vLatest/en-us/mergedProjects/nv3d/Setting_the_Preferred_Graphics_Processor.htm).
- Hay testimonios de congelamientos en la PX13 y discusiones sobre ambos drivers, AMD y NVIDIA. Son reportes de usuarios, sin demostrar que tengan la misma causa que este juego. [Hilo específico de la PX13](https://www.reddit.com/r/ProArt_PX13/comments/1kmp9mv/screen_partially_freezes_sometimes/).

## Hallazgos del proyecto y cambios

| Hallazgo comprobado | Corrección |
| --- | --- |
| Cada configuración de un orbe del pool creaba otro Material y perdía la referencia al anterior. | Reutilizar la instancia cuando la plantilla es la misma; liberar la anterior cuando cambia y la última al destruir el pickup. Usar sharedMesh. |
| La destrucción de edificios leía Renderer.materials, que clona implícitamente, y después volvía a clonar; las copias no tenían limpieza. | Leer sharedMaterials, crear una copia por slot y liberar las copias en OnDestroy. |
| UpdateManager procesaba altas/bajas diferidas en listas pero no en sus HashSets. Quedaban referencias obsoletas y registros inconsistentes. | Mantener ambos contenedores sincronizados, deduplicar solicitudes y respetar la última alta/baja de cada ciclo. |
| La dispersión buscaba vecinos comparando todos contra todos y reservaba listas dentro del bucle. | Usar la cuadrícula espacial existente, reutilizar buffers y repartir el escaneo y los avisos de teletransporte en bloques de hasta 16 por frame. Una concentración muy densa sigue requiriendo trabajo, pero no se procesa todo el escaneo en un único frame. |
| El overtime precalentaba inmediatamente el pool hasta 2000 aunque el límite subía gradualmente. | Precalentar hasta el límite inicial efectivo. El pool continúa creciendo según la demanda. Se conservan los límites de enemigos y la progresión del overtime. |
| La transparencia recalculaba las mismas proyecciones de enemigos por cada edificio. | Proyectar cada objetivo una vez por detección y reutilizar su posición en pantalla y distancia. |
| Windows usaba Forward+; el inventario del nivel contiene una luz. | Usar Forward, conservando el asset de calidad PC y sus demás opciones. Evita la preparación de listas de luces de Forward+ para este setup. No se atribuye una ganancia medida en la PX13. |
| Los bloques de optimización URP de WebGLOptimizer estaban vacíos. QualitySettings de sombras/AA no configuraban esas opciones en URP. | Aplicar shadowDistance=0, MSAA=1 y el límite de luces adicionales en una copia de URP creada para la ejecución web. Restaurar y liberar la copia al terminar. |
| La plantilla web dejaba que el DPR del dispositivo aumentara la resolución interna. Con DPR=2 el mismo canvas tiene cuatro veces los píxeles. | Plantilla Manners con devicePixelRatio=1, seleccionada en ajustes globales y en ambos perfiles web. Se conserva el manejo de tamaño y pantalla completa de Unity. |
| El monitor buscaba objetos globalmente durante bajones y promediaba FPS instantáneos usando tiempo escalado. | Contadores del pool, FPS calculados como frames/tiempo real y reportes espaciados. Panel F8 disponible en builds normales con GPU, API, resolución, memoria y población. |

Unity documenta el ajuste de DPR como una forma de reducir el coste de render en pantallas de alta densidad: [tamaño del canvas web](https://docs.unity3d.com/6000.3/Documentation/Manual/webgl-canvas-size.html). La reducción se aplica a la resolución interna; la interfaz ocupa el mismo espacio CSS.

También se revisaron problemas de Forward+ en WebGL reportados en [Unity Discussions](https://discussions.unity.com/t/webgl-build-runs-on-such-low-fps-that-it-practically-freezes-the-browser/1560132). El perfil web del proyecto ya usaba Forward y no tenía sombras adicionales, por lo que ese reporte no demuestra la causa de este congelamiento.

### Configuración que ya estaba correcta

Windows ya priorizaba Direct3D 11 sobre Direct3D 12 y el arranque fijaba 1920x1080. Web ya solicitaba GPU de alto rendimiento mediante Power Preference. Se encontraron los símbolos NvOptimusEnablement y AmdPowerXpressRequestHighPerformance en el ejecutable anterior; no se asume su valor solo por encontrar el nombre. No se agregó un detector que cambie opciones basándose únicamente en la marca del equipo o en su VRAM reportada.

Los enemigos tienen geometría relativamente pequeña: 166–2692 triángulos según la variante. El inventario de 14822 renderers de LEVEL 1 incluye objetos inactivos y fragmentos de destrucción; **no equivale a 14822 draw calls por frame**. La medición inicial en Editor batch a 640x480 no representa el render normal del juego y no se usa como benchmark de FPS.

## Validación

- 12/12 pruebas de regresión aprobadas: reutilización de material en 1000 configuraciones de un pickup, liberación de materiales, ciclo de destrucción de edificios, registros de Update/FixedUpdate/LateUpdate y siete pruebas previas de música.
- 384 renders de enemigos en ambas calidades, ocho orientaciones y dos posiciones: cero enemigos invisibles.
- Resultados detallados: Logs/performance-regression-tests.xml, Logs/web-rendering-checks.txt y Logs/performance-asset-audit.txt.
- Windows y WebGL compilados correctamente, cero errores. Builds de prueba: Builds/Compatibility-2026-09-07. Resúmenes: Logs/performance-build-StandaloneWindows64.txt y Logs/performance-build-WebGL.txt. Windows arrancó y escribió el diagnóstico de dispositivo; la prueba por interfaz se interrumpió con Escape antes de validar LEVEL 1 y el panel F8. WebGL todavía necesita una partida en navegador, especialmente en la PX13.

## Prueba necesaria en la PX13

1. Extraer la nueva build completa y ejecutar Windows/manners.exe. La carpeta Windows anterior de este workspace es del 5 de septiembre; no contiene estos cambios. No copiar solamente el .exe.
2. Entrar a LEVEL 1, esperar al menos 15 segundos, pulsar F8 y guardar el reporte. Repetir cuando empeore. El archivo queda en `%USERPROFILE%/AppData/LocalLow/DefaultCompany/manners.exe/performance-report.txt`; Player.log en esa misma carpeta conserva los reportes periódicos de la sesión. Guardar una copia antes de repetir para comparar.
3. Revisar `GPU actually used`. Si aparece Radeon 890M, abrir Configuración de Windows > Sistema > Pantalla > Gráficos, agregar ese ejecutable y elegir Alto rendimiento/NVIDIA. Hacer lo mismo con Chrome, cerrar completamente ambas aplicaciones y volver a probar. La preferencia pertenece a la ruta del ejecutable; una nueva carpeta de build puede necesitar otra entrada.
4. En ProArt Creator Hub, comprobar GPU Mode Standard y modo de funcionamiento Performance con el cargador original. No usar Eco para esta comparación. Si NVIDIA no aparece disponible, resolver primero su detección con MyASUS/ASUS.
5. Para web, abrir `chrome://gpu` y guardar el reporte, particularmente GL_RENDERER, Graphics Feature Status y Problems Detected. Mantener habilitada la aceleración gráfica de Chrome y reiniciarlo después de cambiar GPU o drivers. Si se informa render por software, investigar por qué la aceleración no está disponible.
6. Si la NVIDIA está seleccionada y el bloqueo continúa, revisar las actualizaciones oficiales para **ambos** adaptadores mediante MyASUS/soporte de ASUS, y repetir con el mismo nivel y resolución. El visor de confiabilidad de Windows y los mensajes de contexto WebGL perdido ayudan a distinguir un fallo de controlador de una pestaña ocupada por el juego.

No desactivar la Radeon en el Administrador de dispositivos como primera solución, no modificar el registro ni aplicar ajustes de otros modelos PX13 más recientes. Registrar GPU/API/FPS antes y después es lo que permitirá cerrar el diagnóstico específico.