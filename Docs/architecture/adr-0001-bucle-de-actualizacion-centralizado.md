# ADR-0001: Bucle de actualización centralizado (UpdateManager)

## Status

Accepted

> Aceptado el 2026-11-21 por Francisco Peralta, con `/architecture-decision accept ADR-0001`. Solo el usuario, o `technical-director` con su confirmación explícita, puede cambiar este estado.

## Date

2026-11-21 (registro a posteriori: la decisión se tomó al crear `UpdateManager.cs`, cuyo primer commit es del 2026-02-27)

## Last Verified

2026-11-21, contra el código de `develop` (commit `2c50581`) y Unity 6000.3.5f2

## Decision Makers

Francisco Peralta (FranciscoPS), autor principal de `UpdateManager.cs` según el historial de git, con un commit de M-A-R-M. Redactado por Claude Code a partir del código; la aceptación corresponde a Francisco.

## Summary

En una partida hay cientos de objetos que se actualizan cada frame (enemigos, proyectiles, pickups, efectos y UI), y un `Update()` de Unity por objeto multiplica las llamadas del motor en WebGL. Se decidió que esos objetos implementen `IUpdateable`, `IFixedUpdateable` o `ILateUpdateable` y se registren en un único `UpdateManager`, que los recorre desde sus propios `Update`, `FixedUpdate` y `LateUpdate`.

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 6000.3.5f2 (6.3 LTS) |
| **Domain** | Core |
| **Layer** | Foundation |
| **Knowledge Risk** | HIGH: 6.3 LTS es posterior al corte de conocimiento del modelo (`VERSION.md`). Las APIs de esta decisión existen desde antes de Unity 6 y no aparecen en `breaking-changes.md` ni en `deprecated-apis.md` |
| **References Consulted** | `docs/engine-reference/unity/VERSION.md`, `breaking-changes.md`, `deprecated-apis.md`, `current-best-practices.md`. No hay `modules/core.md` ni `modules/scripting.md`: el coste de los mensajes de `MonoBehaviour` y el orden de ejecución no están cubiertos (NOT SOURCEABLE) |
| **Post-Cutoff APIs Used** | None |
| **Verification Required** | Medir, con la horda máxima, el tiempo de `UpdateManager.Update` frente a los 16,67 ms del frame en una build Web de desarrollo hecha con su perfil (`Assets/Settings/Build Profiles/Web - Desktop - Development`, que activa WebAssembly 2023 aunque `ProjectSettings` no lo haga), con un `ProfilerMarker` por tipo en lugar de Deep Profile; comprobar que ningún objeto registrado depende del orden de actualización |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | None |
| **Enables** | ADR-0003 (los objetos del pool se registran en el bucle al salir del pool y se dan de baja al volver) |
| **Blocks** | None: ninguna historia de `production/epics/` espera este ADR |
| **Ordering Note** | Documenta código existente: aceptarlo no exige cambios. Conviene aceptarlo antes que ADR-0003, que depende de él |

## Context

### Problem Statement

manners.exe tiene cientos de enemigos, proyectiles, orbes y monedas activos a la vez, y WebGL es su plataforma objetivo («El rendimiento manda», README). El README justifica el gestor central en que «reduce el overhead de miles de Update() de Unity»; ese coste no se ha medido aislado en este proyecto, así que es la razón de diseño y no un dato propio. La regla ya se cumple en el código, pero solo estaba escrita en el README: este ADR la deja en el formato que leen `/create-control-manifest`, `/dev-story` y `/code-review`.

### Current State

