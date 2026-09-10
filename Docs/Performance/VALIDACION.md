# Verificación de las optimizaciones

Las pruebas comprueban compilación, funcionamiento de los cambios y estado de los assets. No miden la temperatura de la ASUS PX13 ni equivalen a una partida de 20 minutos. El diagnóstico y el protocolo sostenido están en [ANALISIS_RENDIMIENTO.md](ANALISIS_RENDIMIENTO.md).

## Pruebas realizadas en Unity

`PerformanceValidation.RunChecks` ejecutó **29 grupos de comprobaciones, todos correctos**, en Unity 6000.3.5f2. El resultado completo está en [INVENTARIO_UNITY.json](INVENTARIO_UNITY.json).

- Valores por defecto, presets, sanitización de preferencias corruptas, NaN/infinito y límites de escala/texturas.
- Resolución proporcional 16:10, pantallas pequeñas, resolución nativa y tamaños inválidos.
- Referencias a renderers correctas en PC/Mobile, renderer sin SSAO y renderer ligero del minimapa.
- Profundidad conservada para el agua y ausencia de copia Opaque innecesaria.
- Grid: 240 consultas ordenadas y sin ordenar comparadas con búsqueda exhaustiva, coordenadas negativas, entradas nulas/destruidas y reconstrucción limpia.
- UpdateManager: altas/bajas pendientes, duplicados y reutilización dentro del tick en Update, FixedUpdate y LateUpdate.
- Materiales: 512 reconfiguraciones del mismo orb conservan identidad y cantidad de materiales nativos, color y escala, sin modificar el material fuente.

Las pruebas crean objetos inactivos en una preview scene aislada y la cierran al terminar; no cargan una partida ni guardan escenas. Se corrigieron dos errores de la propia herramienta de inventario durante la preparación: un null especial de Unity al buscar MeshFilter y la lectura indebida de escenas con LoadAllAssetsAtPath. No eran errores nuevos del gameplay.

Se compiló el conjunto de scripts runtime con referencias de la instalación real de Unity tanto con símbolos Editor como release: **cero errores**. El conjunto Editor también compiló con cero errores. Los warnings de campos serializados no asignados estáticamente y campos no usados del runtime no se presentan como fallos de compilación. Un harness adicional de .NET con stubs ejecutó 989 assertions y no asignó memoria administrada después de calentar el grid; los checks nativos anteriores son la evidencia relevante de integración con Unity.

## Build macOS y comprobación en el player

Se generó y ejecutó una **build macOS Release de diagnóstico**, con Unity 6000.3.5f2 y GPU Apple M4 Pro. La primera compilación terminó correctamente en **72,35 segundos**, con **0 errores, 7 warnings y 390,3 MB** de salida. Su detalle se conserva en [BUILD_MACOS.json](BUILD_MACOS.json).

Tras la prueba funcional y la retirada del runner de QA y del puente temporal de solicitudes al Editor, se reconstruyó la **build final sin esos componentes**: **15,08 segundos, 0 errores, 7 warnings y 390,3 MB**. No apareció ningún aviso de cambios de scripts sin compilar. El resultado final está en [BUILD_MACOS_FINAL.json](BUILD_MACOS_FINAL.json), y la aplicación está en `/tmp/manners-performance/MannersPerformance.app`. Las capturas y los 94 checks del smoke corresponden a la ejecución instrumentada; la compilación final confirma que el proyecto también construye correctamente después de retirar esa instrumentación temporal.

Los siete warnings se agrupan en tres causas: un script ausente en `APIDebugger` de MainMenu; cinco avisos de `pow` en el mismo Shader Graph RashoLaser, repetido en distintas pasadas Metal; y una conexión fallida a Unity Services para subir símbolos nativos. No son siete fallos del juego. El aviso de RashoLaser corresponde a una base procedente de Simple Noise y una potencia configurable; conviene revisar esa expresión si aparecen artefactos, pero el warning por sí solo no demuestra valores inválidos ni coste térmico anormal.

