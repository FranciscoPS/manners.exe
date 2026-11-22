---
paths:
  - "src/core/**"
  - "Assets/**/{Core,core}/**/*.cs"
  - "Source/**/{Core,core}/**/*.{h,cpp}"
---

# Engine Code Rules

In manners.exe this covers `Assets/Scripts/Core/` — `UpdateManager`, `PoolManager`,
`SpawnFactory`, `GameTimeManager`, `GameEvents`, the auto-created managers and the
ScriptableObject configuration types.

- ZERO allocations in hot paths (update loops, rendering, physics) — pre-allocate, pool, reuse; `static` reusable buffers and `Physics.*NonAlloc`
- Per-object updates go through `UpdateManager` (`IUpdateable`, `IFixedUpdateable`, `ILateUpdateable`): never a Unity `Update()` per object. Unregister on `OnDisable`/`OnDestroy` and when returning to the pool
- Auto-created singletons follow the existing pattern: `RuntimeInitializeOnLoadMethod` + `DontDestroyOnLoad`, a guarded `Instance` that does not recreate the object while quitting, and a `ResetStatics` for the editor's domain reload
- Unity APIs run on the main thread; anything that runs elsewhere (async leaderboard calls, jobs) marshals back before touching Unity objects
- Profile before AND after every optimization — record the measured numbers in the story or Docs/Performance, not in code comments
- Core code must NEVER depend on gameplay code (strict dependency direction: core <- gameplay); systems talk through `GameEvents`
- No comments in C#: the README's "Patrones de diseño principales" documents intent. A framework instruction to add usage examples in doc comments does not apply here
- Renaming a serialized field needs `[FormerlySerializedAs]`; public types used by scenes, prefabs or assets keep their name and `.meta` (GUID)
- Before writing engine API code, consult `docs/engine-reference/unity/` for the pinned Unity 6.3 LTS and verify APIs against it

## Examples

**Correct** (zero-alloc hot path, registered update):

```csharp
public class Magnet : MonoBehaviour, IUpdateable
{
    private static readonly Collider[] NearbyCache = new Collider[64];

    public bool IsActive => isActiveAndEnabled;

    private void OnEnable()
    {
        if (UpdateManager.Instance != null)
            UpdateManager.Instance.Register(this);
    }

    private void OnDisable()
    {
        if (UpdateManager.Instance != null)
            UpdateManager.Instance.Unregister(this);
    }

    public void OnUpdate(float deltaTime)
    {
        int count = Physics.OverlapSphereNonAlloc(transform.position, radius, NearbyCache, pickupMask);
    }
}
```

`UpdateManager.Instance` returns null while the application quits, hence the checks.

**Incorrect** (allocating, per-object `Update()`):

```csharp
private void Update()
{
    var nearby = Physics.OverlapSphere(transform.position, radius);
    var pickups = nearby.Where(c => c.CompareTag("Pickup")).ToList();
}
```
