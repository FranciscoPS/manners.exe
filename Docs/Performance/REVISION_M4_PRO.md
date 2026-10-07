# Rendimiento de manners.exe en MacBook Pro

El código local permitía que el juego de escritorio y la vista Game del editor dibujaran sin límite de FPS. `ResolutionBootstrap` solicitaba 1920×1080, pero no limitaba la frecuencia; `WebGLOptimizer` aplicaba el límite sólo a WebGL. La configuración de calidad tenía VSync desactivado. Esa combinación es una causa comprobada de trabajo innecesario, aunque no permite atribuirle por sí sola toda la temperatura observada en la MacBook M4 Pro.

El incidente ocurrió **en Play Mode del editor, con Game como única vista abierta**. Las correcciones abarcan ese caso y la aplicación compilada. La configuración inicial es **60 FPS en partida, 30 en menú/pausa, 15 sin foco si Unity continúa ejecutándose, salida 1920×1080 a pantalla completa y render 3D limitado a ese presupuesto**. Se conservan los controles gráficos y las preferencias explícitas del jugador.

## Evidencia y alcance

La revisión corresponde a Unity **6000.3.5f2**, URP **17.3.0**, las escenas habilitadas `MainMenu` y `LEVEL 1`, y la integración de `54ecb32` con `0a4dd402`. Los datos nuevos proceden de Unity en Windows, con una **RTX 4070 Ti / Direct3D 11**. Las pruebas funcionales del editor en modo batch no equivalen a medir Metal, consumo eléctrico, temperatura ni una partida sostenida en la M4 Pro.

Se revisaron los puntos de actualización, altas/bajas del pool, navegación y separación, consultas de proximidad, destrucción, materiales y VFX, UI, cámaras, iluminación, terrenos, shaders, preferencias, importación y dependencias de las escenas. El repositorio contiene 161 archivos C#, 109 FBX, 25 shaders y 8 Shader Graphs al cerrar la revisión; esas cantidades incluyen librerías y contenido no utilizado. El inventario nativo resuelve prefabs, variantes y dependencias, incluyendo assets binarios que no se pueden evaluar correctamente sólo buscando texto.

Los informes `ANALISIS_RENDIMIENTO.md`, `VALIDACION.md`, `INVENTARIO_UNITY.json`, `SMOKE_MACOS.json` y `BUILD_MACOS*.json` llegaron con el merge y se conservan como antecedentes. **Sus pruebas Mac y referencias a otro portátil no son mediciones nuevas de la M4 Pro.** La evidencia de esta integración se entrega por separado.

## Diagnóstico priorizado

| Hallazgo comprobado | Consecuencia | Corrección |
|---|---|---|
| Ningún límite de escritorio en la rama local | Render continuo hasta saturar CPU/GPU, incluso con frames baratos | Una autoridad gráfica inicializada antes de cargar escenas |
| VSync del código entrante usa siempre 1 | Al activarlo puede subir a 120 FPS y Unity ignora el límite numérico | Divisor compatible con el presupuesto, con alternativa por software |
| Altura 1080 del código entrante conserva aspecto nativo | No produce 1920×1080 en pantallas 16:10 | Resoluciones 16:9 y escalado de pantalla completa |
| Cambiar tamaño de Game no estaba cubierto por la resolución de ventana | Una vista grande/Retina puede pedir demasiados píxeles | Límite adicional en la escala real de URP, también en Play Mode |
| Correcciones duplicadas de materiales y actualizaciones durante el merge | Métodos duplicados, registros inconsistentes y código que no compila | Integración por comportamiento y pruebas nativas |
| Enemigos NavMesh registrados también en FixedUpdate | Visitas de física a enemigos que no utilizan ese callback | Registro sólo para movimiento Rigidbody o knockback |
| Enemigos, pickups y transparencias procesados con tiempo detenido | Consultas y escrituras de transformaciones sin avance del juego | Salida inmediata durante pausa |
| Un material transparente por pieza de edificio | Multiplicación de instancias y escrituras del mismo alpha | Un material por material fuente y edificio |
| Cachés estáticas de flipbooks pierden materiales al cambiar textura; fallback sin liberar | Retención de recursos y crecimiento en ciertas configuraciones | Material de asset compartido en el caso normal; propiedad y liberación explícitas en overrides |
| Monitor siempre creado en release en la versión local | Trabajo diagnóstico permanente | Arranque automático sólo en editor/development; release optativo |

## FPS, Retina y pantalla completa

