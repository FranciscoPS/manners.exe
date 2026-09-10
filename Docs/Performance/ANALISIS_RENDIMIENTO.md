# Rendimiento y consumo de manners.exe

El proyecto tiene varias optimizaciones correctas, pero también trabajo innecesario y errores que esas optimizaciones no cubrían. Los hallazgos principales son la ausencia de un límite de FPS en escritorio, un script que anulaba la reducción de resolución interna, iluminación dinámica en la escena publicada, un renderizador con efectos y pasadas costosas, una fuga de materiales al reutilizar orbes y búsquedas entre todos los enemigos. El tamaño de descarga y el bajo número de polígonos no permiten deducir el coste de ejecutar el juego.

La revisión toma como referencia el commit `1bcbed6`, Unity **6000.3.5f2**, URP **17.3.0** y las escenas habilitadas **MainMenu** y **LEVEL 1**. Se inspeccionaron código, escenas, prefabs, configuración de importación, shaders, modelos, terreno y recursos. Los resultados de ejecución y sus límites están en [VALIDACION.md](VALIDACION.md). Este informe distingue propiedades comprobadas del proyecto de hipótesis que requieren un perfil de CPU/GPU; no contiene una medición de temperatura de la ASUS PX13.

## 1. Diagnóstico principal y prioridades

| Prioridad | Hallazgo en la versión inicial | Acción aplicada o pendiente |
|---|---|---|
| Crítica | Escritorio: VSync desactivado y ningún `Application.targetFrameRate` aplicado | Servicio común con 60 FPS por defecto, 30 en menú/pausa y 15 sin foco cuando el sistema sigue ejecutando |
| Alta | `WebGLOptimizer` fuerza render scale 1 cada dos segundos, incluso en escritorio | Eliminado el watchdog; la configuración guardada tiene una única autoridad |
| Alta | Cada reutilización de orb crea un Material y pierde el anterior | Un material por objeto del pool, reutilizado y liberado al destruirlo |
| Alta | Scatter compara todos los enemigos entre sí cada cuatro segundos | Consultas al grid espacial y buffers reutilizados, preservando el orden de candidatos |
| Alta | Sol Realtime, sin resultados de bake asignados | Opciones de sombras y menor presupuesto; conservar luz dinámica hasta diseñar un bake compatible con destrucción |
| Alta | Forward+, SSAO completo y pasadas adicionales para una escena con pocas luces | Renderer Forward sin SSAO para Equilibrado/Eco; renderer original accesible al activar SSAO, con AO a media resolución por eje |
| Media | Cámara de minimapa e iconos actualizados cada frame | Renderer propio sin efectos y frecuencia máxima de 20 Hz; congelado en pausa/oculto |
| Media | Muchos renderers para muy pocos triángulos por edificio/poste | Inventario y siguiente fase de combinación de mallas por objeto/material, condicionada a Frame Debugger |
| Media | Monitor de FPS busca objetos globalmente durante las caídas | Contadores existentes, promedio correcto y desactivado por defecto en release |
| Media | Listas y sets de UpdateManager pierden sincronía con altas/bajas pendientes | Corregidas las tres fases de actualización y el reciclaje dentro del mismo tick |
| Media | Pool de texto flotante presta incorrectamente al agotarse | Corregido préstamo/activación; eliminado raycaster de texto decorativo |
| Por medir | Física, VFX, destrucción y drops de hordas tardías | Prueba sostenida con hasta 450 enemigos y escenario overtime de hasta 2000; no se alteró la dificultad |

No se ha activado indiscriminadamente todo lo que Unity llama “optimización”. Ya estaban habilitados SRP Batcher, RenderGraph y native render passes; MSAA ya estaba desactivado. Cambiar estas opciones sin comprobar el estado real no habría resuelto los errores anteriores.

## 2. Por qué un juego pequeño puede calentar tanto

Una GPU no sabe que un juego es indie. Ejecuta el trabajo solicitado por cada frame, tantas veces por segundo como permita el motor. Un frame relativamente barato, repetido cientos de veces por segundo sin límite, puede ocupar la GPU de forma sostenida. Si optimizamos ese frame pero permitimos dibujar aún más frames, el consumo puede seguir siendo alto. Por eso el presupuesto de FPS precede a la reducción de calidad.

En Unity de escritorio, `vSyncCount = 0` y `targetFrameRate = -1` significan renderizar tan rápido como sea posible. Con VSync activo, Unity ignora el objetivo de FPS y sincroniza con el monitor. VSync suele ofrecer mejor regularidad de presentación que el limitador por software, pero un monitor de 120/144 Hz no establece por sí mismo un presupuesto de 60. La nueva UI explica esta interacción y mantiene el límite numérico como opción predeterminada. [1]

También hay que separar **calor normal**, **consumo evitable** y **thermal throttling**. Un ventilador que acelera no demuestra por sí solo sobrecalentamiento perjudicial. Apple describe el aumento de temperatura y ventilación durante cargas de trabajo como parte del funcionamiento normal. Una pérdida progresiva de rendimiento sí merece correlacionarse con temperaturas, frecuencias, potencia y tiempos de frame; sin esa correlación puede confundirse una horda más grande con una caída térmica. [2]

