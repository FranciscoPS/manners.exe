# ADR-0002: Configuración por ScriptableObject con copias de producción y sandbox

## Status

Accepted

> Aceptado el 2026-11-21 por Francisco Peralta, con `/architecture-decision accept ADR-0002`. Solo el usuario, o `technical-director` con su confirmación explícita, puede cambiar este estado.

## Date

2026-11-21 (registro a posteriori: la configuración por ScriptableObject existe desde el primer commit de `GameBalanceConfig.cs`, del 2026-02-16; el sandbox, desde `SandboxTuning` el 2026-09-01, y los sufijos `_Production` y `_Sandbox`, desde `GameAssetPaths` el 2026-10-06)

## Last Verified

2026-11-21, contra el código de `develop` (commit `2c50581`) y Unity 6000.3.5f2

## Decision Makers

Francisco Peralta (FranciscoPS), autor de `GameBalanceConfig`, `SandboxTuning` y las herramientas del sandbox según el historial de git. Redactado por Claude Code a partir del código; la aceptación corresponde a Francisco.

## Summary

El balance de manners.exe (estadísticas, oleadas, mejoras, sobrecargas, cofres, tienda) se ajusta desde el Inspector y se prueba sin tocar los niveles que funcionan. Se decidió que cada valor ajustable y su visual vivan en ScriptableObjects de `Assets/Configurations/Production/` (sufijo `_Production`), con una copia independiente en `Assets/Configurations/Sandbox/` (`_Sandbox`) que `SandboxTuning` inyecta en la escena Sandbox; los globales se cargan desde `Resources` con una `Instance` perezosa, y los valores solo pasan del sandbox a producción con `Tools > Manners > Sandbox > 4`.

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 6000.3.5f2 (6.3 LTS) |
| **Domain** | Core |
| **Layer** | Foundation |
| **Knowledge Risk** | HIGH: 6.3 LTS es posterior al corte de conocimiento del modelo (`VERSION.md`). `deprecated-apis.md` lista `Resources.Load()` como reemplazado por Addressables y `current-best-practices.md` recomienda Addressables; esta decisión se aparta de esa recomendación a propósito |
| **References Consulted** | `docs/engine-reference/unity/VERSION.md`, `deprecated-apis.md` (Asset Loading), `current-best-practices.md` (Asset Management), `breaking-changes.md` (Addressables, 6.2+). No hay módulo de assets ni de scripting en `modules/` (NOT SOURCEABLE) |
| **Post-Cutoff APIs Used** | None |
| **Verification Required** | Que `Resources.Load` siga compilando sin avisos de obsolescencia al actualizar desde 6000.3.5f2 (hoy compila, y la build v3.0.0 salió con él); que `SandboxTuning` (`DefaultExecutionOrder(-5000)`) se ejecute antes que cualquier lector de los globales que inyecta |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | None |
| **Enables** | ADR-0003 (las llamadas de spawn reciben `EnemyConfiguration`, `ProjectileConfiguration` y `OrbConfiguration`) |
| **Blocks** | None: ninguna historia de `production/epics/` espera este ADR |
| **Ordering Note** | Documenta código existente: aceptarlo no exige cambios. Conviene aceptarlo antes que ADR-0003, que depende de él |

## Context

### Problem Statement

El juego se rebalancea a menudo: por ejemplo, `GameBalanceConfig_Production` fija una partida de 5 minutos aunque el valor por defecto del código es 15. El equipo necesita tocar números sin programar, probar los cambios sin arriesgar los niveles que ya funcionan y pasar a producción solo lo probado, sin romper referencias. CLAUDE.md lo fija como regla («los sistemas nuevos y los experimentos de balance se prueban en `Assets/Scenes/Sandbox.unity`, con sus propias copias de assets») y el README como patrón («Configuración por ScriptableObject»), pero no estaba escrito como decisión de arquitectura con sus alternativas.

### Current State

