---
paths:
  - "src/gameplay/**"
  - "src/game/**"
  - "Assets/**/{Gameplay,gameplay}/**/*.cs"
  - "Source/**/{Gameplay,gameplay}/**/*.{h,cpp}"
  - "Assets/Scripts/{Player,Enemy,Combat,Chest,Overrides,InvisibleWalls,Camera,VFX,Sandbox}/**/*.cs"
---

# Gameplay Code Rules

- ALL gameplay values come from the system's ScriptableObject (`Assets/Configurations/`, `_Production` and `_Sandbox` copies), NEVER hardcoded. A new tunable system keeps all its values and its visual in one ScriptableObject, like the override configs (`OverrideEffectConfig`)
- Match timing uses `GameTimeManager.GetGameTime()` (scaled, frozen in pause, tutorial and level-up), never the real clock; per-frame movement uses the `deltaTime` that `UpdateManager` passes in
- NO direct references to UI code — publish and subscribe through `GameEvents`
- Override combinations with the special upgrades (Multi disparo, Balas explosivas, Cadena de impacto) and between overrides are resolved in `OverrideCombatResolver`, always active when the player has the upgrade, never by chance, and secret to the player
- Frequent objects (enemies, projectiles, pickups, VFX) come from `PoolManager` / `SpawnFactory` and reset their state when reused
- Every gameplay system exposes a clear interface; state machines have explicit states and transitions
- Logic that can be tested in EditMode lives in methods a test can call (`Assets/Scripts/Editor/[System]Tests.cs`), separate from presentation
- The Jira key goes in the commit and PR, not in code comments (no comments in C#)
- Balance experiments happen in `Sandbox.unity` with the `_Sandbox` assets; values reach production only through `Tools > Manners > Sandbox > 4`
- Map 2 (`MilitaryBase`, `L2*` enemies) is paused: no gameplay change targets it

## Examples

**Correct** (values from a ScriptableObject, game time):

```csharp
float damage = combatConfig.BaseDamage;
float speed = stats.MovementSpeed * deltaTime;
bool expired = GameTimeManager.Instance.GetGameTime() >= nextWaveTime;
```

**Incorrect** (hardcoded, real clock):

```csharp
float damage = 25f;
bool expired = Time.realtimeSinceStartup >= nextWaveTime;
```