La denominación “RTX 40xx Laptop” tampoco fija un rendimiento sostenido. NVIDIA identifica diferencias de potencia, CPU, memoria, diseño térmico y perfiles del fabricante entre portátiles. ASUS publica para variantes PX13 HN7306 una pantalla 2880×1800 y distintas GPU RTX; esto no identifica la variante exacta del equipo afectado. No se presupone un TGP, temperatura límite o GPU concreta. [3][4]

Más RAM ayuda a que los recursos quepan; no acelera automáticamente la física, las llamadas de dibujo o los shaders. Un PNG ocupa pocos bytes en distribución, pero se carga en un formato distinto para la GPU. Un modelo de 1000 triángulos repartido en veinte objetos sigue requiriendo gestionar esos objetos. El pooling evita parte de crear/destruir objetos, pero no evita actualizar, simular, dibujar y sombrear los que están activos.

## 3. Resolución y política de gráficos

### Estado inicial

`ResolutionBootstrap.cs` ya intentaba forzar **1920×1080** fuera de WebGL. Player Settings también tenía 1920×1080 y modo de ventana a pantalla completa. Por tanto, **no es correcto afirmar que el problema se debe necesariamente a renderizar a resolución Retina/3K nativa**. Hay que leer la resolución efectiva del player y su render target. El editor y una ventana de juego escalada no prueban cómo se comporta una build.

El problema comprobado estaba en `WebGLOptimizer.Awake`: arrancaba el watchdog de render scale sin condicionar ese watchdog a WebGL. Cada dos segundos devolvía la escala a 1. Además, varias de sus “optimizaciones” modificaban `QualitySettings` del renderizado tradicional y dejaban vacíos los bloques destinados al asset URP. El nombre del script y sus casillas transmitían una protección que no correspondía a su comportamiento.

### Configuración implementada

`GameGraphicsSettings` se inicializa antes de cargar la primera escena. Lee y valida una configuración persistente, clona el asset URP para aplicar ajustes durante la ejecución y restaura el asset original al salir de Play Mode. Los renderers fuente y sus features no se modifican en runtime. `ResolutionBootstrap` y `WebGLOptimizer` delegan en este servicio; ya no compiten por la escala.

| Ajuste inicial | Eco | Equilibrado | Alto |
|---|---:|---:|---:|
| Objetivo de juego | 30 FPS | 60 FPS | 60 FPS |
| VSync | No | No | No |
| Escala 3D | 75 % | 100 % | 100 % |
| Mipmaps globales | Mitad por eje, donde se admiten | Completos | Completos |
| Sombras | No | Sí, hasta 20 m, una cascada | Sí, hasta 28 m y cascadas del asset fuente |
| SSAO | No | No | Sí, si la plataforma tiene renderer con SSAO |
| Postprocesado | No | Sí | Sí |
| Altura de salida predeterminada | 1080, limitada por pantalla | 1080, limitada por pantalla | 1080, limitada por pantalla |

Los perfiles se pueden personalizar. Están disponibles 30/45/60/90/120 FPS, VSync, altura de pantalla 720/900/1080/1440/2160/nativa, ventana/pantalla completa, escala 3D 50/60/67/75/85/100 %, detalle de texturas, sombras, SSAO y postprocesado. La selección de perfil conserva la resolución y el modo elegidos; “Restablecer” vuelve a los valores recomendados.

La resolución conserva la relación de aspecto: en una pantalla 2880×1800, solicitar 1080 de altura produce 1728×1080, no una imagen estirada a 16:9. La resolución nunca supera la pantalla por accidente. El tamaño de ventana y la resolución del sistema no son conceptos idénticos, especialmente en pantalla completa sin bordes. El tamaño efectivo se registra en las capturas de rendimiento.

Los cambios de pantalla se prueban durante **15 segundos reales**, incluso si el juego está pausado. Sólo la confirmación guarda la nueva pantalla. Si vence el plazo, se cierra el panel o se elige Revertir, se restaura la anterior. Una salida inesperada durante la prueba conserva la última pantalla confirmada en preferencias. En editor y WebGL, el control de pantalla está deshabilitado; la escala interna sigue siendo utilizable.

Reducir ambos ejes de renderizado al 75 % produce **56,25 % de los píxeles 3D**; al 67 %, **44,89 %**. Es aritmética del target, no una promesa de ganar ese porcentaje de FPS. Si domina CPU/física, la mejora puede ser pequeña. La UI de tipo overlay conserva su resolución de salida. [5]

No se añadió resolución dinámica automática: primero se necesitan mediciones fiables para definir umbrales, histéresis y calidad mínima. Eliminar el watchdog y dar controles persistentes ya permite una reducción predecible sin que el juego la revierta silenciosamente.

## 4. Iluminación, sombras y bake

En ambas escenas de build, `m_LightingDataAsset` apunta al recurso integrado con GUID `0000000000000000f000000000000000`, y `m_LightingSettings` es nulo. No se encontraron resultados generados `Lightmap*`/`LightingData*` correspondientes al nivel. `Assets/Scenes/Map2Light.lighting` es una configuración de iluminación, pero no está asignada a estas escenas.

En `LEVEL 1`, `Directional Light` tiene `m_Lightmapping: 4`, equivalente a **Realtime**, con sombras suaves. MainMenu también tiene sol Realtime. La conclusión no depende de que exista o no un script que afirme “optimizar luces”: la escena publicada contiene una luz dinámica y carece de los resultados de horneado esperados.