- 23 tipos concretos de ScriptableObject en el código de ejecución, más la base abstracta `OverrideEffectConfig` (con `ApplyTo(GameObject)` polimórfico): enemigos, proyectiles, orbes, oleadas, mejoras y su base de datos, balance general, tienda, cofres y sus objetos, sobrecargas (`OverrideData` y `CryoFieldConfig`, `LaserBeamConfig`, `EmpPulseConfig`), tutorial, muros invisibles, ranking, sonido, estilo del entorno y UI (`UIStyle`, `RuntimeUIPrefabs`, `SpriteAnimationData`).
- Producción: 46 assets en `Assets/Configurations/Production/` (`Enemies`, `Orbs`, `Overrides`, `Projectiles`, `Upgrades/Base`, `Upgrades/Premium`, `Waves` y `Resources`). Sandbox: 32 copias en `Assets/Configurations/Sandbox/`.
- Doce globales viven en `Assets/Configurations/Production/Resources/`. Once se leen con una `Instance` estática que hace `Resources.Load<T>("<Nombre>_Production")` la primera vez: `GameBalanceConfig`, `UpgradeDatabase`, `OverrideDatabase`, `ShopUpgradeDatabase`, `TutorialConfig`, `LeaderboardConfig`, `ChestOpeningConfig`, `InvisibleWallWarningConfig`, `SFXDatabase`, `ToonEnvironmentStyle` y `UIStyle` (en `Resources/UI/`). `RuntimeUIPrefabs` (también en `Resources/UI/`) no sigue la forma: su `Instance` solo se rellena dentro de `Spawn<T>`, y `HoverTooltipUI` lo carga con su propio `Resources.Load`. `ChestItemProvider` carga además todos los assets de `Resources/ChestItems/`.
- Seis de esos globales aceptan `OverrideInstance(...)`, que `SandboxTuning` (`DefaultExecutionOrder(-5000)`) llama en `Awake`. Cinco reciben su copia `_Sandbox`: `GameBalanceConfig`, `UpgradeDatabase`, `OverrideDatabase`, `ChestOpeningConfig` e `InvisibleWallWarningConfig`. `TutorialConfig` recibe una instancia creada en ejecución (`ScriptableObject.CreateInstance`) según `tutorialMode`, salvo con `UsarConfigDeProduccion`, que deja la de producción.
- Herramientas del editor: `Tools > Manners > Sandbox > 1. Crear assets del sandbox`, `2. Duplicar Nivel 1 en escena Sandbox`, `3. Conectar sandbox a la escena abierta` y `4. Comparar y sincronizar sandbox ↔ producción`. La cuarta (`SandboxSyncWindow` y `SandboxDiffTool`) compara cada pareja propiedad por propiedad, reasigna las referencias al lado correcto y escribe valores sin reemplazar assets, así que los GUID no cambian. `GameAssetPaths` fija las rutas y empareja por nombre base (`BaseName`, `SandboxFileName`).
- `UIAuthoringTests.RenamedSandboxAssetsRemainIndependentAndPairable` comprueba que hay al menos 30 parejas, que cada lado está en su carpeta, que no son el mismo objeto, que el `PersistentId` de cada `OverrideData` coincide con su nombre base y que la `UpgradeDatabase` del sandbox solo apunta a mejoras del sandbox.
- `EnemySpawnManager` clona en ejecución (`Instantiate`) una `EnemyConfiguration` para la oleada final y modifica la copia, no el asset.
- Fuera de este esquema quedan las carpetas del Mapa 2, en pausa (`Assets/Configurations/Enemies Configurations/` y `Assets/Configurations/Waves Configurations/Map2Waves/`), y algunos recursos auxiliares cargados por nombre desde `Resources` (el prefab de respaldo `Cofre`, el material `SpawnWarningMaterial`).

### Constraints

- WebGL y escritorio, sin Addressables (CLAUDE.md, «Technology Stack»: «ScriptableObjects en `Assets/Configurations/` y `Resources/` para los globales (sin Addressables)»).
- Los GUID de los assets y el `PersistentId` de cada `OverrideData` (identifica el progreso guardado) no pueden cambiar.
- El Mapa 2 está en pausa: sus assets no se tocan.
- Los valores se ajustan desde el Inspector con `[Tooltip]` y `[Header]` en español; el código C# no lleva comentarios.

### Requirements

- Todos los valores ajustables de un sistema y su visual en un solo ScriptableObject (CLAUDE.md, «Sobrecargas»: «Cualquier sistema ajustable nuevo sigue la misma idea»).
- Probar cambios en una escena aparte con copias independientes y llevarlos a producción sin romper referencias.
- Acceso síncrono a los globales desde cualquier sistema, también durante `Awake`.

## Decision