Unity documenta que, en escritorio, `targetFrameRate = -1` con `vSyncCount = 0` dibuja tan rápido como puede. Con VSync activo, se ignora `targetFrameRate`. Un monitor de 120 Hz no impone un límite de 60 por activar VSync una vez. El límite por software puede introducir pequeñas irregularidades de presentación; se mantiene como opción inicial porque expresa directamente el presupuesto solicitado. [1][2]

Si se activa VSync, el servicio calcula `ceil(refresco / presupuesto)`, dentro de los divisores 1–4 admitidos. A 120 Hz y 60 FPS utiliza 2; a 144 Hz utiliza 3, por lo que presenta a 48 FPS. En una pantalla demasiado rápida para esos divisores se vuelve al límite por software. La UI explica que la sincronización puede elegir una frecuencia menor y permite conservar el control de FPS. El refresco se revisa cada medio segundo para cambios de pantalla; no hay una búsqueda global de objetos en esa revisión.

La aplicación solicita `FullScreenWindow` y 1920×1080. Unity documenta que esta modalidad cubre la pantalla nativa, escala la imagen y añade bandas cuando es necesario para conservar su aspecto. En macOS no se depende de pantalla completa exclusiva para cambiar el refresco. Si el monitor es más pequeño, se calcula una resolución que quepa; “Nativa” sigue disponible como selección explícita. [3][4]

**La vista Game del editor no es una ventana de la aplicación compilada.** No se fuerza el tamaño de los paneles personales del editor. Se limita el mundo 3D mediante `renderScale`: toma el menor factor necesario para que ambos ejes quepan en 1920×1080, multiplicado por la escala elegida. Una vista 3840×2160 usa como máximo 0,5; una 1920×1080 usa 1. La UI overlay mantiene su resolución de salida. En pantallas con otra proporción, el 3D conserva el aspecto de Game y cabe dentro del presupuesto; no se estira.

Un target 4K tiene cuatro veces los píxeles de Full HD. A 120 FPS frente a 60, la cantidad de píxeles solicitada por segundo se multiplica otra vez por dos. Esto es aritmética de los targets, **no una predicción de ocho veces más consumo ni una medición del equipo afectado**. Si el cuello de botella está en CPU o navegación, reducir resolución tendrá un efecto menor.

Las preferencias se mantienen en `GraphicsSettings.v1`. Un jugador que ya haya elegido 120 FPS o resolución nativa conserva su elección. **Restablecer** recupera los valores iniciales. Los cambios de resolución de la aplicación se confirman durante 15 segundos reales y se revierten si no se aceptan; la escala del editor no depende de ese diálogo.

## Renderizado, geometría y texturas

El juego no tiene geometría compleja en cada carta/enemigo, pero sí un entorno con miles de piezas. El inventario previo a ejecutar `Awake` encontró:

| Métrica de contenido | MainMenu | LEVEL 1 |
|---|---:|---:|
| GameObjects | 13 407 | 18 225 |
| Renderers habilitados y activos antes de Awake | 11 024 | 14 814 |
| Rigidbody, incluidos escombros que se desactivan | 6 524 | 9 938 |
| Renderers con flag BatchingStatic | 64 | 53 |
| Renderers con lightmap | 0 | 0 |
| LOD Groups | 0 | 0 |

**No son draw calls ni miles de cuerpos simulándose simultáneamente.** Después de arrancar, la prueba funcional confirmó 4 500 renderers habilitados en MainMenu y aproximadamente 4 880 en el nivel inicial. MainMenu tenía cero Rigidbody activos; el nivel inicial tenía dos en esa instantánea. Los escombros inactivos explican buena parte de la diferencia. El culling de las cámaras todavía puede descartar muchos de esos renderers.

SRP Batcher ya estaba activado. Reduce preparación de estados de dibujo, pero no combina automáticamente miles de meshes en un solo draw. Por eso contar polígonos de una carta no representa el coste total de la escena. El uso de materiales compartidos se conserva, y las transparencias ahora reutilizan una instancia por fuente dentro de cada edificio; no se sustituyó masivamente todo por MaterialPropertyBlock, que tiene sus propias implicaciones para SRP Batcher. [5]

El inventario contiene **139 texturas** en las dependencias de build/Resources y aproximadamente **415 MiB de memoria residente de assets importados del editor** en esta ejecución. Esto no es VRAM medida del player ni prueba de una fuga. Hay texturas de edificios, vehículos y terreno de 2048×2048; varias ya están comprimidas y tienen mipmaps. Algunos sprites UI sin compresión ocupan más que lo que su PNG sugiere. No se encontró una razón para degradar indiscriminadamente todos los gráficos a 512 px. Eco permite reducir mipmaps globales donde se admiten.