`LevelOptimizationTools` hornea **occlusion culling**, no iluminación. Sus flags incluyen batching, occluder/occludee y reflexión, pero no `ContributeGI`. Es probable que aquí se haya confundido un bake de visibilidad con un bake de luz. Son datos y costes completamente diferentes.

La textura de sombras principal de PC ya era muy pequeña, **256 px**; su distancia era 28 m y tenía dos cascadas. No se atribuye el problema a una supuesta shadow map 4K inexistente. Sin embargo, dibujar los casters, generar cascadas y filtrar sombras sigue costando. Mobile tenía un presupuesto distinto: 1024 px y 50 m. La nueva configuración limita la distancia en ambos y permite apagar sombras sin apagar el sol.

No se apagó la iluminación dinámica ni se marcó todo como Baked: eso cambiaría el aspecto sin disponer de lightmaps válidos. Además, los edificios destruidos o transparentes pueden dejar sombras horneadas que ya no corresponden al mundo. Para una segunda fase de iluminación:

1. Separar entorno estable de edificios destructibles y elementos móviles.
2. Revisar UV2/unwrap real: `generateSecondaryUV: 0` sólo dice que Unity no lo genera; un FBX podría incluirlo del artista.
3. Hornear el entorno estable y preparar probes para objetos móviles.
4. Diseñar deliberadamente qué sombras permanecen dinámicas, mixtas o aproximadas.
5. Validar destrucción, transparencia, personaje, costa y cambios de cámara.

Esta fase es de contenido y dirección visual, no una casilla que pueda corregirse responsablemente sin verificar su resultado. Las optimizaciones de esta entrega preservan una luz principal funcional.

## 5. URP, cámaras, SSAO y postprocesado

PC usaba **Forward+**, mientras que Mobile usaba Forward. Forward+ puede resultar útil con muchas luces y otros requisitos; no es automáticamente más eficiente para una escena con un sol. Unity recomienda elegir el render path según el contenido y medirlo. Se añadió un renderer Forward sin SSAO para la ruta económica, conservando la ruta original como alternativa al activar oclusión ambiental. [6]

El SSAO original estaba activo a resolución completa, ocho muestras y blur bilateral de calidad alta. Es una Renderer Feature: `cameraData.renderPostProcessing = false` **no** significa que el SSAO esté apagado. Ahora Equilibrado/Eco usan el renderer sin esa feature, y la ruta con SSAO usa downsample. Reducir a mitad cada eje del AO reduce a un cuarto sus píxeles base; quedan costes de reconstrucción, blur y otras pasadas, así que no se extrapola a una mejora de cuatro veces del juego. [7]

La Opaque Texture estaba activada globalmente en PC. La inspección de shaders y subgraphs no encontró muestreo de Scene Color que la necesitara, por lo que se desactivó esa copia. **Depth Texture se conserva**: el agua usa `SceneDepthNode` en `DepthFade.shadersubgraph`. Se habilitó también la profundidad necesaria en Mobile, cuyo asset previo no la solicitaba; es una corrección de la dependencia visual, no un ahorro. El minimapa la desactiva explícitamente porque no dibuja esa agua.

HDR se conserva para los efectos emisivos y bloom existentes. La cámara principal usa el volumen `SampleSceneProfile`: bloom, viñeta, aberración cromática y tonemapping; Motion Blur está inactivo. Una intensidad de bloom 34 no representa “34 veces más procesamiento”: importan las pasadas y su resolución. Tampoco se contaron como activos todos los componentes del volumen global sólo porque su campo `active` sea verdadero: intensidad cero o modo None pueden hacerlos inoperantes.

El minimapa usa una RenderTexture **256×256**, por lo que no equivalía a dibujar otra pantalla 1080p completa. Sí repetía culling, preparación del renderer y actualización de iconos en cada frame. Ahora selecciona `Minimap_Renderer`, sin SSAO, postprocesado, sombras, HDR, MSAA ni copias de color/profundidad. Actualiza como máximo a 20 Hz, conserva la textura entre capturas y deja de dibujar pausado/oculto. La representación del minimapa se actualiza con menor frecuencia; el movimiento y daño del juego conservan sus frecuencias originales.

El menú también contiene una isla 3D y una cámara orbital; no es una pantalla estática gratuita. Su límite propio de 30 FPS reduce trabajo incluso antes de empezar una partida. Reemplazar la isla por un fondo prerenderizado sería una opción futura de mayor ahorro, con un cambio visual explícito.

## 6. Geometría, materiales y culling

### Pocos triángulos, muchas piezas

El análisis de FBX confirma que los edificios son ligeros. Las cifras siguientes corresponden a geometría fuente, no a draw calls visibles ni a triángulos multiplicados por todas las pasadas:

| Modelo | Triángulos fuente | Mallas fuente |
|---|---:|---:|
| Edificio2 | 1500 | 18 |
| Edificio4Fix | 1264 | 24 |
| Edificio3 | 716 | 17 |
| Edificio5Fix | 844 | 8 |
| Edificio1 (1) | 804 | 1 |
| POSTE_L | 288 | 4 |
| MilitaryHumvee | 832 | 5 |
| APC2 | 425 | 5 |
| MannersShop | 4722 | 1 |

Por ejemplo, LEVEL 1 serializa 193 instancias directas de Prop4, cuyo poste fuente tiene cuatro mallas. Eso sugiere hasta 772 renderers fuente antes de resolver variantes, culling y batching, pese a sumar sólo 55 584 triángulos fuente. Simplificar otros veinte triángulos por poste probablemente sea menos útil que reducir las piezas/materiales despachados.