La ejecución final de la prueba funcional del player terminó con **94 checks correctos, cero errores de runtime, ningún fallo fatal y preferencias de gráficos restauradas**. El resultado completo está en [SMOKE_MACOS.json](SMOKE_MACOS.json); su lista de checks define el alcance exacto. Se comprobaron:

- Apertura de gráficos desde el menú, controles disponibles y foco dentro del panel.
- Aplicación y persistencia de Eco, Equilibrado y Alto; escala interna y presupuesto de texturas aplicados al asset URP clonado.
- Interacción VSync/límite de FPS, escala manual y etiqueta de configuración personalizada.
- Tamaños efectivos **1280×720, 1920×1080 y 1440×900 (16:10)**, con comprobaciones automáticas de etiquetas sin desbordamiento.
- Confirmación de cambios de pantalla: no guardar el cambio antes de aceptarlo, guardar al confirmar, revertir explícitamente, revertir al cerrar y restaurar después de **15 segundos incluso con `Time.timeScale = 0`**.
- Apertura de gráficos en LEVEL 1 pausado, mantenimiento de la pausa al modificar opciones y al volver, y restauración del foco al acceso de gráficos.
- Uso de `PC_NoAO_Renderer` en la configuración Equilibrado, `Minimap_Renderer` en la cámara del minimapa y conservación de los valores del asset URP fuente durante los cambios y la transición de escena.

Estas comprobaciones son funcionales. El CSV generado durante el smoke **no se usa como benchmark de FPS**: la prueba cambia resoluciones, toma capturas, carga escenas y comparte el equipo con el Editor/importaciones. No representa una partida sostenida en condiciones controladas.

La revisión visual final comprobó el menú y los gráficos de pausa a 720p y 1080p, además de la confirmación de pantalla: controles legibles, sin solapamientos ni tutorial superpuesto. Evidencia: [menú a 1080p](screenshots/MENU_GRAFICOS.png), [pausa a 720p](screenshots/PAUSA_GRAFICOS.png), [confirmación de pantalla](screenshots/CONFIRMACION_PANTALLA.png) y [nivel](screenshots/NIVEL.png). Para inspeccionar la pausa sin obstáculos, el runner ocultó temporalmente el tutorial en la prueba, sin avanzar pasos ni modificar sus preferencias guardadas. La revisión corresponde al menú de pausa sin tutorial superpuesto. El runner automático temporal se eliminó después de validar; no forma parte del código de juego que se entrega.

## Inventario resuelto

Estos valores provienen de preview scenes, **antes de Awake/Start**. No son renderers visibles ni draw calls. BuildingDestroyedVisual apaga escombros durante el arranque; las cifras de ejecución del player se documentan más abajo.

| Métrica | MainMenu | LEVEL 1 |
|---|---:|---:|
| GameObjects serializados/resueltos | 13 407 | 18 225 |
| GameObjects activos en preview | 12 997 | 17 943 |
| Renderers totales | 11 024 | 14 822 |
| Renderers activos y habilitados en preview | 11 024 | 14 814 |
| Materiales únicos de renderers | 19 | 37 |
| Renderers BatchingStatic | 64 | 53 |
| Renderers OccluderStatic | 64 | 53 |
| Renderers con lightmap asignado | 0 | 0 |
| Scripts faltantes detectados | 1 | 0 |

El script faltante en MainMenu es un hallazgo previo de integridad de escena, no una causa cuantificada de consumo; no se eliminaron componentes sin identificar su finalidad. La lista de texturas del cierre de dependencias incluye **139** assets y suma aproximadamente **331,13 MiB de memoria de assets importados en el editor**. No debe leerse como VRAM residente de la build ni como tamaño de descarga. La cifra de 331,13 MiB corresponde a la última instantánea; una lectura anterior fue de 369,52 MiB. La memoria del Editor fluctúa con sus cachés y assets dinámicos. Esa variación no demuestra un ahorro atribuible a los cambios de rendimiento.

