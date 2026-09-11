# Verificación de la integración de rendimiento

La integración se compiló y comprobó en **Unity 6000.3.5f2 sobre Windows, RTX 4070 Ti / Direct3D 11**. El diagnóstico del caso Mac y las fuentes están en [REVISION_M4_PRO.md](REVISION_M4_PRO.md). No se midieron temperatura, potencia, Metal ni rendimiento sostenido de la M4 Pro.

| Comprobación | Resultado | Evidencia |
|---|---|---|
| Suite Unity EditMode, incluyendo entrada a Play Mode y escenas reales | **39 aprobadas, 0 fallidas** | [PRUEBAS_UNITY.xml](PRUEBAS_UNITY.xml) |
| Inventario y validaciones nativas | **29 grupos aprobados** | [INVENTARIO_WINDOWS.json](INVENTARIO_WINDOWS.json) |
| Visibilidad de prefabs de enemigos, orientaciones y ambos niveles de calidad | **384 renders, 0 invisibles** | [RENDER_ENEMIGOS.txt](RENDER_ENEMIGOS.txt) |
| Menú, nivel, pausa, sin foco, destrucción y horda forzada | Correctos | [SMOKE_WINDOWS_EDITOR.json](SMOKE_WINDOWS_EDITOR.json) |
| Build Windows x64 de release con estadísticas de Frame Timing habilitadas para diagnóstico | **Succeeded, 0 errores** | [BUILD_WINDOWS.txt](BUILD_WINDOWS.txt) |
| Arranque de esa build, resolución y límite del menú | **1920×1080, 30 FPS tras el arranque** | [PLAYER_WINDOWS_MENU.csv](PLAYER_WINDOWS_MENU.csv) |

La build queda en `Builds/PerformanceReview/Windows/manners.exe`, junto con su carpeta de datos. No es una build Development. El helper de construcción activa Frame Timing Stats sólo para esa build y restaura el ajuste del proyecto al terminar. El ejecutable se abrió con `-batchmode -manners-perf-capture`; este modo de validación no sustituye una partida normal ni su rendimiento de GPU. La captura no recibió tiempos de GPU válidos y conserva esas columnas vacías.

## Lo que verifican las pruebas

Los casos de presupuesto comprueban pantallas de 60, 120, 144, 165, 240 y 360 Hz, refrescos fraccionales y datos inválidos. Verifican que VSync no anule el límite elegido, las resoluciones Full HD en diferentes tamaños, la protección ante vistas Game 4K/8K y el modo nativo explícito. Las pruebas de materiales comprueban reutilización de mil configuraciones de pickup/flipbook, liberación de instancias y conservación del asset fuente; también comprueban que dos piezas de un edificio compartan la variante de transparencia.

Las pruebas del gestor comprueban altas y bajas diferidas, duplicados, reciclaje y referencias destruidas en las tres fases. Las verificaciones nativas del grid comparan sus consultas con búsquedas exhaustivas. Se conservan las regresiones existentes de audio/cámara y se ejecutan con el resto de la suite.

La primera ejecución tuvo dos fallos en comprobaciones de `Destroy` diferido: las pruebas EditMode continuaban tras un update del editor sin garantizar que hubiera terminado un frame del player. Se cambió la espera por una espera acotada a dos segundos, manteniendo las aserciones de liberación. La suite posterior pasó completa; no se suprimieron errores ni se eliminaron aserciones para obtener el resultado.

## Escenas y estrés

| Escenario en el editor batch | Enemigos | Renderers activos/habilitados antes de culling | Callbacks Update | Callbacks FixedUpdate | Objetivo aplicado |
|---|---:|---:|---:|---:|---:|
| MainMenu | 0 | 4 500 | 3 | 0 | 30 FPS |
| LEVEL 1 inicial | 1 | 4 880 | 11 | 2 | 60 FPS |
| LEVEL 1, 450 enemigos adicionales en un spawn point | 451 | 5 780 | 461 | 452 | 60 FPS |

La horda utilizó una configuración de enemigos Rigidbody; esos callbacks fijos sí tienen trabajo físico, por lo que se mantienen. Los enemigos que usan NavMesh sólo se registran en la fase fija cuando necesitan knockback. La configuración del estrés se clonó durante la prueba y no modificó el asset de balance.

El runner utiliza **640×480** y simula el callback de foco para comprobar los objetivos de 30/60; prueba también el de 15 sin foco. Los intervalos de cinco segundos registraron 150 frames de menú, 301 de nivel y 300 con la horda. Son evidencia del funcionamiento del limitador en este entorno, **no un benchmark Full HD ni una promesa de 60 FPS en la M4 Pro**. La prueba del minimapa confirma su renderer sin sombras/postprocesado y que la cámara se deshabilita en pausa. La comprobación de resolución efectiva Full HD procede del ejecutable por separado.

El inventario final encontró **cero scripts faltantes** en ambas escenas. Se retiró del objeto `APIDebugger` de MainMenu la referencia huérfana a `LeaderboardDebugTester`, cuyo script ya no existe. Era un aviso de arranque, no la causa del consumo sostenido.

## Resolución del merge

Se integraron los siete archivos en conflicto conservando las mejoras compatibles de ambos lados:

- `BaseCollectible` y `ExperienceOrb`: un material propio por pickup del pool, con liberación y malla compartida.
- `PerformanceMonitor`: diagnóstico local F8 y contadores baratos, con release optativo.
- `UpdateManager`: listas y conjuntos coherentes, con la comprobación de objetos Unity destruidos.
- `WebGLOptimizer`: delega al servicio gráfico; ya no compite por escala, FPS o assets URP.
- `EnemySpawnManager`: grid ordenado, buffers reutilizados, trabajo repartido entre frames y precarga inicial gradual del rush.
- `PC_RPAsset`: renderers principal/sin SSAO/minimapa y cachés de filtrado regeneradas por Unity para esa configuración.

Las preferencias globales del editor que Unity reserializó durante las pruebas se restauraron a su estado previo. No se cambió la versión del motor, no se sustituyeron las texturas de todo el juego y no se publicaron builds ni cambios remotos.

## Repetición local

En un editor separado que tenga disponible el proyecto:

```text
Unity -batchmode -projectPath <proyecto> -runTests -testPlatform EditMode -testResults <resultado.xml> -logFile <pruebas.log>
Unity -batchmode -quit -projectPath <proyecto> -executeMethod PerformanceValidation.RunChecks -logFile <inventario.log>
Unity -batchmode -quit -projectPath <proyecto> -executeMethod PerformanceBuilds.WindowsPerformanceReview -logFile <build.log>
```

`WindowsPerformanceReview` incluye el inventario y las comprobaciones de visibilidad antes de construir. Para observar el equipo Mac afectado se puede activar la captura desde el menú Tools durante Play Mode, sin recompilar ni usar terminal. La comprobación térmica pendiente debe ejecutarse en esa Mac, comparando editor y build con las mismas condiciones.