`Island.prefab` contiene 340 instancias anidadas de edificios. LEVEL 1 añade prefabs y elimina algunos objetos heredados mediante overrides. No se sumaron ciegamente todos los prefabs como si fueran objetos activos distintos. El inventario nativo de Unity resuelve la jerarquía y se entrega separado de las estimaciones fuente. En preview (antes de ejecutar Awake/Start), MainMenu contiene 13 407 GameObjects y 11 024 renderers habilitados; LEVEL 1 contiene 18 225 GameObjects y 14 814 renderers habilitados. Parte pertenece a escombros que el arranque desactiva: estos números NO son draw calls ni el conteo activo definitivo del player. Ambos tienen cero renderers lightmapped. El cierre de dependencias nativo resolvió 139 texturas con unos 331,13 MiB de memoria de assets importados en el editor, no VRAM medida en la build. Ver [inventario completo](INVENTARIO_UNITY.json). Esta cifra corresponde a la última instantánea del Editor; otra lectura anterior fue de 369,52 MiB. Las cachés y los assets dinámicos cargados cambian entre lecturas, por lo que esa diferencia no se atribuye a una mejora de memoria del juego.

El modelo `Mecha-V@Capoeira` contiene más geometría, pero una referencia a su AnimationClip no demuestra que su malla se esté dibujando. Tampoco se sumaron los FBX de idle/walk/death del personaje como tres personajes renderizados simultáneamente.

### Recuento comprobado en la build Mac

La prueba funcional se ejecutó también en el player macOS, después de que el arranque desactivara los escombros iniciales. Se obtuvo el siguiente estado al inicio de las escenas:

| Métrica de ejecución inicial | MainMenu | LEVEL 1 |
|---|---:|---:|
| Renderers activos y habilitados | 4 500 | 4 878 |
| Activos configurados para proyectar sombras | 4 499 | 4 877 |
| Materiales únicos activos | 14 | 31 |
| Suma de triángulos de mallas activas | 374 016 | 430 117 |
| Texturas de lightmap asignadas | 0 | 0 |

**Son recuentos antes del culling por cámara y de LOD, no draw calls ni objetos necesariamente visibles.** La suma de triángulos excluye terreno, partículas y pasadas adicionales. Tampoco describe la horda tardía. La diferencia frente al inventario de preview confirma por qué no deben presentarse los 14 814 renderers habilitados antes de Awake como coste activo de la partida. Aun después del arranque permanecen miles de renderers: agrupar las piezas por objeto/material sigue siendo una hipótesis útil para perfilar. Evidencia completa: [SMOKE_MACOS.json](SMOKE_MACOS.json).

### Materiales y batching

El repositorio contiene 152 materiales externos, incluyendo librerías y contenido no necesariamente cargado. Muchos tienen GPU Instancing marcado; eso no une automáticamente meshes diferentes, no elimina las llamadas de dibujo y no garantiza que la ruta SRP Batcher los trate igual. El SRP Batcher reduce preparación de draws compatibles en CPU, pero no vuelve gratuitos todos los renderers. [8]

`BuildingFader` conserva sharedMaterials y crea variantes transparentes cuando se necesitan, no cada frame. Eso está mejor que clonar permanentemente. Aun así, transparencia, blend y ZWrite desactivado aumentan overdraw; ver el edificio translúcido puede costar más que verlo opaco. Las instancias de materiales reducen algunas posibilidades de agrupación.

No se reemplazó todo por MaterialPropertyBlock: en URP puede sacar un renderer de la ruta SRP Batcher. La fuga demostrada de orbes se corrigió manteniendo un material propio por objeto del pool. Combinar materiales o usar instancing requiere validar los shaders concretos.

La siguiente mejora de assets con mejor fundamento es combinar **las piezas del edificio intacto que comparten material**, manteniendo el visual de destrucción separado. No conviene unir toda la isla en una única malla: se pierde granularidad de visibilidad y destrucción. Primero medir `Draw Calls`, `SetPass`, CPU de render y profundidad/sombras en Frame Debugger.

### Culling y LOD

LEVEL 1 sí referencia un OcclusionCullingData real de unos 984 KiB. MainMenu no. Tener un archivo de occlusion demuestra que existe un bake, no que esté actualizado o ahorre lo suficiente en la cámara de juego. Deben revisarse Visualization y los draws antes/después de destruir o transparentar edificios.

Frustum/occlusion/LOD eliminan dibujo; no eliminan automáticamente física, IA, timers, animaciones, audio y callbacks centralizados. Un Rigidbody fuera de cámara puede seguir simulándose. La cámara del minimapa también participa en visibilidad.

Los cuatro LODGroup encontrados en enemigos tienen un solo nivel y corte final. Eso es conservar la malla completa hasta ocultarla, no reducir gradualmente su complejidad. No se encontraron LODGroups de edificios. `lodBias = 2` en PC prolonga la visibilidad respecto a 1, pero reducirlo puede hacer desaparecer enemigos y no se cambió indiscriminadamente.

La herramienta de optimización excluye edificios destructibles/faders de static flags por motivos válidos. Un edificio que puede desaparecer no debe hornearse sin más como oclusor permanente, porque podría ocultar objetos detrás de un espacio que después queda vacío. [9]

