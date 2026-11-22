# ADR-0003: Object pooling con PoolManager y SpawnFactory

## Status

Accepted

> Aceptado el 2026-11-21 por Francisco Peralta, con `/architecture-decision accept ADR-0003`, después de ADR-0001 y ADR-0002. Solo el usuario, o `technical-director` con su confirmación explícita, puede cambiar este estado.

## Date

2026-11-21 (registro a posteriori: `PoolManager.cs` existe desde el 2026-02-06 y `SpawnFactory.cs` desde el 2026-02-27)

## Last Verified

2026-11-21, contra el código de `develop` (commit `2c50581`) y Unity 6000.3.5f2

## Decision Makers

Francisco Peralta (FranciscoPS), autor principal de `PoolManager.cs` y `SpawnFactory.cs` según el historial de git, con tres commits de M-A-R-M en `PoolManager.cs`. Redactado por Claude Code a partir del código; la aceptación corresponde a Francisco.

## Summary

Enemigos, proyectiles, orbes de experiencia, monedas y diamantes aparecen y desaparecen cientos de veces por partida, y crearlos y destruirlos cada vez produce tirones y basura en WebGL. Se decidió que salgan de pools de `PoolManager`, precargados en lotes repartidos en varios frames al cargar el nivel, con `SpawnFactory` como fachada para crearlos y devolverlos e `IPoolable` para reiniciar su estado en cada reutilización.

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 6000.3.5f2 (6.3 LTS) |
| **Domain** | Core |
| **Layer** | Core |
| **Knowledge Risk** | HIGH: 6.3 LTS es posterior al corte de conocimiento del modelo (`VERSION.md`). Las APIs de esta decisión (`Instantiate`, `SetActive`, `DontDestroyOnLoad`, `SceneManager.sceneLoaded`, corrutinas) existen desde antes de Unity 6 y no aparecen en `breaking-changes.md` ni en `deprecated-apis.md` |
| **References Consulted** | `docs/engine-reference/unity/VERSION.md`, `breaking-changes.md`, `deprecated-apis.md`, `current-best-practices.md`. La referencia no cubre `UnityEngine.Pool` (el proyecto lo usó con 6000.3.5f2 hasta el commit `5734c19`) ni el coste de `Instantiate` y de la recolección de basura en WebGL (NOT SOURCEABLE) |
| **Post-Cutoff APIs Used** | None |
| **Verification Required** | Que la precarga inicial (unos 1050 objetos en lotes de 25 por frame, `Docs/Performance/ANALISIS_RENDIMIENTO.md`, §8) no produzca un tirón visible al cargar el nivel en WebGL; que, con `warnOnGrow` activado, una partida normal no haga crecer ningún pool antes de la oleada final |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-0001, ADR-0002 |
| **Enables** | None |
| **Blocks** | None: ninguna historia de `production/epics/` espera este ADR |
| **Ordering Note** | Documenta código existente: aceptarlo no exige cambios, pero solo puede aceptarse después de ADR-0001 (los objetos del pool se dan de alta y de baja en `UpdateManager`) y ADR-0002 (las llamadas de spawn reciben ScriptableObjects de configuración) |

## Context

### Problem Statement

Una partida de manners.exe genera hordas de enemigos, un proyectil por disparo y orbes, monedas y diamantes en cada muerte, y la oleada final llega a precalentar hasta 2000 enemigos. Con `Instantiate` y `Destroy` en cada aparición, el coste de crear objetos y la basura que dejan se pagan en mitad de la partida, en WebGL. El README lo fija como regla («todo lo que aparece/desaparece muchas veces se poolea»), pero no estaba escrito como decisión de arquitectura con su contrato, sus excepciones y su deuda.

### Current State