- `Assets/Scripts/Core/UpdateManager.cs` define las tres interfaces y el gestor (9 commits entre el 2026-02-27 y el 2026-09-10).
- 40 clases de ejecución implementan las interfaces (39 `IUpdateable`, 2 `IFixedUpdateable` y 1 `ILateUpdateable`): enemigos, jugador, proyectiles, pickups, sobrecargas, VFX, menús y UI.
- La mayoría se registra en `OnEnable` y se da de baja en `OnDisable`. Las excepciones son `Awake`/`OnDestroy` (`ChestSpawner`, `EnemySpawnManager`, `GameTimeManager`), `Start`/`OnDestroy` (`ExperienceUI`, `PlayerHealth`), `OnSpawn`/`OnDespawn` del pool (`Projectile`), solo mientras dura el empuje (`EnemyController`) y hasta que se recoge (`ChestPickup`).
- Siete clases conservan un mensaje de Unity propio: `Update()` en `DropSpawner`, `GameGraphicsSettingsDriver`, `PerformanceCapture`, `GlobalCursorManager`, `MenuBackgroundCamera` y `ShopVisualFeedback`, y `LateUpdate()` en `MinimapSystem`. Seis son de instancia única y `ShopVisualFeedback` va en las cuatro tiendas de `LEVEL 1`: ninguna crece con la horda.
- La revisión de rendimiento corrigió que las listas y los conjuntos perdieran la sincronía con las altas y bajas pendientes (`Docs/Performance/ANALISIS_RENDIMIENTO.md`, §8); `PerformanceRegressionTests.DeferredRegistrationKeepsMembershipConsistent` lo comprueba en las tres fases.

### Constraints

- WebGL es la plataforma objetivo: 60 FPS en partida (16,67 ms por frame) y ninguna asignación de memoria por frame en las rutas calientes (README; `.claude/rules/engine-code.md`).
- El código C# no lleva comentarios: la intención vive en el README («Patrones de diseño principales») y en este ADR.
- El proyecto recarga el dominio y la escena al entrar en Play Mode (`ProjectSettings/EditorSettings.asset`: `m_EnterPlayModeOptions: 0`, ninguna recarga desactivada); los singletons llevan `ResetStatics` para no depender de ello.
- La build Web usa `webGLExceptionSupport: 1` (solo las excepciones lanzadas con `throw`), tanto en `ProjectSettings` como en los dos perfiles Web.

### Requirements

- Llamar cada frame a todos los objetos registrados y activos, en las fases Update, FixedUpdate y LateUpdate.
- Admitir altas y bajas durante el recorrido sin saltarse ni repetir objetos.
- Tolerar objetos destruidos que no se dieron de baja, y que la excepción de un objeto no detenga a los demás.
- No asignar memoria al recorrer.

## Decision

Toda clase que deba ejecutarse cada frame implementa una de las tres interfaces y se registra en `UpdateManager`. `UpdateManager` es un singleton que se crea solo (`[UpdateManager]`, `DontDestroyOnLoad`) y que, en cada fase, llama a `OnUpdate(Time.deltaTime)`, `OnFixedUpdate(Time.fixedDeltaTime)` u `OnLateUpdate(Time.deltaTime)` en los objetos cuyo `IsActive` es verdadero.

Forman parte de la decisión:

- Cada fase tiene una `List<T>` (capacidad inicial de 100, 50 y 20) y un `HashSet<T>` que impide registrar dos veces el mismo objeto.
- El recorrido va del último al primero. Un alta o una baja que llega durante el recorrido espera en una lista pendiente y se aplica al terminar la fase; si un objeto se da de baja y de alta en el mismo tick, prevalece la última operación.
- Un objeto `null` o un `UnityEngine.Object` destruido sale de la lista sin ser llamado. Si `IsActive` lanza una excepción, o `OnUpdate` lanza `MissingReferenceException`, el objeto también sale; cualquier otra excepción se registra con `Debug.LogException` y el recorrido continúa. En la build Web solo llegan a esos `catch` las excepciones lanzadas con `throw` (ver *Risks*).
- Mientras la aplicación se cierra, `Instance` no crea un gestor nuevo: devuelve el existente, o `null` si ya se destruyó. `ResetStatics` (`RuntimeInitializeOnLoadMethod(SubsystemRegistration)`) limpia el estado estático en el editor.
- Las siete clases de *Current State* quedan como excepciones toleradas porque no crecen con la horda. No sirven de modelo para código nuevo y se migran cuando se toquen por otro motivo.