## 7. Texturas, memoria, terreno y efectos

Se inspeccionaron **732 TextureImporter**. De ellos, 567 tienen mipmaps y 165 no, principalmente UI/sprites; 731 no son readable. El único readable identificado es el cursor. Casi todos declaran compresión normal; el uncompressed explícito pertenece a un icono de TutorialInfo. El límite de importación 2048 no significa que una fuente 32×32 se convierta en 2048×2048.

Las 66 texturas de `Materials/Map` incluyen 57 fuentes 1024² y nueve 2048², todas con mipmaps, compresión y sin Read/Write. No hay evidencia de que todo el mapa esté lleno de texturas sin comprimir de tamaño enorme. El cierre inicial de referencias YAML era parcial; la validación nativa incluye dependencias de TerrainData, Resources y otros assets que no se pueden resolver leyendo sólo YAML.

Una textura 1024² en BC1 con todos sus mipmaps requiere aproximadamente 0,667 MiB; en BC3/BC5, 1,333 MiB. A 2048² son cuatro veces esas cantidades. Son ejemplos del formato, no mediciones de la memoria residente del juego. Crunch afecta principalmente distribución; no mantiene el formato de distribución dentro de la GPU. La memoria reportada por un asset en el editor tampoco equivale automáticamente a VRAM de la build.

Se ofrece mitad/cuarto de mipmaps como elección reversible para texturas elegibles. No se bajaron todos los importers a 512 ni se degradaron UI y fuentes. Streaming está apagado tanto globalmente como en importers; encender sólo la casilla global no incorpora automáticamente todas las texturas. Implementarlo requiere elegir texturas elegibles, presupuesto de memoria y probar recorridos/cambios de cámara para evitar cargas tardías. [10]

Algunas máscaras tienen sRGB y algunos archivos con “Normal” en su nombre no están importados como NormalMap. Son posibles errores de interpretación visual que se deben revisar contra su uso real; no son una explicación térmica demostrada y no se alteró la dirección de arte sin comparación.

El TerrainData inspeccionado tiene heightmap 513, tamaño aproximado 500×500, ocho TerrainLayers y dos alphamaps 512. No contiene árboles instanciados. Por tanto los valores altos de treeDistance no prueban un coste real de miles de árboles. Ocho capas de Terrain Lit pueden necesitar trabajo/pasadas adicionales donde un patch usa más de cuatro; eso merece inspección en Frame Debugger. El terreno tiene huecos, por lo que no se desactivó su soporte indiscriminadamente.

El agua y las áreas de habilidades sí pueden ser costosas por píxel aunque tengan pocos vértices. CryoField/CryoDome usan ruido procedural, Voronoi con vecindarios 3×3, mezclas y transparencias. Láser y EMP añaden otros efectos de pantalla. Una prueba decisiva consiste en mantener cámara/horda y comparar con y sin criogenia, y luego escala 100/75 %. Si el tiempo GPU cae claramente, conviene crear una variante visual sencilla o un flipbook en Eco; eso requiere QA visual de las habilidades.

## 8. CPU, enemigos, pools y memoria nativa

### Hordas y física

LEVEL 1 permite **450 enemigos simultáneos** y durante overtime sube hasta **2000**. Los prefabs actuales usan Rigidbody dinámico y CapsuleCollider. No se encontró NavMeshAgent serializado ni una ruta que lo añada: aunque EnemyController contiene una rama opcional para NavMeshAgent, no se atribuyó a la build un coste de 2000 agentes de navegación inexistentes.

El timestep es 0,02 s, es decir, 50 pasos físicos/s. Con 2000 enemigos eso puede implicar 100 000 callbacks del controlador/s, además de PhysX y lógica visual. Es un cálculo de frecuencia, no una medición de tiempo CPU. La matriz ya desactiva Enemy–Enemy, Enemy–Buildings y Enemy–Store; la separación se resuelve por código con grid y un límite de vecinos. Esas optimizaciones se conservaron.

No se redujo el número de enemigos, el daño, las recompensas ni el timestep. Si el escenario tardío sigue limitado por CPU, la siguiente fase deberá presupuestar simulación y representación de hordas: reducir frecuencia de lógica distante, compartir consultas espaciales o adoptar representaciones más ligeras, comprobando equivalencia del gameplay. Añadir ECS/Jobs sin ese diagnóstico sería una reescritura prematura.

### Fuga de materiales corregida

La ruta `PoolManager.SpawnOrb → OrbConfiguration.ApplyToOrb → BaseCollectible.SetVisuals` creaba `new Material(material)` por cada spawn y perdía la referencia a la instancia anterior. Desactivar el GameObject no liberaba ese material nativo. Los tres tamaños de orb utilizan la misma fuente de material, así que era trabajo repetido sin necesidad.

Ahora el objeto conserva una única instancia, restaura sus propiedades cuando se reutiliza y la libera en OnDestroy. Si cambia realmente el material/shader fuente, reemplaza y libera la instancia anterior. ExperienceOrb usa la misma propiedad en vez de provocar otra clonación implícita. Esto corrige crecimiento de memoria/churn; no convierte por sí mismo todos los orbes en un único draw call. Unity documenta que el material instanciado pertenece al código que lo solicita. [11]

### Búsqueda cuadrática corregida