1. **Datos**: cada sistema ajustable define un tipo `ScriptableObject` con todos sus números y su visual. Su asset de producción vive en `Assets/Configurations/Production/<Carpeta>/` con el sufijo `_Production`, y su copia del sandbox en `Assets/Configurations/Sandbox/<Carpeta>/` con el mismo nombre base y el sufijo `_Sandbox`. Son dos assets reales, con GUID propio.
2. **Acceso**: un tipo que se lee de forma global expone `public static T Instance`, que carga `<Nombre>_Production` desde `Resources` la primera vez; lleva `ResetStatics` (`RuntimeInitializeOnLoadMethod(SubsystemRegistration)`) y, si el sandbox debe poder sustituirlo, `OverrideInstance(T)`. Las configuraciones que no son globales (enemigos, oleadas, orbes, proyectiles, mejoras, efectos de sobrecarga) se enlazan con referencias serializadas desde escenas, prefabs o bases de datos, no se cargan por nombre.
3. **Sandbox**: la escena `Assets/Scenes/Sandbox.unity`, un duplicado del Nivel 1, lleva un `SandboxTuning` que, en `Awake` y con orden de ejecución -5000, inyecta las copias del sandbox (y una configuración de tutorial creada en ejecución); `Tools > Manners > Sandbox > 3` conecta sus componentes a los assets `_Sandbox`.
4. **Sincronización**: los valores pasan entre sandbox y producción solo con `Tools > Manners > Sandbox > 4`, en cualquiera de los dos sentidos. Si la herramienta no lleva un cambio, se arregla la herramienta.
5. **Valores en ejecución**: el código que necesita valores distintos durante la partida clona el asset y trabaja sobre la copia.

### Architecture Diagram

```
Assets/Configurations/
  Production/   *_Production.asset --- referencias serializadas ---> escenas LEVEL 1 y MainMenu, prefabs
    Resources/  globales ---> T.Instance = Resources.Load<T>("<Nombre>_Production")
  Sandbox/      *_Sandbox.asset (mismo nombre base, GUID propio)
       |
       |  escena Sandbox: SandboxTuning.Awake (orden -5000)
       v
  T.OverrideInstance(copia)  --->  T.Instance devuelve la copia del sandbox

Tools > Manners > Sandbox > 4 (SandboxDiffTool)
  compara las parejas propiedad por propiedad y copia valores
  sandbox -> producción (o al revés); no reemplaza assets: los GUID se conservan
```

### Key Interfaces

```csharp
public static class GameAssetPaths
{
    public const string Production = "Assets/Configurations/Production";
    public const string Sandbox = "Assets/Configurations/Sandbox";
    public const string ProductionResources = Production + "/Resources";
    public const string UIResources = ProductionResources + "/UI";
    public static string BaseName(string assetName);
    public static string SandboxFileName(string sourcePath);
}

public class GameBalanceConfig : ScriptableObject
{
    public static GameBalanceConfig Instance { get; }
    public static void OverrideInstance(GameBalanceConfig config);
}

public abstract class OverrideEffectConfig : ScriptableObject
{
    public abstract void ApplyTo(GameObject effectInstance);
}

[DefaultExecutionOrder(-5000)]
public class SandboxTuning : MonoBehaviour
{
}

public static class SandboxDiffTool
{
    public static DiffResult Compare();
    public static int Apply(DiffResult result, IList<DiffEntry> entries, SyncDirection direction);
}
```

`UpgradeDatabase`, `OverrideDatabase`, `ChestOpeningConfig`, `InvisibleWallWarningConfig` y `TutorialConfig` siguen la misma forma de `Instance` y `OverrideInstance` que `GameBalanceConfig`. `SandboxDiffTool` es una herramienta del editor (`Assets/Scripts/Editor/`).

### Implementation Guidelines

- Todo valor de balance y su visual **debe** vivir en un ScriptableObject; **nunca debe** quedar fijo en C#. (La UI se edita en escenas y prefabs con referencias serializadas, y los ajustes técnicos de un componente, como la precarga de los pools, viven en ese componente.)
- Un tipo de configuración nuevo **debe** tener su asset en `Production/` con el sufijo `_Production` y su copia en `Sandbox/` con el mismo nombre base y el sufijo `_Sandbox`, y **debe** añadirse a `SandboxSetupTools` y a `SandboxDiffTool` para que se copie y se compare.
- Un global **debe** exponer `Instance` con carga perezosa de `<Nombre>_Production`, llevar `ResetStatics` y, si el sandbox debe poder probarlo, `OverrideInstance` llamado desde `SandboxTuning`. El código **nunca debe** cargar un global con `Resources.Load` fuera de su `Instance`.
- `Instance` **nunca debe** leerse desde un inicializador de campo ni desde un constructor: Unity no permite llamar a `Resources.Load` ahí. Se lee a partir de `Awake`.
- Los valores **solo** pasan del sandbox a producción con `Tools > Manners > Sandbox > 4`; **nunca** se borran, duplican ni pegan assets a mano.
- Un asset renombrado o movido **debe** llevar su `.meta`; un campo serializado renombrado **debe** llevar `[FormerlySerializedAs("nombreAnterior")]`; el `PersistentId` de un `OverrideData` **nunca debe** cambiar.
- El código que necesite valores distintos en ejecución **debe** clonar el asset con `Instantiate` y trabajar sobre la copia, como `EnemySpawnManager` en la oleada final; **nunca debe** escribir en el asset compartido.
- Los assets del Mapa 2 (`L2*`, `Enemies Configurations/` y `Waves Configurations/Map2Waves/`) **nunca deben** tocarse mientras el mapa esté en pausa.
- Cada campo ajustable **debe** llevar `[Tooltip]` y, por grupos, `[Header]`, en español.