## Inventario al arrancar la build

El smoke también recogió el estado **después de inicializar las escenas**. El arranque desactiva gran parte de los escombros que estaban activos en preview. Estos recuentos corresponden al comienzo de la ejecución, **antes del culling por cámara y de la selección de LOD**; no son draw calls, objetos visibles simultáneamente ni una medición de la horda tardía.

| Métrica al inicio del player | MainMenu | LEVEL 1 |
|---|---:|---:|
| Renderers activos y habilitados | 4 500 | 4 878 |
| Activos configurados para proyectar sombras | 4 499 | 4 877 |
| Materiales únicos activos | 14 | 31 |
| Suma de triángulos de mallas activas | 374 016 | 430 117 |
| Texturas de lightmap asignadas | 0 | 0 |

La suma de triángulos considera índices de las mallas compartidas de MeshRenderer/SkinnedMeshRenderer activos. **Excluye terreno, partículas, culling, LOD y pasadas adicionales**; no equivale a triángulos ejecutados por la GPU. Un renderer marcado para proyectar sombras tampoco implica que entre en la shadow map de cada frame. Los datos respaldan que hay miles de renderers habilitados pese a una geometría relativamente sencilla y que la iluminación sigue sin lightmaps.

## Cómo repetir los checks

En Unity, usar **Tools → Manners → Performance → Validate Graphics and Inventory Build Scenes**. Se escribe `Logs/performance-validation.json`; el inventario incluye referencias de escenas, luces, cámaras, terreno, materiales y texturas. El código queda en `Assets/Scripts/Editor/PerformanceValidation.cs` y `PerformanceRegressionChecks.cs`.

Para crear una build de diagnóstico Mac, usar **Tools → Manners → Performance → Build macOS Release for Performance Test**. Genera `/tmp/manners-performance/MannersPerformance.app` y `Logs/performance-build-mac.json`. La herramienta cambia temporalmente el nombre/identificador de la build para separar preferencias y habilita Frame Timing Stats; después restaura los valores originales del proyecto. No publica ni distribuye la aplicación.

Para medir una sesión de esa build:

```sh
"/tmp/manners-performance/MannersPerformance.app/Contents/MacOS/MannersPerformanceValidation" -manners-perf-capture
```

El nombre del ejecutable se puede confirmar en `Contents/Info.plist` de la build. La consola/player log indica la ruta del CSV dentro de `Application.persistentDataPath/Performance`. La captura es opcional, no se activa al jugar normalmente. En una build sin Frame Timing Stats, los valores CPU/GPU no disponibles quedan vacíos.

## Estado final del proyecto

Los scripts runtime compilaron de nuevo con símbolos Editor y release, y el conjunto Editor también compiló después de retirar los componentes temporales: **cero errores**. `git diff --check` terminó sin incidencias.

Se restauraron los ajustes temporales del proyecto: `productName = manners.exe`, identificador Standalone `com.Unity-Technologies.com.unity.template.urp-blank` y `enableFrameTimingStats = 0`. Los assets de fuentes quedaron restaurados. No permanecen el runner del smoke ni el puente de solicitudes al Editor. Se conservan los MenuItems de validación y el capturador de rendimiento voluntario para futuras pruebas.

## Límites de estas comprobaciones

No hay un porcentaje de reducción térmica certificado. Falta medir una partida sostenida en la PX13, otra en el M4 Pro, comparar condiciones térmicas y de energía equivalentes, y someter a carga el escenario de overtime con destrucción/habilidades. Compilar y pasar pruebas de lógica no demuestra que todos los equipos mantengan 60 FPS.

La configuración usa un límite de 60 FPS y perfiles reversibles; si el equipo sigue sin sostenerlo, el informe indica cómo separar coste de GPU, física y sincronización. La luz permanece dinámica: esta entrega no genera lightmaps ni cambia la dificultad de la horda.