Scatter hacía una copia de ActiveEnemies y, por cada enemigo, recorría toda la lista calculando distancias y creando listas de vecinos/candidatos. Con 450 entradas son hasta unos 202 500 pares; con 2000, unos cuatro millones antes de los filtros. La tarea se ejecuta cada cuatro segundos cuando corresponde, con riesgo de tirones periódicos.

La nueva implementación construye el grid existente y consulta las celdas del radio. Reutiliza listas y sets, compara distancias al cuadrado y mantiene el orden de candidatos de ActiveEnemies. Conserva radios, umbrales, avisos de aparición y cantidades dispersadas. Un grid no vuelve gratuita una concentración patológica de todos los enemigos en la misma celda; la mejora consiste en evitar barridos globales cuando la distribución no los necesita y eliminar allocations recurrentes tras calentarse.

### Registro de actualizaciones y otros pools

UpdateManager mantenía listas y HashSets, pero procesaba altas/bajas pendientes actualizando sólo las listas. Esto podía dejar registros inconsistentes y duplicados o callbacks perdidos en Fixed/Late. Ahora ambas estructuras cambian juntas, se deduplican operaciones pendientes y prevalece la última operación si un objeto vuelve al pool y reaparece dentro del mismo tick. También se reconoce el null especial de objetos Unity destruidos a través de interfaces.

FloatingTextManager, al agotar el pool, creaba un objeto, lo encolaba inactivo y lo devolvía directamente. La siguiente solicitud podía recibirlo otra vez. Ahora todos los préstamos pasan por Dequeue y activación. Se eliminó GraphicRaycaster del canvas de números decorativos; no recibe interacción. Los pools no evitan reconstruir mallas TMP o crear strings/tweens por cada número. [12]

### Costes que siguen existiendo

El prewarm inicial configurado ronda **1050 objetos**, repartidos en lotes de 25/frame. `maxSize` de PoolManager es legacy y no impone un límite; al agotarse un pool con crecimiento permitido todavía se instancia. Overtime precalienta hasta 2000 enemigos. La reserva reduce tirones posteriores a cambio de memoria y trabajo de arranque, no reduce el coste de los activos.

Los drops pueden vivir más conforme aumenta el nivel y las oleadas altas producen varios por enemigo. El culling del renderer de un collectible no elimina sus actualizaciones, rotación, timers, atracción o componentes físicos. DropSpawner limita a cinco drops/frame, de modo que a 30 FPS procesa menos solicitudes por segundo que a 60. Se conserva esa semántica, pero la cola de recompensas debe medirse en muertes masivas.

Explosiones de enemigos y destrucción de edificios aún usan algunas rutas Instantiate/Destroy. Son candidatos a picos de eventos. Los escombros no están todos simulándose permanentemente: BuildingDestroyedVisual los mantiene inactivos/cinemáticos hasta necesitarlos. El visual del proyectil actual también tiene una salida temprana que evita una ruta de instanciación presente en código; no se confundió posibilidad con ejecución real.

CryoFieldEffect recorre ActiveEnemies para aplicar slow, y otras habilidades recorren enemigos/edificios al dar daño. BuildingTransparencyManager puede repetir proyecciones de objetivos por edificio cercano. Son candidatos para el siguiente perfil CPU, especialmente con hordas grandes. Los buffers NonAlloc de ataques/proyectiles pueden truncar resultados al saturarse; cambiar su tamaño o semántica es una decisión de gameplay y no se hizo como supuesto ahorro térmico.

## 9. Prácticas de industria y juegos comparables

**Vampire Survivors:** poncle documentó en marzo de 2022 que su cuello principal estaba en cálculos físicos sobre un núcleo, incluso con hardware gráfico potente. Es un antecedente relevante para el género; no prueba que manners.exe tenga exactamente el mismo límite ni permite trasladar sus porcentajes de mejora. La lección aplicable es perfilar simulación además de gráficos cuando se acumulan enemigos. [13]

**V Rising:** el caso de Stunlock publicado por Unity describe uso de Profiler para CPU/GC, trabajo paralelo cuando el hilo principal lo exige y agrupación de submeshes/materiales para reducir envío de render. Aquí hay una aplicación concreta antes de plantear una reescritura: revisar las 18–24 piezas de ciertos edificios intactos y medir el coste de despacharlas. Su escala y arquitectura son diferentes; se toma el método, no un presupuesto de FPS ajeno. [14]

**Alba:** Unity recoge su trabajo de pruebas automatizadas en entornos abiertos. Para un mapa estilizado como éste, una ruta de cámara repetible y capturas consistentes son más útiles que comparar dos partidas con distinta horda, resolución y temperatura. No se atribuyeron a Alba configuraciones de materiales o render que la fuente no documenta. [15]

La práctica común es fijar una plataforma mínima y un presupuesto de frame, medir, aislar una variable, corregir el subsistema que domina y verificar visual/gameplay. En un portátil interesa el rendimiento **sostenido** y la energía por segundo, no sólo el FPS máximo en una escena vacía y fría.

## Validación ejecutada en macOS

La primera build Release de diagnóstico produjo **390,3 MB en 72,35 segundos**, con **cero errores y siete warnings**. Los warnings corresponden a un APIDebugger sin script, cinco avisos de una misma expresión `pow` de RashoLaser en pasadas Metal y una subida de símbolos a Unity Services no disponible. [Resultado inicial](BUILD_MACOS.json).