## Alternatives Considered

### Alternative 1: Valores en el código o en los componentes de escenas y prefabs

- **Description**: constantes en C# o campos serializados en cada `MonoBehaviour` de las escenas y los prefabs.
- **Pros**: no hay assets aparte y cada valor está junto al código que lo usa.
- **Cons**: ajustar exige tocar código o escenas (conflictos de merge en los `.unity`; CLAUDE.md desaconseja editar escenas como texto), los valores quedan repartidos y no hay copia de sandbox sin duplicar escenas ni herramienta para comparar.
- **Rejection Reason**: contradice «Datos fuera del código» (README) y el flujo de trabajo del sandbox.

### Alternative 2: Archivos de datos externos (JSON o CSV)

- **Description**: los valores en archivos de texto que el juego lee al arrancar.
- **Pros**: se editan fuera de Unity y sus diferencias se leen bien en git.
- **Cons**: se pierden el Inspector (tooltips y rangos) y las referencias directas a assets: prefabs, sprites y materiales tendrían que buscarse por nombre, algo que el proyecto evita en la UI («no reemplazar referencias por búsquedas de nombres», CLAUDE.md).
- **Rejection Reason**: el equipo ajusta el balance en el Inspector y las configuraciones enlazan prefabs y visuales.

### Alternative 3: Addressables para los globales

- **Description**: cargar los globales con `Addressables.LoadAssetAsync<T>()`.
- **Pros**: según la referencia del motor, mejor control de memoria y carga asíncrona (`deprecated-apis.md`) y entrega remota de contenido (`current-best-practices.md`).
- **Cons**: la carga pasa a ser asíncrona y cada lector de `Instance` durante `Awake` tendría que esperarla; añade grupos y catálogo a la build de WebGL; desde 6.2 un fallo de carga lanza una excepción que hay que gestionar (`breaking-changes.md`). Los globales son pocos y pequeños.
- **Rejection Reason**: el proyecto decidió no usar Addressables (CLAUDE.md, «Technology Stack»). Se revisa si una medición de memoria o de carga en WebGL lo justifica.

## Consequences

### Positive

- El balance se edita desde el Inspector sin tocar código ni escenas.
- Los experimentos se hacen en el sandbox con copias reales, que se comparan y se sincronizan sin romper GUID.
- El acceso a los globales es síncrono y simple.

### Negative

- Cada asset existe dos veces: hay que mantener las parejas y ampliar las herramientas del sandbox con cada tipo nuevo.
- `Resources` ofrece, según la referencia del motor, peor control de memoria que Addressables y solo carga síncrona.
- Los globales son estáticos: el orden de inicialización importa, y la inyección del sandbox depende del orden -5000.

### Neutral

- Seis globales se pueden sustituir en el sandbox; los otros seis (`ShopUpgradeDatabase`, `SFXDatabase`, `LeaderboardConfig`, `ToonEnvironmentStyle`, `UIStyle` y `RuntimeUIPrefabs`) usan siempre los de producción.

## Risks

