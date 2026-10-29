# manners.exe

La revisión actual de rendimiento y del merge está en [REVISION_M4_PRO.md](Docs/Performance/REVISION_M4_PRO.md), con las pruebas en [RESULTADOS_REVISION.md](Docs/Performance/RESULTADOS_REVISION.md). Por defecto: **1920×1080, pantalla completa y 60 FPS**; menú/pausa a 30 y sin foco a 15 cuando Unity continúa ejecutándose. Play Mode también limita los FPS y el tamaño del render 3D en vistas Retina. **Opciones → Gráficos** y **Pausa → Gráficos** permiten personalizarlo; Restablecer recupera estos valores sin borrar progreso. La captura CSV se activa desde **Tools → Manners → Performance** durante Play Mode.

Juego tipo *bullet-heaven* (estilo Vampire Survivors) hecho en **Unity 6 (URP)** con **WebGL** como plataforma objetivo y cámara en perspectiva inclinada.

La UI actual es editable en escenas y prefabs y tiene **inglés/español**; el idioma inicial es inglés y se cambia en **Opciones** o **Pausa**. Su identidad (paleta del documento de arte, placas en paralelogramo, franjas, fuente Orbitron y la cara de la I.A.) vive en el kit `UIStyle_Production` y se aplica con **Tools → Manners → UI**. La guía [UI_AUTHORING.md](Docs/UI_AUTHORING.md) explica dónde editar textos, placas, spritesheets, daño flotante y tutorial desde Inspector. **Tools → Manners → UI → Editar** abre directamente los prefabs principales y **Tools → Manners → UI → Corregir HUD** vuelve a dejar la barra de vida, los niveles del panel de sobrecargas y los números de daño como están definidos en el kit.

Este documento resume, a grandes rasgos, **cómo está pensado el código**: los patrones de diseño, la arquitectura y las reglas que se siguen, para que cualquiera que entre al proyecto entienda rápido la forma de trabajar. (El código no lleva comentarios; la intención se documenta aquí y con nombres claros.)

## Filosofía general
- **El rendimiento manda (objetivo WebGL).** Se evita el *garbage collection* por frame (buffers reutilizables, físicas `NonAlloc`, referencias cacheadas) y se cuida el batching. Si algo corre cada frame, se piensa su costo.
- **Datos fuera del código.** El balance vive en ScriptableObjects y la UI en componentes de escenas/prefabs con referencias serializadas. Los textos bilingües se editan junto a su TMP o en el controlador que formatea sus valores.
- **Bajo acoplamiento.** Los sistemas se comunican por eventos; no se conocen entre sí directamente.

## Patrones de diseño principales

### 1. Bucle de actualización centralizado — `UpdateManager`
En vez de que cada objeto tenga su propio `Update()`, los objetos implementan `IUpdateable` / `IFixedUpdateable` / `ILateUpdateable` y se **registran** en un único `UpdateManager` que itera sobre todos. Reduce el overhead de miles de `Update()` de Unity.
> **Regla:** no crear `Update()` por objeto; registrarse en el `UpdateManager`.

### 2. Object Pooling — `PoolManager` + `SpawnFactory`
Enemigos, proyectiles y coleccionables salen de *pools* con precarga escalonada. *Spawnear* reutiliza una instancia disponible; *despawnear* la devuelve. El pool puede crecer al agotarse y algunos VFX todavía crean objetos en ejecución, por lo que siguen existiendo costes de asignación. `SpawnFactory` es una **fachada** sobre el pool (`Create*` / `DestroyObject`).
> **Regla:** todo lo que aparece/desaparece muchas veces se poolea.

### 3. Singletons autocreados (Managers)
Los sistemas globales (`GameTimeManager`, `MusicManager`, `EnemySpawnManager`, …) son **singletons**. Muchos se **autocrean** con `RuntimeInitializeOnLoadMethod` + `DontDestroyOnLoad`, con un `ResetStatics` para sobrevivir al *domain reload* del editor. No hace falta colocarlos en cada escena.