Después de la validación se retiraron el runner de QA y el puente temporal al Editor. La **build final**, sin esos componentes, terminó en **15,08 segundos, con cero errores, los mismos siete warnings y 390,3 MB**. No hubo aviso de scripts pendientes de compilar. [Resultado final](BUILD_MACOS_FINAL.json). La aplicación final está en `/tmp/manners-performance/MannersPerformance.app`. Los 94 checks y las capturas documentan la ejecución instrumentada; el build final verifica la compilación después de retirarla.

La ejecución final del smoke en **Apple M4 Pro** pasó **94 checks**, registró **cero errores de runtime y ningún fallo fatal**, y restauró las preferencias usadas por la prueba. Verificó presets, escala aplicada a la copia runtime de URP, persistencia, VSync, controles y foco; los assets URP fuente conservaron sus valores durante la prueba. Se probaron salidas **1280×720, 1920×1080 y 1440×900**, esta última 16:10, sin desbordamientos de texto detectados automáticamente.

También se verificó que un cambio de pantalla no se guarda hasta confirmarse, que revierte al cancelar/cerrar y que el timeout de **15 segundos funciona con `Time.timeScale = 0`**. Abrir gráficos desde pausa y regresar conserva el juego congelado y devuelve el foco al acceso de gráficos. El reporte de la ejecución entregada y su lista exacta de checks se conservan en [SMOKE_MACOS.json](SMOKE_MACOS.json); [VALIDACION.md](VALIDACION.md) detalla los límites de cada prueba.

Se inspeccionaron visualmente el menú y la pausa a 720p y 1080p, además de la confirmación de pantalla: quedaron legibles, sin solapamientos ni tutorial superpuesto. Capturas finales: [menú](screenshots/MENU_GRAFICOS.png), [pausa](screenshots/PAUSA_GRAFICOS.png), [confirmación](screenshots/CONFIRMACION_PANTALLA.png) y [nivel](screenshots/NIVEL.png). El runner ocultó temporalmente el tutorial únicamente durante la prueba de pausa, sin alterar su progreso ni preferencias, para inspeccionar ese menú. Se eliminó tras la validación y no se entrega como código del juego.

También se recompilaron los scripts runtime con símbolos Editor/release y el conjunto Editor después de limpiar los temporales: cero errores. Los ajustes del proyecto volvieron a `manners.exe`, al identificador Standalone original y a Frame Timing Stats desactivado; los assets de fuentes quedaron restaurados y `git diff --check` no detectó incidencias.

**El CSV del smoke no constituye un benchmark sostenido.** Cambiar resoluciones, capturar imágenes, cargar escenas y mantener el Editor/importaciones abiertos altera el frame time. Por eso no se atribuye un porcentaje de mejora de FPS o temperatura a esa ejecución; el protocolo siguiente sigue siendo necesario.

## 10. Protocolo de validación sostenida

La configuración inicial recomendada para probar es **Equilibrado, VSync apagado, 60 FPS y salida hasta 1080 de altura**. En la PX13, probar después **Eco/30 FPS** manteniendo la misma escena. Menú y pausa tienen su propio máximo de 30. No comparar 60 FPS del perfil nuevo con 200 FPS ilimitados del anterior como si fueran igual trabajo.

| Escenario | Qué controla | Qué registrar |
|---|---|---|
| Menú orbital, 60–120 s | Sin gameplay, misma cámara/posición | GPU/CPU, resolución, FPS, consumo |
| Recorrido del nivel con poca horda | Edificios, agua, culling y terreno | Draws, SetPass, sombras, GPU |
| Horda de 450 | Simulación habitual máxima | Main thread, física, GC, frametime p95/p99 |
| Overtime y muertes masivas | Hasta 2000 enemigos, drops y efectos | Picos, colas, memoria y recuperación |
| Criogenia/láser/EMP y destrucción | Overdraw y shaders/eventos | GPU por pasada, GC y frame spikes |
| Repetir tras 15–20 min | Estado térmico sostenido | Frecuencias/potencia/temperatura junto a frametimes |

En cada caso mantener resolución, escala, FPS, perfil energético del portátil y carga comparable. Medir varias veces, con el editor y tareas de importación cerrados al cuantificar una build. Development/Profiler sirven para localizar el coste; una build release sirve para comprobar el rendimiento final. Deep Profile y spam de consola cambian las mediciones.

Aislar sucesivamente SSAO, sombras, postprocesado, minimapa y escala 100/75 %. Si baja el tiempo GPU al reducir escala, hay evidencia de coste por píxel. Si no cambia pero dominan Physics/ScriptRunBehaviourFixedUpdate, la resolución no arreglará ese límite. Si ambos tiempos son bajos pero el frame total es alto, revisar waits, sincronización y limitadores antes de optimizar geometría.

Se añadió `PerformanceCapture`, que sólo se crea con **`-manners-perf-capture`**. Escribe CSV y datos del hardware en `Application.persistentDataPath/Performance`: FPS medio agregado, p95/p99 del intervalo, tamaño de salida, escala, objetivo de FPS, VSync, enemigos, foco y pausa. CPU/GPU se registran si Frame Timing Stats está habilitado en la build y la plataforma devuelve valores; los no disponibles se dejan vacíos. La captura no mide temperaturas ni identifica por sí sola el origen de un frame lento. El tiempo CPU de FrameTimingManager puede incluir esperas, y Metal/WebGL tienen limitaciones documentadas. [16]