- **Inyección tardía**: un script que lea uno de los globales inyectables antes del `Awake` de `SandboxTuning` (con un orden menor que -5000, desde un inicializador `BeforeSceneLoad` o desde un inicializador estático) recibe en el sandbox el asset de producción. Hoy no ocurre: ni `LanguageSelector` (-10000) ni los inicializadores `BeforeSceneLoad` (`ResolutionBootstrap`, `UIStyleClock`) leen esos globales. Mitigación: la regla de *Verification Required*.
- **Sustitución para toda la sesión**: `OverrideInstance` dura hasta salir de Play Mode. Si desde el Sandbox se vuelve al menú y se entra en `LEVEL 1`, el nivel sigue usando las copias del sandbox. Mitigación: probar producción en una sesión de Play nueva.
- **Globales sin `ResetStatics`**: `LeaderboardConfig`, `ShopUpgradeDatabase` y `SFXDatabase` no limpian su `instance`. Como no se pueden sustituir, sin recarga del dominio (Unity 6.6 la desactiva por defecto en los proyectos nuevos, `VERSION.md`) seguirían apuntando al mismo asset de producción, y su getter lo recarga si se destruye: es una incoherencia con el patrón, no un fallo. Mitigación: añadírselo por coherencia (deuda técnica).
- **Datos que dependen del código**: `EnemyConfiguration.enemyPoolType` y `GameBalanceConfig.EnemyDropConfig.enemyType` usan `PoolManager.PoolType`, de modo que los datos dependen de una clase de ejecución (ciclo señalado en `design/gdd/systems-index.md`, *Circular Dependencies*). Mitigación propuesta allí: que la configuración sea solo datos.
- **Herramienta incompleta**: un tipo nuevo que no se añada a `SandboxSetupTools` y `SandboxDiffTool` ni se copia ni se compara, y sus cambios del sandbox no llegan a producción. Mitigación: la guía de implementación y `UIAuthoringTests`.
- **Documentación desactualizada**: la página de Confluence «Sistema de Balance» (`design/gdd/balance-system.md`) sitúa los assets en `Assets/Resources/` y `Assets/ScriptableObjects/`, rutas que ya no existen. Mitigación: plan de adopción, 4.6.

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|------------|-------------|--------------------------|
| balance-system.md | §1: `GameBalanceConfig` es un ScriptableObject que se edita directamente en Unity, no en código, y es el archivo maestro de configuración; §3, §5, §6 y §10: cada mejora, tipo de enemigo, oleada y orbe es un asset individual | Fija la ubicación (`Production/` y `Sandbox/`), los sufijos y la forma de acceso de esos assets |
| overrides.md | Regla 1: cada sobrecarga se define con dos conjuntos de datos (identidad y requisitos; efecto con todos sus números y su visual), el juego los lee de `OverrideDatabase_Production` y el `PersistentId` no cambia nunca | `OverrideData` y su `OverrideEffectConfig` en `Production/Overrides/` con copias en `Sandbox/Overrides/`; `OverrideDatabase` es un global inyectable; la guía protege el `PersistentId` |
| overrides.md | *Overview*: «Cómo se configuran y aplican los efectos (una configuración por sobrecarga con todos sus números y su visual) es una decisión de arquitectura: → se documenta en un ADR» | Este ADR es ese documento |
| overrides.md | AC-1 (Config/Data): las tres sobrecargas tienen sus `PersistentId`, están en `OverrideDatabase_Production` y cada una tiene prefab de efecto y configuración | Las parejas y `UIAuthoringTests` comprueban los `PersistentId` y la separación entre producción y sandbox |
| systems-index.md | «Configuración de balance» es el sistema n.º 4, en la capa Foundation, del que dependen casi todos los demás | Define el contrato de datos que leen esos sistemas |

## Performance Implications

- **CPU**: leer un global cuesta una comprobación de `null` después de la primera carga.
- **Memory**: cada global retiene en memoria lo que referencia (prefabs, sprites, materiales, los clips de `SFXDatabase`) mientras su estático lo apunte, y todo lo que hay en una carpeta `Resources` entra en la build, se use o no (documentación de Unity anterior a Unity 6; no está en la referencia fijada). No medido.
- **Load Time**: cada global se carga de forma síncrona la primera vez que se pide.
- **Network**: no aplica.

## Migration Plan

No hay migración: el código ya sigue la decisión. Lo que queda como deuda (que `RuntimeUIPrefabs` siga la forma de los demás globales, añadir `ResetStatics` a tres globales y corregir las rutas de «Sistema de Balance») se hace en historias aparte.

**Rollback plan**: no aplica. Cambiar a Addressables o a datos externos sería una decisión nueva, escrita en otro ADR que dejaría este en `Superseded`.

## Validation Criteria

- `UIAuthoringTests.RenamedSandboxAssetsRemainIndependentAndPairable` pasa.
- `Tools > Manners > Sandbox > 4` no muestra diferencias inesperadas después de sincronizar.
- Una búsqueda de `Resources.Load` en `Assets/Scripts` (sin `Editor/`) solo encuentra las `Instance` de los globales, `ChestItemProvider`, los recursos auxiliares de *Current State* y, mientras no se corrija, `RuntimeUIPrefabs.Spawn` y `HoverTooltipUI`.

## Related

- ADR-0003 (Object pooling con PoolManager y SpawnFactory): depende de este ADR.
- README, «Patrones de diseño principales», 4. Configuración por ScriptableObject.
- CLAUDE.md, «Escenas y pruebas» y «Sobrecargas».
- `design/gdd/balance-system.md`, `design/gdd/overrides.md` y `design/gdd/systems-index.md` (sistema n.º 4).