### Architecture Diagram

```
             Unity: un mensaje por fase
   Update()             FixedUpdate()            LateUpdate()
      |                      |                        |
      v                      v                        v
+-----------------------------------------------------------------+
| UpdateManager  ([UpdateManager], DontDestroyOnLoad)              |
|   updateables       List + HashSet  -> OnUpdate(Time.deltaTime)   |
|   fixedUpdateables  List + HashSet  -> OnFixedUpdate(fixedDelta)  |
|   lateUpdateables   List + HashSet  -> OnLateUpdate(Time.deltaTime)|
|   altas y bajas pendientes, aplicadas al terminar cada fase       |
+-----------------------------------------------------------------+
      ^ Register(this)                    ^ Unregister(this)
      | OnEnable, OnSpawn, Awake          | OnDisable, OnDespawn, OnDestroy
+-----------------------------------------------------------------+
| 40 clases: enemigos, proyectiles, pickups, sobrecargas, UI...    |
+-----------------------------------------------------------------+
```

### Key Interfaces

```csharp
public interface IUpdateable
{
    void OnUpdate(float deltaTime);
    bool IsActive { get; }
}

public interface IFixedUpdateable
{
    void OnFixedUpdate(float fixedDeltaTime);
    bool IsActive { get; }
}

public interface ILateUpdateable
{
    void OnLateUpdate(float deltaTime);
    bool IsActive { get; }
}

public class UpdateManager : MonoBehaviour
{
    public static UpdateManager Instance { get; }
    public void Register(IUpdateable updateable);
    public void Unregister(IUpdateable updateable);
    public void Register(IFixedUpdateable updateable);
    public void Unregister(IFixedUpdateable updateable);
    public void Register(ILateUpdateable updateable);
    public void Unregister(ILateUpdateable updateable);
    public void ClearAll();
    public int GetUpdateableCount();
    public int GetFixedUpdateableCount();
    public int GetLateUpdateableCount();
}
```

### Implementation Guidelines

- Toda clase nueva que deba ejecutarse cada frame **debe** implementar `IUpdateable`, `IFixedUpdateable` o `ILateUpdateable` y registrarse en `UpdateManager`; **nunca debe** declarar `Update()`, `FixedUpdate()` ni `LateUpdate()` propios. Las siete excepciones de *Current State* no amplían la regla.
- El registro **debe** hacerse en `OnEnable` y la baja en `OnDisable`. Un objeto del pool que se registre en `OnSpawn` **debe** darse de baja en `OnDespawn`, como `Projectile`.
- Antes de `Register` o `Unregister`, el código **debe** comprobar que `UpdateManager.Instance != null`: es `null` si el gestor ya se destruyó durante el cierre.
- `IsActive` **debe** ser barato, sin efectos secundarios, y pasar a `false` en cuanto el objeto se desactiva (normalmente `isActiveAndEnabled`): se lee cada frame en cada objeto registrado, y una baja pedida durante el recorrido no se aplica hasta que termina la fase.
- `OnUpdate`, `OnFixedUpdate` y `OnLateUpdate` **nunca deben** asignar memoria administrada (LINQ, `new` de tipos por referencia, concatenación de strings, lambdas que capturan variables); se usan buffers reutilizables y `Physics.*NonAlloc`.
- La lógica de partida **debe** medir el tiempo con `GameTimeManager.GetGameTime()` o con el `deltaTime` recibido, que es `Time.deltaTime` y por tanto se detiene cuando `timeScale` es 0 (y `OnFixedUpdate` ni siquiera se llama). Lo que deba avanzar con el juego en pausa (UI, como `HoldToSelectButton`) **debe** usar `Time.unscaledDeltaTime`.
- Ningún objeto **debe** depender del orden en que `UpdateManager` llama a los objetos registrados.
- Un objeto **nunca debe** usar excepciones para controlar el flujo dentro de `OnUpdate`: se registran en la consola cada frame y el objeto sigue en la lista.