El terreno mide 500×500, con heightmap 513, ocho capas y dos alphamaps. No es una superficie gratuita: las capas y sus materiales implican trabajo de sombreado. Tiene instancing activado, cero árboles, un prototipo de detalle, error de píxel 5 y distancia de detalle 80. El siguiente análisis visual debe determinar qué capas son necesarias antes de eliminar o fusionar texturas; hacerlo sin observar el nivel puede alterar suelo, agua o transiciones.

La configuración integrada usa Forward. Equilibrado/Eco seleccionan `PC_NoAO_Renderer`, sin SSAO; Alto puede utilizar el renderer con SSAO, cuyo downsample se conserva. La cámara del minimapa usa su propio renderer sin efectos, sombras, HDR ni copias de color/profundidad. La Opaque Texture global queda apagada. **Depth Texture se conserva porque el agua utiliza SceneDepth.** HDR y el postprocesado visual existente se mantienen cuando el perfil lo permite. Unity recomienda medir estas opciones y conservar las dependencias de los shaders. [6][7]

La luz principal sigue siendo dinámica: no hay resultados de lightmaps asignados en las escenas publicadas. Equilibrado limita sombras a 20 m y una cascada; Eco puede apagarlas. La shadow map principal de PC ya era de 256 px, no 4K. Hornear todo sin separar edificios destructibles de entorno permanente dejaría iluminación o sombras incoherentes al destruir objetos. No se atribuyó a un bake inexistente una optimización que no ocurrió.

## CPU, actualizaciones y arquitectura

El gestor central de actualizaciones es útil, pero no reduce por sí solo la cantidad de trabajo: sustituye llamadas nativas individuales por una lista de callbacks administrados. El merge se resolvió conservando conjuntos de pertenencia y listas sincronizados, detección de objetos Unity destruidos y orden final de altas/bajas dentro del mismo tick. Las pruebas cubren Update, FixedUpdate y LateUpdate, registros repetidos y reutilización del pool.

Los enemigos de NavMesh continúan moviéndose y girando a la frecuencia visual, con actualización de ruta escalonada cada 0,2 s. El callback fijo se registra para knockback y se retira al terminar; el movimiento puramente Rigidbody lo mantiene. La barra de salud ahora escribe su tamaño cuando cambia la vida. En pausa, los sistemas de mundo identificados evitan sus consultas y escrituras; la UI y los temporizadores de confirmación siguen usando tiempo real.

Scatter utiliza el grid espacial, reutiliza buffers, mantiene el orden original de candidatos y reparte la exploración en grupos de 16. También conserva las pausas entre grupos de teletransportes, comprueba objetos desactivados y evita elegir puntos demasiado cercanos al jugador después de ceder un frame. Se mantuvo la precarga inicial gradual de la oleada final, sin reservar de golpe el techo de 2 000 instancias del extremo final.

No se cambió la dificultad para aparentar una mejora de rendimiento. El límite normal depende de la configuración serializada del nivel; la oleada final puede crecer hasta 2 000. Separación ya usa grid con máximo de vecinos, y varias consultas de combate ya usan buffers NonAlloc. También persisten costes reales en navegación, física, explosiones en área y cientos de iconos UI cuando crecen las hordas. La prueba de estrés describe su escenario concreto; no demuestra capacidad estable a 2 000 enemigos en una Mac.

Pooling evita muchas creaciones, pero no vuelve gratuitos los objetos activos. El pool puede crecer y los flipbooks todavía crean su GameObject/ParticleSystem al dispararse; no es correcto afirmar que todo el juego elimina Instantiate/Destroy. Los dos flipbooks revisados emiten una sola partícula cada uno: aquí el área transparente dibujada y el número simultáneo de efectos importan más que la cifra máxima genérica de partículas. Sus materiales ahora tienen una vida útil verificable.

## Investigación de Unity y Apple

El foro de Unity contiene un reporte directo de 2022 sobre picos en Metal sin VSync en M1. Ayuda a justificar comprobar presentación y frame pacing, pero usa otra generación y Unity 2022.1; no demuestra que exista el mismo fallo en M4 Pro con Unity 6000.3.5f2. Tampoco justifica aplicar argumentos de arranque para forzar renderizado monohilo ni cambiar de versión del motor a ciegas. [8]

Unity explica que Play Mode comparte recursos con el editor. Maximizar Game y cerrar otras vistas ayuda a reducir esa interferencia, pero no convierte el proceso en un player de release. El número de FPS, por sí solo, tampoco identifica si se espera a la GPU, a presentación o al limitador. [9]