- `PoolManager` (547 líneas) es un `MonoBehaviour` colocado en las escenas de nivel: `LEVEL 1` y `Sandbox` llevan uno cada una y `MainMenu` ninguno. Asigna `Instance` en `Awake`, pasa a `DontDestroyOnLoad` y, al cargarse otra escena, devuelve todos los objetos activos (`DespawnAll`). Si la escena siguiente trae otro `PoolManager`, este fusiona su lista de pools en el primero y se destruye.
- `PoolType`: `Projectile`, `ExperienceOrb`, `Enemy`, `BasicEnemy`, `FastEnemy`, `L2BasicEnemy` y `L2FastEnemy` (Mapa 2, en pausa), `Coin` y `Diamond`.
- Cada `PoolConfig` tiene sus prefabs (variantes, una al azar por instancia), `prewarmCount` (20 por defecto) y `preventGrow` (desactivado: el pool crece si se agota). `defaultCapacity` y `maxSize` son heredados del `ObjectPool` de Unity que se usaba antes (ver *Alternative 2*); `maxSize` no limita nada.
- La precarga se reparte en lotes por frame (`prewarmSpreadOverFrames`, 25 objetos por frame por defecto). `EnemySpawnManager` amplía la capacidad de forma escalonada para la oleada final (`EnsureCapacityOverFrames`), y `PerformanceMonitor` y el informe del sandbox leen las cifras de cada pool (`TryGetPoolStats`).
- Al sacar un objeto: se toma de la pila de inactivos (o se crea uno si el pool puede crecer), se coloca, se llama a `IPoolable.OnSpawn()` y se activa. Enemigos, orbes y monedas reciben su configuración (`ApplyToEnemy`, `ApplyToOrb`, o el tipo y el valor) antes de `OnSpawn`; los proyectiles (`ApplyToProjectile`) y el callback `onConfigure` de `Spawn<T>`, después de activarse. Al devolverlo: `OnDespawn()`, se desactiva y vuelve a la pila. Un objeto que no salió del pool solo se desactiva.
- `SpawnFactory` (135 líneas) es un singleton que se crea solo (`[SpawnFactory]`, `DontDestroyOnLoad`), implementa `ISpawnFactory` y guarda `PoolManager.Instance` en su `Awake`. Sin `PoolManager`, sus `Create*` devuelven `null` y `DestroyObject` usa `Destroy`.
- `IPoolable` lo implementan `Projectile` (reinicia su tiempo de vida, su velocidad y sus estelas, y entra y sale de `UpdateManager`), `BaseCollectible` (reinicia el estado de recogida y su tiempo de vida) y `EnemyPoolable` (restaura la vida).
- Ocho scripts crean o devuelven objetos con `SpawnFactory.Instance` (`AutoAttackSystem`, `SpawnPoint`, `EnemyHealth`, `DropSpawner`, `BuildingsScript`, `Projectile`, `BaseCollectible` y `SandboxCommands`); las muertes encolan sus drops en `DropSpawner`, que los crea repartidos en varios frames. `ISpawnFactory` no se inyecta en ningún sitio: todos usan la clase concreta.
- Siguen creándose con `Instantiate` en la partida: la explosión de cada enemigo al morir (`EnemyHealth`), el VFX de destrucción de cada edificio (`BuildingsScript`), el visual de cada onda del Pulso Electromagnético (`EmpPulseEffect`) y cada cofre (`ChestSpawner`). Los números de daño tienen su propio pool en `FloatingTextManager`, y otros efectos crean su visual una vez y lo reutilizan.
- `PerformanceRegressionTests.ReconfiguringPooledPickupReusesAndReleasesOwnedMaterials` comprueba que un pickup reutilizado conserva y libera bien sus materiales, un error que corrigió la revisión de rendimiento.

### Constraints

- WebGL es la plataforma objetivo: 60 FPS en partida y ninguna asignación de memoria por frame en las rutas calientes (README; `.claude/rules/engine-code.md`).
- La lista de pools se configura en el componente de cada escena de nivel; la escena `Sandbox` es un duplicado de `LEVEL 1` y `SandboxDiffTool` no compara el `PoolManager`.
- Los tipos `L2*` pertenecen al Mapa 2, en pausa: no se tocan.

### Requirements

- Cientos de enemigos, proyectiles y pickups activos a la vez, y hasta 2000 enemigos precalentados para la oleada final.
- Ningún tirón al cargar el nivel ni al empezar la oleada final.
- Cada objeto reutilizado vuelve a su estado inicial.
- Devolver todo al cambiar de escena.

## Decision

Todo objeto que aparece y desaparece muchas veces por partida sale de un pool de `PoolManager`, y el gameplay lo pide y lo devuelve a través de `SpawnFactory`:

