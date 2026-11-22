# Technical Preferences

<!-- project.yaml at the repo root is the machine-readable source of truth for
     engine, specialists, naming, performance, platform, and testing.framework.
     This file is the human-readable LEGACY FALLBACK: agents and skills resolve
     each key from project.yaml first and fall back here only when the
     project.yaml key is absent. /setup-engine dual-writes both.
     Forbidden patterns and allowed libraries are NOT migrated — they live only
     in this file. Populated by /setup-engine; updated as decisions are made. -->

## Engine & Language

- **Engine**: Unity 6000.3.5f2 (Unity 6.3 LTS)
- **Language**: C#
- **Rendering**: URP 17.3 (toon shading: `com.unity.toonshader` and the project's own shaders in `Assets/Shaders/`)
- **Physics**: PhysX (3D), queried with `Physics.*NonAlloc`

## Input & Platform

<!-- Written by /setup-engine. Read by /ux-design, /ux-review, /test-setup, /team-ui, and /dev-story -->
<!-- to scope interaction specs, test helpers, and implementation to the correct input methods. -->

- **Target Platforms**: Web (WebGL, the main target) and desktop (Windows, macOS)
- **Input Methods**: Keyboard/Mouse (Input System 1.17, `Assets/InputSystem_Actions.inputactions`)
- **Primary Input**: Keyboard/Mouse
- **Gamepad Support**: [TO BE CONFIGURED — the actions asset has Gamepad bindings from Unity's template; confirm in Confluence during /atlassian-setup]
- **Touch Support**: [TO BE CONFIGURED — same as above]
- **Platform Notes**: Default output 1920×1080 fullscreen at 60 FPS; menu/pause 30 FPS; 15 FPS without focus. Graphics options in Options → Gráficos and Pausa → Gráficos. The event demo (`demo` branch, 5-minute balance) is shown on ultrawide 4K screens (3840×1600, 5120×2160).

## Naming Conventions

- **Classes**: PascalCase (`GameTimeManager`, `OverrideCombatResolver`)
- **Variables**: camelCase, private fields without prefix (`matchDurationMinutes`, `isGameActive`)
- **Signals/Events**: PascalCase `On<Event>` on `GameEvents` (`OnMatchTimeExpired`, `OnChestSpawned`)
- **Files**: PascalCase, one class per file, named after the class
- **Scenes/Prefabs**: PascalCase; configuration assets end in `_Production` or `_Sandbox`
- **Constants**: PascalCase (`MaxSlots`, `CheckInterval`); a few older SCREAMING_SNAKE keys remain

## Performance Budgets

- **Target Framerate**: 60 FPS in a match (30 in menus and pause)
- **Frame Budget**: 16.67 ms
- **Draw Calls**: [TO BE CONFIGURED — no budget defined; see Docs/Performance/]
- **Memory Ceiling**: [TO BE CONFIGURED — no budget defined; WebGL is the tightest target]

## Testing

- **Framework**: Unity Test Framework 1.6 (NUnit), EditMode tests in `Assets/Scripts/Editor/*Tests.cs`
- **Minimum Coverage**: [TO BE CONFIGURED]
- **Required Tests**: Balance formulas, gameplay systems, override combinations (`OverrideCombinationTests`), UI authoring and both languages (`UIAuthoringTests`), WebGL build regressions (`WebBuildRegressionTests`)

## Forbidden Patterns

<!-- Add patterns that should never appear in this project's codebase -->
Source: `CLAUDE.md` and the README. `/code-review` checks these.

- `Update()` / `FixedUpdate()` / `LateUpdate()` on individual objects — register in `UpdateManager` instead
- Per-frame allocations on hot paths: `new` collections, LINQ, string building, `GetComponent` in loops, non-`NonAlloc` physics queries
- `Instantiate` / `Destroy` for objects that appear often (enemies, projectiles, pickups, VFX) — use `PoolManager` / `SpawnFactory`
- Match timers on `Time.time` or the real clock — use `GameTimeManager.GetGameTime()`
- Hardcoded gameplay or balance values — they go in the system's ScriptableObject
- Comments in C# code (only `[Tooltip]` and `[Header]`, in Spanish)
- UI hierarchy built at runtime, or references found by name (`GameObject.Find`, `transform.Find`) instead of serialized fields
- Player-visible text that bypasses `LocalizedText` / `LocalizedString`
- Player-visible text that mentions secret override combinations
- Changing `OverrideData.PersistentId`
- The words synergy/sinergia in code, assets or menus (they are overrides / sobrecargas)
- Editing `.unity` / `.prefab` / `.asset` YAML by hand while the Unity Editor has the project open
- Moving balance between Sandbox and Production by deleting and pasting assets — only `Tools > Manners > Sandbox > 4`
- Renaming a serialized field without `[FormerlySerializedAs]`, or moving an asset without its `.meta`
- Any change to Map 2 (`MilitaryBase`, `L2*` prefabs) while it is paused

## Allowed Libraries / Addons

<!-- Add approved third-party dependencies here -->
- Unity packages in `Packages/manifest.json`: URP 17.3, Input System 1.17, Cinemachine 3.1, AI Navigation 2.0, Timeline 1.8, Terrain Tools 5.3, Toon Shader 0.15 (preview), uGUI 2.0 with TextMesh Pro, Unity Test Framework 1.6
- DOTween (Demigiant), for UI transitions
- No Addressables, DOTS/Entities or UI Toolkit: adopting one is an architecture decision (ADR) the user approves first

## Architecture Decisions Log

<!-- Quick reference linking to full ADRs in docs/architecture/ -->
- [No ADRs yet — /atlassian-setup brings the technical pages from Confluence; new ones via /architecture-decision]

## Engine Specialists

<!-- Written by /setup-engine when engine is configured. -->
<!-- Read by /code-review, /architecture-decision, /architecture-review, and team skills -->
<!-- to know which specialist to spawn for engine-specific validation. -->

- **Primary**: unity-specialist
- **Language/Code Specialist**: unity-specialist (C# review — primary covers it)
- **Shader Specialist**: unity-shader-specialist (HLSL `.shader` files, URP materials, toon pipeline)
- **UI Specialist**: unity-ui-specialist (uGUI Canvas prefabs, `UIStyle` kit, TextMesh Pro)
- **Additional Specialists**: unity-dots-specialist, unity-addressables-specialist (consulted for analysis only: the project uses neither)
- **Routing Notes**: Editor tooling (`Assets/Scripts/Editor/`, `Tools > Manners` menus) goes to tools-programmer with unity-specialist review; performance work to performance-analyst

### File Extension Routing

<!-- Skills use this table to select the right specialist per file type. -->
<!-- If a row says [TO BE CONFIGURED], fall back to Primary for that file type. -->

| File Extension / Type | Specialist to Spawn |
|-----------------------|---------------------|
| Game code (.cs files) | unity-specialist |
| Shader / material files (.shader, .hlsl, .shadergraph, .mat) | unity-shader-specialist |
| UI / screen files (Canvas prefabs under `Assets/Prefabs/UI/`, `Assets/Scripts/UI/`) | unity-ui-specialist |
| Scene / prefab / level files (.unity, .prefab) | unity-specialist |
| Native extension / plugin files (.dll, native plugins) | unity-specialist |
| General architecture review | unity-specialist |
