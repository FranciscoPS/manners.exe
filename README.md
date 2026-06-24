# manners.exe

Juego tipo *bullet-heaven* (estilo Vampire Survivors) hecho en **Unity 6 (URP)** con **WebGL** como plataforma objetivo y cámara en perspectiva inclinada.

Este documento resume, a grandes rasgos, **cómo está pensado el código**: los patrones de diseño, la arquitectura y las reglas que se siguen, para que cualquiera que entre al proyecto entienda rápido la forma de trabajar. (El código no lleva comentarios; la intención se documenta aquí y con nombres claros.)

## Filosofía general
- **El rendimiento manda (objetivo WebGL).** Se evita el *garbage collection* por frame (buffers reutilizables, físicas `NonAlloc`, referencias cacheadas) y se cuida el batching. Si algo corre cada frame, se piensa su costo.
- **Datos fuera del código.** El balance y el contenido viven en *assets* (ScriptableObjects), no en constantes incrustadas. Ajustar el juego = editar assets, no recompilar.
- **Bajo acoplamiento.** Los sistemas se comunican por eventos; no se conocen entre sí directamente.

## Patrones de diseño principales

### 1. Bucle de actualización centralizado — `UpdateManager`
En vez de que cada objeto tenga su propio `Update()`, los objetos implementan `IUpdateable` / `IFixedUpdateable` / `ILateUpdateable` y se **registran** en un único `UpdateManager` que itera sobre todos. Reduce el overhead de miles de `Update()` de Unity.
> **Regla:** no crear `Update()` por objeto; registrarse en el `UpdateManager`.

### 2. Object Pooling — `PoolManager` + `SpawnFactory`
Enemigos, proyectiles y coleccionables salen de *pools* pre-asignados. *Spawnear* = encender un objeto; *despawnear* = apagarlo. **Nunca** se hace `Instantiate`/`Destroy` en caliente (evita picos de GC). `SpawnFactory` es una **fachada** simple sobre el pool (`Create*` / `DestroyObject`).
> **Regla:** todo lo que aparece/desaparece muchas veces se poolea.

### 3. Singletons autocreados (Managers)
Los sistemas globales (`GameTimeManager`, `MusicManager`, `EnemySpawnManager`, …) son **singletons**. Muchos se **autocrean** con `RuntimeInitializeOnLoadMethod` + `DontDestroyOnLoad`, con un `ResetStatics` para sobrevivir al *domain reload* del editor. No hace falta colocarlos en cada escena.

### 4. Configuración por ScriptableObject (data-driven)
`EnemyConfiguration`, `WaveData`, `UpgradeData`, `GameBalanceConfig`, etc. son ScriptableObjects que definen stats, oleadas, mejoras y balance. El código *lee* esos datos; se *editan* desde el Inspector sin tocar C#.

### 5. Bus de eventos — `GameEvents` (Observer)
Un punto estático con eventos C# (`OnMatchTimeExpired`, `OnChestSpawned`, `OnShopLocationChanged`, …). Quien produce el evento lo dispara; quien le interesa se suscribe. Mantiene los sistemas desacoplados.

## Reglas y convenciones
- **Tiempo de juego vs. tiempo real:** la lógica de partida usa `GameTimeManager.GetGameTime()` (escalado por `timeScale`, se congela en pausa/tutorial/level-up), no el reloj real, para que todo quede sincronizado.
- **Sin asignaciones por frame** en rutas calientes: buffers `static` reutilizables y `Physics.*NonAlloc`.
- **Preservar `.meta`/GUID** al renombrar o reescribir scripts (no romper referencias de escenas/prefabs).
- **Layers/Tags** definen colisiones y filtros (Enemy, Player, Buildings, Store, …).
- Un solo error de compilación frena **todo** el assembly: si un cambio "no se siente", verificar primero que el proyecto compile.

## Estructura
- `Assets/Scripts/` — código del juego (Core, Enemy, Player, UI, Combat, Camera, Chest, Menus…).
- `Assets/Configurations/` — ScriptableObjects de balance (enemigos, oleadas, orbes, proyectiles).
- `Assets/Resources/` — assets cargados en runtime (balance, base de datos de SFX, prefabs por nombre).