- `PoolManager` mantiene un pool por `PoolType`, con su lista de prefabs y una pila de inactivos, precargado en lotes por frame al cargar el nivel. Un pool agotado crece salvo que tenga `preventGrow`.
- `IPoolable.OnSpawn()` se llama antes de `SetActive(true)`, es decir, antes del `OnEnable` del objeto, y `OnDespawn()` antes de `SetActive(false)`, antes de su `OnDisable`. Cada objeto del pool reinicia ahí el estado que cambia durante su vida. La configuración del tipo llega antes de `OnSpawn`, salvo en los proyectiles y en `Spawn<T>`, que la reciben después de activarse.
- `SpawnFactory` es la fachada del gameplay (`CreateEnemy`, `CreateProjectile`, `CreateExperienceOrb`, `CreateCollectible`, `DestroyObject`). El acceso directo a `PoolManager` queda para la capacidad y las estadísticas.
- Al cargar otra escena, `PoolManager` devuelve todos los objetos activos.
- Quedan fuera los objetos de instancia única o que se crean una sola vez (el efecto de una sobrecarga al activarse, la UI), y los VFX por evento citados en *Current State*, que quedan como deuda.

### Architecture Diagram

```
 AutoAttackSystem, SpawnPoint, EnemyHealth, DropSpawner, BuildingsScript...
        | Create*(posición, configuración)           | DestroyObject(objeto)
        v                                            v
 SpawnFactory   ([SpawnFactory], DontDestroyOnLoad, implementa ISpawnFactory)
        | SpawnEnemy / SpawnProjectile / SpawnOrb / SpawnCollectible / Despawn
        v
 PoolManager    (en la escena del nivel, después DontDestroyOnLoad)
   un pool por PoolType: prefabs[] + Stack<GameObject> de inactivos + totalCount
   precarga en lotes por frame; crece si se agota (preventGrow = false)
        | OnSpawn() antes de SetActive(true); OnDespawn() antes de SetActive(false)
        v
 IPoolable: Projectile, BaseCollectible, EnemyPoolable
        | Register / Unregister en UpdateManager (ADR-0001)
```

### Key Interfaces

```csharp
public interface IPoolable
{
    void OnSpawn();
    void OnDespawn();
}

public interface ISpawnFactory
{
    GameObject CreateEnemy(Vector3 position, EnemyConfiguration config);
    Projectile CreateProjectile(Vector3 position, Quaternion rotation, ProjectileConfiguration config);
    ExperienceOrb CreateExperienceOrb(Vector3 position, OrbConfiguration config);
    Collectible CreateCollectible(Vector3 position, Collectible.CollectibleType type, int value);
    void DestroyObject(GameObject obj);
    void PrewarmPools(int enemyCount, int projectileCount, int orbCount, int collectibleCount);
}

public class PoolManager : MonoBehaviour
{
    public static PoolManager Instance { get; }
    public enum PoolType { Projectile, ExperienceOrb, Enemy, BasicEnemy, FastEnemy, L2BasicEnemy, L2FastEnemy, Coin, Diamond }
    public GameObject Spawn(PoolType poolType, Vector3 position, Quaternion rotation);
    public T Spawn<T>(PoolType poolType, Vector3 position, Quaternion rotation, Action<T> onConfigure = null) where T : Component;
    public void Despawn(GameObject obj);
    public void PrewarmPool(PoolType poolType, int count);
    public void EnsureCapacityOverFrames(PoolType poolType, int totalInstances);
    public bool TryGetPoolStats(PoolType poolType, out int total, out int available);
    public void DespawnAll();
}
```

### Implementation Guidelines

- Todo objeto que aparezca y desaparezca muchas veces por partida **debe** salir de un pool; **nunca debe** crearse con `Instantiate` ni eliminarse con `Destroy` durante la partida.
- El gameplay **debe** crear y devolver los objetos del pool con `SpawnFactory.Instance` (`Create*` y `DestroyObject`); solo la gestión de capacidad y las estadísticas **deben** llamar a `PoolManager` directamente.
- Un objeto del pool **debe** reiniciar en `OnSpawn` todo el estado que cambie durante su vida (vida, temporizadores, velocidad, estelas, materiales) y **nunca debe** suponer que `OnSpawn` corre con el objeto activo ni, en un proyectil, con su configuración ya aplicada.
- Las referencias a objetos de la escena **deben** resolverse en `OnEnable` u `OnSpawn`, como hacen `EnemyController` y `BaseCollectible`: los objetos del pool cuelgan del `PoolManager` persistente, y su `Awake` y su `Start` no vuelven a ejecutarse.
- Cada prefab del pool **debe** tener un solo `IPoolable`, en su raíz: `PoolManager` solo llama al primero que encuentra ahí.
- Un objeto del pool **debe** darse de baja de `UpdateManager` al volver al pool (ADR-0001): en `OnDisable`, o en `OnDespawn` si se registró en `OnSpawn`.
- Un tipo nuevo de objeto masivo **debe** tener su `PoolType` y su `PoolConfig` en el `PoolManager` de `LEVEL 1` y de `Sandbox`, con un `prewarmCount` que evite que crezca en una partida normal (se comprueba con `warnOnGrow`).
- Toda precarga o ampliación de capacidad **debe** repartirse en varios frames (`prewarmSpreadOverFrames`, `EnsureCapacityOverFrames`).
- `Spawn` y `Despawn` **nunca deben** asignar memoria salvo cuando el pool crece.
- Los tipos `L2BasicEnemy` y `L2FastEnemy` **nunca deben** tocarse mientras el Mapa 2 esté en pausa.