## Alternatives Considered

### Alternative 1: Update() de Unity en cada objeto

- **Description**: cada `MonoBehaviour` declara sus propios `Update()`, `FixedUpdate()` o `LateUpdate()` y el motor los llama.
- **Pros**: es el patrón estándar de Unity, sin código propio ni registro manual; el Profiler muestra cada clase por separado y el orden se puede fijar con `DefaultExecutionOrder`.
- **Cons**: una llamada del motor por objeto y por frame; con cientos de enemigos, proyectiles y pickups es justo el coste que el README quiere evitar en WebGL.
- **Rejection Reason**: contradice la razón de diseño del proyecto (README, «Bucle de actualización centralizado»). Se conserva solo en las siete clases de *Current State*.

### Alternative 2: DOTS / ECS (Entities, ISystem y Burst)

- **Description**: enemigos y proyectiles pasan a ser entidades actualizadas por sistemas `ISystem` compilados con Burst.
- **Pros**: según la referencia del motor, ECS está listo para producción en 6.3 LTS y Burst con Jobs es la vía de mayor rendimiento para bucles masivos (`current-best-practices.md`).
- **Cons**: exige reescribir el gameplay, la física de contacto, los VFX y la integración con la UI en uGUI; la API de Entities cambió por completo en Unity 6 (`breaking-changes.md`).
- **Rejection Reason**: el juego está en Production y todo su código es `MonoBehaviour`; la migración no se justifica sin una medición que muestre que el bucle actual no llega a 60 FPS.

Sin evaluar: la API `PlayerLoop`, para insertar sistemas propios en el bucle del motor, no está en `docs/engine-reference/unity/` (NOT SOURCEABLE).

## Consequences

### Positive

- Un único punto por fase: se puede leer cuántos objetos se actualizan (`GetUpdateableCount()` y los otros dos contadores), y lo usan `PerformanceSceneSmokeTests` y las comprobaciones de regresión de rendimiento.
- Las altas y bajas durante el recorrido son seguras, y un objeto destruido sin darse de baja no rompe el bucle.
- En el Editor y en escritorio, una excepción en un objeto no detiene a los demás (para la build Web, ver *Risks*).

### Negative

- Código repetido en cada clase: registro, baja e `IsActive`.
- El Profiler agrupa el coste de todos los objetos bajo `UpdateManager.Update`; separarlo exige Deep Profile o marcadores propios.
- No hay control del orden, ni entre los objetos registrados ni frente a los `Update()` de otros `MonoBehaviour`: `UpdateManager` no tiene `DefaultExecutionOrder`.
- Las excepciones se capturan: un error en `OnUpdate` se repite en la consola cada frame en vez de detener el juego.

### Neutral

- Las siete excepciones mantienen sus mensajes de Unity propios.

## Risks