### 4. Configuración por ScriptableObject (data-driven)
`EnemyConfiguration`, `WaveData`, `UpgradeData`, `GameBalanceConfig`, etc. son ScriptableObjects que definen stats, oleadas, mejoras y balance. El código *lee* esos datos; se *editan* desde el Inspector sin tocar C#.

### 5. Bus de eventos — `GameEvents` (Observer)
Un punto estático con eventos C# (`OnMatchTimeExpired`, `OnChestSpawned`, `OnShopLocationChanged`, …). Quien produce el evento lo dispara; quien le interesa se suscribe. Mantiene los sistemas desacoplados.

### 6. UI e idiomas
`GameLocalization` guarda el idioma y publica cambios. `LocalizedText` actualiza etiquetas fijas; `LocalizedString` contiene formatos dinámicos editables. Los controladores activan o instancian prefabs ya diseñados. Los avisos persistentes salen del registro `RuntimeUIPrefabs_Production`; sus objetos visuales no se crean por código. Las animaciones continuas se registran en `UpdateManager` y las transiciones usan DOTween/coroutines. El daño reutiliza un pool; el estallido del cofre reutiliza su ParticleSystem.

El estilo es un ScriptableObject (`UIStyle`, en `Assets/Configurations/Production/Resources/UI/`) con la paleta, las formas, los sprites generados, los materiales de placa y los tiempos de movimiento. Los componentes leen de ahí los colores que cambian en juego (vida, acentos, cara de la I.A.) y `UIStyleApplier` (Editor) escribe el resto en escenas y prefabs. El HUD de partida se compone de la barra de experiencia, la barra de vida con la cara de la I.A. y su lectura `HP actual/máxima` (`HealthBarUI`, que late en rojo con vida baja), el cronómetro, las monedas, el panel de sobrecargas (`OverrideHudPanel`, con el nivel de cada mejora requerida) y los números flotantes (`FloatingTextManager`, daño en rojo semitransparente). Las tarjetas de mejora se eligen manteniendo pulsado (`HoldToSelectButton`): el relleno a franjas avanza con tiempo real, también con el juego en pausa.

## Reglas y convenciones
- **Tiempo de juego vs. tiempo real:** la lógica de partida usa `GameTimeManager.GetGameTime()` (escalado por `timeScale`, se congela en pausa/tutorial/level-up), no el reloj real, para que todo quede sincronizado.
- **Sin asignaciones por frame** en rutas calientes: buffers `static` reutilizables y `Physics.*NonAlloc`.
- **Preservar `.meta`/GUID** al renombrar o reescribir scripts (no romper referencias de escenas/prefabs).
- **Layers/Tags** definen colisiones y filtros (Enemy, Player, Buildings, Store, …).
- Un solo error de compilación frena **todo** el assembly: si un cambio "no se siente", verificar primero que el proyecto compile.

## Estructura
- `Assets/Scripts/` — código del juego (Core, Enemy, Player, UI, Combat, Camera, Chest, Menus…).
- `Assets/Scripts/Localization/` — servicio y componentes de idioma.
- `Assets/Prefabs/UI/` — componentes visuales reales, separados por Notifications, Chest, Settings, Overrides, Overlays y Effects.
- `Assets/Configurations/Production/` — datos de producción con sufijo `_Production`; los globales se encuentran en su subcarpeta `Resources/`.
- `Assets/Configurations/Sandbox/` — copias independientes con sufijo `_Sandbox`, conectadas por `SandboxTuning`.
- `Assets/Resources/` — recursos auxiliares y del proveedor que conservan su carga original.

Los GUID y los identificadores de progreso se conservan al reorganizar assets. El Mapa 2 está pausado: sus archivos permanecen en sus carpetas originales. Los valores del sandbox solo pasan a producción con **Tools → Manners → Sandbox → 4**.