Frame Timing Stats permanece desactivado en la configuración normal del proyecto. La herramienta de build de validación lo habilita temporalmente para esa build y luego lo restaura. La captura añade trabajo de medición y escritura cada intervalo; no está activa en el juego normal.

Para comprobar ahorro térmico en Windows registrar además clocks, potencia y razón de limitación con herramientas del fabricante. En Mac usar Instruments/Metal cuando se necesite atribuir coste GPU. Una lectura de temperatura aislada o la velocidad del ventilador no identifica el subsistema responsable.

## 11. Qué no debe considerarse resuelto sin medición adicional

Los cambios corrigen defectos demostrados y proporcionan controles utilizables en las builds. **No garantizan ausencia de ventiladores ni una temperatura determinada en todos los equipos.** La prueba de la PX13 requiere ejecutar la nueva build allí durante una partida suficientemente larga.

El bake de iluminación continúa pendiente como decisión de contenido; no hay nuevos lightmaps. La combinación de mallas por edificio, simplificación de VFX, streaming de texturas y arquitectura de horda extrema siguen siendo trabajos candidatos sujetos al Profiler. Tampoco se validaron drivers, modo de energía o GPU seleccionada en el equipo Windows, que no estuvieron disponibles en esta revisión.

## Fuentes

Las fuentes web se consultaron el 10 de septiembre de 2026. La documentación Unity 6.3 y el código instalado de URP 17.3 tienen prioridad sobre consejos de otras versiones. Los casos históricos se citan como experiencias, no como benchmarks actuales del proyecto.

[1]: https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Application-targetFrameRate.html
[2]: https://support.apple.com/en-ca/102336
[3]: https://www.nvidia.com/en-us/geforce/news/geforce-rtx-40-series-laptop-power-and-efficiency/
[4]: https://www.asus.com/laptops/for-creators/proart/proart-px13-hn7306/techspec/
[5]: https://docs.unity3d.com/6000.3/Documentation/Manual/resolution-scale.html
[6]: https://docs.unity3d.com/6000.3/Documentation/Manual/urp/rendering-paths-comparison.html
[7]: https://docs.unity3d.com/6000.3/Documentation/Manual/urp/ssao-renderer-feature-reference.html
[8]: https://docs.unity3d.com/6000.3/Documentation/Manual/SRPBatcher.html
[9]: https://docs.unity.cn/Documentation/Manual/occlusion-culling-dynamic-gameobjects.html
[10]: https://docs.unity3d.com/6000.3/Documentation/Manual/TextureStreaming.html
[11]: https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Renderer-material.html
[12]: https://unity.com/how-to/unity-ui-optimization-tips
[13]: https://store.steampowered.com/news/posts/?enddate=1648165599&feed=steam_community_announcements
[14]: https://web-prd.hexagon.unity.com/resources/stunlock-studios-v-rising
[15]: https://unity.com/how-to/testing-and-quality-assurance-tips-unity-projects
[16]: https://docs.unity3d.com/6000.3/Documentation/Manual/frame-timing-manager.html

1. Unity. [Application.targetFrameRate][1], API Unity 6.3: límite, VSync y plataformas.
2. Apple. [Keep your Mac laptop within acceptable operating temperatures][2]: calor durante uso y condiciones de operación.
3. NVIDIA. [Power, performance and efficiency of GeForce RTX 40 Series laptops][3], 2023: diferencias de potencia y diseño entre portátiles.
4. ASUS. [ProArt PX13 HN7306, especificaciones][4]: variantes de GPU y pantalla; no identificación del equipo concreto.
5. Unity. [Resolution scale][5], Unity 6.3: escala de renderizado.
6. Unity. [Rendering path comparison][6], Unity 6.3: criterios Forward/Forward+/Deferred.
7. Unity. [SSAO Renderer Feature reference][7], Unity 6.3: downsample, muestras, profundidad y blur.
8. Unity. [SRP Batcher][8], Unity 6.3: agrupación de estado y límites.
9. Unity. [Occlusion culling and dynamic GameObjects][9]: dinámicos y oclusores estáticos.
10. Unity. [Texture mipmap streaming][10], Unity 6.3: importers y configuración de streaming.
11. Unity. [Renderer.material][11], Unity 6.0: instanciación y propiedad de materiales; comportamiento corroborado en APIs instaladas.
12. Unity. [UI optimization tips][12]: canvases, raycasters y pooling.
13. poncle. [Vampire Survivors, Development Roadmap Overview][13], 24 de marzo de 2022, sección del anuncio oficial sobre física y motor.
14. Unity / Stunlock Studios. [How Stunlock Studios built V Rising][14], 9 de diciembre de 2024.
15. Unity. [Testing and quality assurance tips][15], referencia al caso Alba y Unite 2022.
16. Unity. [Frame timing manager][16], Unity 6.3: disponibilidad, demora y límites de las métricas.

También se consultó [Configure for better performance in URP](https://docs.unity3d.com/6000.3/Documentation/Manual/urp/configure-for-better-performance.html), y las implementaciones instaladas de `UniversalRenderPipelineAsset`, `UniversalAdditionalCameraData`, `ShaderBuildPreprocessor` y `ScreenSpaceAmbientOcclusion` en `Library/PackageCache` para comprobar las APIs y variantes de esta versión.