## Alternatives Considered

### Alternative 1: Instantiate y Destroy en cada aparición

- **Description**: cada enemigo, proyectil o pickup se crea al aparecer y se destruye al desaparecer.
- **Pros**: no hay código de pool ni reinicio de estado: cada objeto nace limpio.
- **Cons**: el coste de instanciar y la basura de cada objeto destruido se pagan en mitad de la partida, cientos de veces por minuto; es lo que el README evita en WebGL («se evita el garbage collection por frame»).
- **Rejection Reason**: incompatible con el volumen de apariciones del juego.

### Alternative 2: UnityEngine.Pool.ObjectPool de Unity

- **Description**: el pool genérico del motor, con funciones para crear, tomar, devolver y destruir objetos. `PoolManager` lo usó con Unity 6000.3.5f2 hasta el 2026-06-07, cuando el commit `5734c19` («feat: pool optimization») lo sustituyó por las pilas propias; `defaultCapacity` y `maxSize` son los parámetros que quedaron de él.
- **Pros**: lo mantiene Unity y requiere menos código propio.
- **Cons**: no trae por sí solo lo que el proyecto usa (variantes de prefab por pool, precarga repartida en frames, ampliación escalonada para la oleada final, estadísticas por tipo); no está en `docs/engine-reference/unity/`.
- **Rejection Reason**: ya se usó y se sustituyó; volver a él no aporta un beneficio medido.

### Alternative 3: ECS (entidades en lugar de GameObjects)

- **Description**: enemigos y proyectiles como entidades de Entities, creadas en bloque.
- **Pros**: según la referencia del motor, ECS con Burst es la vía de mayor rendimiento para crear y actualizar muchos objetos (`current-best-practices.md`).
- **Cons**: obliga a reescribir el gameplay (ver ADR-0001, Alternative 2).
- **Rejection Reason**: la misma que en ADR-0001: el juego está en Production y no hay una medición que lo justifique.

## Consequences

### Positive

- Una vez precalentados, aparecer y desaparecer no asigna memoria.
- La precarga escalonada evita el tirón al cargar el nivel y al empezar la oleada final.
- Cada pool informa de su total y de sus disponibles, y la captura de rendimiento los registra.
- Cambiar de escena devuelve todos los objetos.

### Negative

- La memoria de los objetos precalentados (unos 1050) está reservada aunque no se usen.
- Reiniciar el estado es manual: un campo olvidado en `OnSpawn` deja estado de la vida anterior (el caso de los materiales de los pickups, ya corregido).
- Los pools crecen sin tope (`maxSize` no limita nada) y la oleada final llega a 2000 enemigos.
- La lista de pools vive en el componente de cada escena de nivel: un cambio en `LEVEL 1` hay que repetirlo en `Sandbox`.

### Neutral

- `ISpawnFactory` existe, pero nadie lo inyecta: todos los llamadores usan `SpawnFactory.Instance`.

## Risks

