---
paths:
  - "src/ai/**"
  - "Assets/**/{AI,Ai,ai}/**/*.cs"
  - "Source/**/{AI,Ai,ai}/**/*.{h,cpp}"
  - "Assets/Scripts/Enemy/**/*.cs"
---

# AI Code Rules

In manners.exe enemy AI is horde behaviour: hundreds of pooled enemies chasing the
player (`EnemyController`), kept apart by `EnemySeparationManager` and
`EnemyProximityGrid`, spawned in waves by `EnemySpawnManager`.

- AI update budget: 2 ms per frame for the whole horde at the late-match peak — profile to verify (`Tools > Manners > Performance` CSV capture)
- Cost scales with the horde: no per-enemy allocation, no per-enemy physics query without `NonAlloc`, neighbour lookups through the proximity grid, updates through `UpdateManager`
- All AI parameters are tunable from ScriptableObjects (`EnemyConfiguration`, `WaveData`, `GameBalanceConfig`): speeds, separation radii, spawn curves, timers
- AI must be debuggable: draw gizmos for grids, radii and targets in the editor; no logging per enemy per frame
- AI telegraphs intentions — players need time to read and react (`SpawnWarningIndicator`)
- Prefer simple, data-driven steering over long if/else chains; formations, flanking and role assignment from the generic template apply only when a design doc asks for them
- Enemies return to the pool with their state reset; nothing keeps a reference to a despawned enemy
- `L2*` enemies belong to the paused Map 2: do not change them