- **Dependencias de orden ocultas**: un objeto que lee en su `OnUpdate` un estado que otro objeto registrado escribe en el mismo frame puede ver el valor del frame anterior. Mitigación: la regla de no depender del orden; si un caso lo necesita, se resuelve con eventos (`GameEvents`) o dentro de un solo objeto.
- **Bajas olvidadas**: un objeto desactivado que no se dio de baja sigue en la lista y se consulta su `IsActive` cada frame (si se destruye, sale solo). Mitigación: la regla `OnEnable`/`OnDisable` y la revisión de código.
- **Errores silenciosos**: si `IsActive` lanza una excepción, el objeto sale de la lista sin registrar nada en la consola. Mitigación: `IsActive` sin lógica.
- **Excepciones en la build Web**: con `webGLExceptionSupport: 1`, una referencia nula o un acceso fuera de rango de un array dentro de `OnUpdate` no llega al `catch`, y su efecto no está definido: lo que en el Editor es un error en la consola puede romper la build. Mitigación: comprobar las referencias en vez de confiar en el `catch`, y depurar con un modo de excepciones completo en una build de desarrollo. (Comportamiento descrito en la documentación de Unity; no está en la referencia fijada.)
- **Bajas masivas**: cada baja es un `List.Remove` que recorre la lista (`UpdateManager.cs:125` y `:351`), así que muchas muertes en el mismo frame suman. Mitigación: medir el peor frame de muertes en masa.
- **Recarga del dominio**: Unity 6.6 deja de recargar el dominio al entrar en Play Mode por defecto en los proyectos nuevos (`VERSION.md`). Este proyecto conserva su ajuste, y si se desactivara, `ResetStatics` ya limpia `instance` e `isQuitting`; las listas viven en la instancia.
- **Coste sin medir**: la ventaja frente a `Update()` por objeto no está medida en este proyecto. Mitigación: la medición de *Verification Required*.

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|------------|-------------|--------------------------|
| overrides.md | AC-28: con las tres sobrecargas activas, las mejoras premium en nivel 6 y 450 enemigos en la build de WebGL, el juego mantiene 60 FPS y las sobrecargas no asignan memoria por frame | Los efectos (`CryoFieldEffect`, `LaserBeamEffect`, `EmpPulseEffect`, `OverrideActivationHUD`, `OverrideProcVisual`) se actualizan desde el bucle central, que no asigna memoria al recorrer, y la regla de no asignar en `OnUpdate` los alcanza a todos |
| systems-index.md | «Bucle de actualización» es el sistema n.º 1, en la capa Foundation, y se documenta con un ADR | Este ADR es ese documento |

Foundational: además, condiciona a todos los sistemas del índice que se ejecutan cada frame (Control del jugador, Cámara, Ataque automático, Enemigos, Oleadas, Drops, Sobrecargas, HUD, Minimapa y Tutorial).

## Performance Implications

- **CPU**: una llamada del motor por fase en lugar de una por objeto; por objeto quedan la llamada de interfaz y la lectura de `IsActive`. Sin medición propia (ver *Verification Required*).
- **Memory**: tres listas, tres conjuntos y seis listas de pendientes, reservadas una sola vez; el recorrido no asigna.
- **Load Time**: despreciable; el gestor se crea la primera vez que algo pide `Instance`.
- **Network**: no aplica.

## Migration Plan

No hay migración: el código ya sigue la decisión. Las siete excepciones pasan a `IUpdateable` solo cuando se toquen por otro motivo.

**Rollback plan**: no aplica. Volver a `Update()` por objeto sería una decisión nueva, escrita en otro ADR que dejaría este en `Superseded`.

## Validation Criteria

- `PerformanceRegressionTests.DeferredRegistrationKeepsMembershipConsistent` pasa en sus tres casos (Update, FixedUpdate y LateUpdate).
- Una búsqueda en `Assets/Scripts` (sin `Editor/`) solo encuentra `Update()`, `FixedUpdate()` o `LateUpdate()` en las siete clases listadas; una clase nueva en esa búsqueda incumple este ADR. Hoy es una comprobación manual y puede convertirse en una prueba EditMode.
- La medición de *Verification Required* mantiene los 60 FPS con la horda máxima (pendiente de los presupuestos de rendimiento, plan de adopción 4.5).

## Related

- ADR-0003 (Object pooling con PoolManager y SpawnFactory): depende de este ADR.
- README, «Patrones de diseño principales», 1. Bucle de actualización centralizado.
- `.claude/rules/engine-code.md` y `.claude/docs/coding-standards.md`.
- `Docs/Performance/ANALISIS_RENDIMIENTO.md` (§8) y `Docs/Performance/VALIDACION.md`.
- `design/gdd/systems-index.md` (sistema n.º 1) y `design/gdd/overrides.md` (AC-28).