- **`SpawnFactory` fuera del patrón de singleton del proyecto**: su `Instance` crea el objeto aunque la aplicación se esté cerrando (no comprueba la salida, como sí hace `UpdateManager`) y no tiene `ResetStatics`. Además guarda `PoolManager.Instance` en su `Awake`: si algo lo pidiera en una escena sin `PoolManager`, como `MainMenu`, se quedaría sin pool el resto de la sesión y sus `Create*` devolverían `null`. Hoy nadie lo pide desde el menú. Mitigación: una historia de deuda que lo alinee con el patrón de `.claude/rules/engine-code.md` y que resuelva `PoolManager` cuando lo necesite.
- **`PoolManager` sin `ResetStatics`**: sin recarga del dominio (Unity 6.6 la desactiva por defecto en los proyectos nuevos, `VERSION.md`), `Instance` conservaría una referencia destruida. Las comprobaciones con `== null` la tratan como nula y el siguiente `Awake` la reemplaza, pero el operador `?.` no (`EnemySpawnManager.cs:191`) y llamaría al objeto destruido. Mitigación: añadir `ResetStatics` en la misma historia y comparar con `!= null` en lugar de usar `?.`.
- **Objetos sacados al cargar una escena**: `SceneManager.sceneLoaded` llega después del `Awake` y del `OnEnable` de la escena nueva, así que `DespawnAll` devuelve lo que se haya sacado del pool en esos métodos (documentación de Unity; no está en la referencia fijada). Mitigación: sacar objetos a partir de `Start`.
- **Crecimiento sin tope**: con `preventGrow` desactivado, un pico de apariciones instancia objetos nuevos en mitad de la partida. Mitigación: vigilarlo con `warnOnGrow` y fijar topes cuando existan los presupuestos de rendimiento (plan de adopción, 4.5).
- **VFX sin pool**: la explosión de cada enemigo, la destrucción de cada edificio y cada onda del Pulso crean un objeto nuevo; con hordas grandes es una asignación por evento. Mitigación: pasarlos a pools (deuda técnica).
- **Configuración por escena**: un pool añadido en `LEVEL 1` y olvidado en `Sandbox` hace que el sandbox se comporte distinto. Mitigación: la guía de implementación; ampliar `SandboxDiffTool` si vuelve a pasar.

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|------------|-------------|--------------------------|
| overrides.md | AC-27: un enemigo al que mata una sobrecarga suelta su experiencia y sus monedas como cualquier otra muerte | Los drops salen siempre del mismo camino (`EnemyHealth` → `DropSpawner` → `SpawnFactory`), sea cual sea la fuente del daño |
| overrides.md | AC-28: 60 FPS con 450 enemigos en la build de WebGL y sin asignaciones de memoria por frame de las sobrecargas | Las apariciones de enemigos, proyectiles y drops no asignan memoria una vez precalentados los pools; los VFX por evento sin pool quedan como riesgo |
| systems-index.md | «Pooling y fábrica de spawn» es el sistema n.º 2, en la capa Core, y se documenta con un ADR; entre los sistemas de alto riesgo, «cientos de enemigos simultáneos en WebGL» | Este ADR es ese documento y fija el contrato que mitiga ese riesgo |

## Performance Implications

- **CPU**: sacar un objeto cuesta sacarlo de la pila, colocarlo, aplicar su configuración, un `GetComponent<IPoolable>` y activarlo; crear un objeto solo ocurre al precalentar o cuando el pool crece.
- **Memory**: unos 1050 objetos precalentados al cargar el nivel y hasta 2000 enemigos para la oleada final (`Docs/Performance/ANALISIS_RENDIMIENTO.md`, §8), más el diccionario de objetos activos.
- **Load Time**: la precarga se reparte a 25 objetos por frame, unos 42 frames para 1050 objetos. Con el prefab activo, cada instancia ejecuta una vez `Awake`, `OnEnable` y `OnDisable` al crearse; ese coste forma parte de la precarga.
- **Network**: no aplica.

## Migration Plan

No hay migración: el código ya sigue la decisión. La deuda (el patrón de singleton de `SpawnFactory`, `ResetStatics` en `PoolManager` y pools para los VFX por evento) se hace en historias aparte.

**Rollback plan**: no aplica. Sustituir los pools por otro mecanismo sería una decisión nueva, escrita en otro ADR que dejaría este en `Superseded`.

## Validation Criteria

- `PerformanceRegressionTests.ReconfiguringPooledPickupReusesAndReleasesOwnedMaterials` pasa.
- Con `warnOnGrow` activado, una partida completa en `LEVEL 1` no muestra avisos de crecimiento antes de la oleada final.
- Una búsqueda de `Instantiate` en el código de gameplay solo encuentra los casos de *Current State* (instancias únicas y la deuda de VFX).

## Related

- ADR-0001 (Bucle de actualización centralizado): los objetos del pool se dan de alta y de baja en `UpdateManager`.
- ADR-0002 (Configuración por ScriptableObject): las llamadas de spawn reciben las configuraciones de enemigos, proyectiles y orbes.
- README, «Patrones de diseño principales», 2. Object Pooling.
- `.claude/rules/engine-code.md`.
- `Docs/Performance/ANALISIS_RENDIMIENTO.md` (§8) y `Docs/Performance/REVISION_M4_PRO.md`.
- `design/gdd/systems-index.md` (sistema n.º 2) y `design/gdd/overrides.md` (AC-27 y AC-28).