Apple recomienda analizar el trabajo de CPU y GPU con Metal System Trace/Instruments y capturas de GPU. Su plantilla Game Performance permite correlacionar tiempos y estado térmico. Esa es la evidencia necesaria para distinguir una carga excesiva del juego, el coste del editor y una caída sostenida asociada a límites térmicos. No hay acceso a ese equipo desde esta revisión. [10]

## Comprobaciones y reproducción

Los resultados finales de compilación y pruebas están en [RESULTADOS_REVISION.md](RESULTADOS_REVISION.md). La suite incluye el presupuesto de pantallas 60/120/144/165/240/360 Hz, tamaños Retina/4K/8K, preferencias corruptas, propiedad de materiales, pooling, updates, carga de escenas, pausa y una horda forzada. Los contadores de frames de un editor batch son una comprobación funcional de la política de frecuencia; no se publican como benchmark de la M4 Pro.

Para comprobar el caso original en la Mac:

1. Entrar en Play Mode con Game en 1920×1080, escala de visualización cómoda y perfil Equilibrado restablecido. Revisar con F8 el límite y la resolución efectiva; el modo Game puede mostrar un tamaño distinto si su panel lo impone.
2. Usar **Tools → Manners → Performance → Start CSV Capture (Play Mode)**. Se guardan archivos locales en la carpeta `Performance` de `Application.persistentDataPath`; el menú permite detener la captura y abrir la carpeta. No hay captura continua por defecto ni envío de datos.
3. Comparar menú, inicio de partida, horda, pausa y juego sin foco. Repetir con la misma escena y duración. Anotar si está conectado a corriente y el modo de energía, sin cambiar esas condiciones entre pasadas.
4. Medir después una build macOS de release equivalente, idealmente durante 15–20 minutos. La herramienta existente **Build macOS Release for Performance Test** habilita Frame Timing Stats temporalmente para esa build. Requiere instalar el módulo Mac en Unity.
5. Para diagnóstico detallado, correlacionar el CSV con CPU/Rendering Profiler y Metal System Trace. Los campos CPU/GPU vacíos significan que la plataforma no suministró la medición; no equivalen a cero milisegundos. `cpu_frame_ms_including_wait` incluye esperas.

Una build puede arrancarse con `-manners-perf-capture` para activar el mismo registro. El perfil Eco ofrece 30 FPS, menor escala, menos detalle de texturas y sombras/postprocesado desactivados como control A/B. Las preferencias de gráficos se pueden restaurar desde la UI sin borrar progreso.

Para una siguiente optimización de contenido, la mayor oportunidad identificada es **reducir piezas de renderizado por edificio/poste y revisar las ocho capas del terreno**, guiándose por Frame Debugger y tiempos de GPU. Se debe conservar destrucción, fades y culling por objeto. No se hizo una combinación global de la isla que comprometiera esas funciones.

## Fuentes

Consultadas para Unity 6.3 cuando existe documentación versionada. Los reportes del foro se tratan como evidencia de su propio caso, no como diagnóstico de esta máquina.

1. Unity Technologies. [Application.targetFrameRate](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Application-targetFrameRate.html).
2. Unity Technologies. [QualitySettings.vSyncCount](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/QualitySettings-vSyncCount.html).
3. Unity Technologies. [FullScreenMode.FullScreenWindow](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/FullScreenMode.FullScreenWindow.html).
4. Unity Technologies. [Screen.SetResolution](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Screen.SetResolution.html).
5. Unity Technologies. [Scriptable Render Pipeline Batcher](https://docs.unity3d.com/6000.3/Documentation/Manual/SRPBatcher.html).
6. Unity Technologies. [Configure for better performance in URP](https://docs.unity3d.com/6000.3/Documentation/Manual/urp/configure-for-better-performance.html).
7. Unity Technologies. [Screen space ambient occlusion](https://docs.unity3d.com/6000.3/Documentation/Manual/urp/post-processing-ssao.html).
8. minDog, Unity Discussions, julio de 2022. [Mac M1 Metal spikes in profiler without vsync](https://discussions.unity.com/t/mac-m1-metal-spikes-in-profiler-without-vsync/887946).
9. Unity Technologies. [Collect performance data in Play mode](https://docs.unity3d.com/6000.3/Documentation/Manual/profiling-play-mode.html).
10. Apple Developer. [Analyzing the performance of your Metal app](https://developer.apple.com/documentation/xcode/analyzing-the-performance-of-your-metal-app).
