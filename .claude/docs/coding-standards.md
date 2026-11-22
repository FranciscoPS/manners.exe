# Coding Standards

These are the framework's standards as adapted to manners.exe. Where a framework
skill, agent or template says something different, this file, `CLAUDE.md` and the
README win (list of adaptations: `.claude/docs/manners-adaptations.md`).

- **No comments in C# code.** Intent lives in names, in the README ("Patrones de
  diseño principales") and in the design docs. `[Tooltip]` and `[Header]` in Spanish
  are used on serialized fields, because balance is tuned from the Inspector. A
  framework instruction to add doc comments, usage examples or "which design doc this
  implements" comments does not apply here.
- Architecture decisions are recorded as ADRs in `docs/architecture/` (a Confluence
  mirror, see `.claude/rules/atlassian-mirror.md`). **How many is set by
  `modes.workflow`** — all ADRs at `full`, *critical* ADRs only at `standard` (this
  project), none required at `minimal`. See `.claude/docs/workflow-modes.md`.
- Gameplay values are data-driven: every tunable number and its visual live in one
  ScriptableObject (`Assets/Configurations/`, `_Production` and `_Sandbox` copies),
  never hardcoded.
- Follow the project's patterns instead of generic alternatives: per-object updates
  register in `UpdateManager` (`IUpdateable` / `IFixedUpdateable` / `ILateUpdateable`),
  frequent objects come from `PoolManager` / `SpawnFactory`, global systems are the
  auto-created singletons with `ResetStatics`, and systems talk through `GameEvents`.
  "Dependency injection over singletons" from the generic template does not apply.
  Keep logic that needs tests in plain methods or classes an EditMode test can call.
- No allocations per frame on hot paths: reusable buffers, `Physics.*NonAlloc`,
  cached references. Match logic uses `GameTimeManager.GetGameTime()`, not the real clock.
- **Commits, branches and PRs reference the Jira key** of the story or bug
  (`MAN-123`), so Jira links them. The repo does not use Conventional Commits: write
  a short, plain summary in the language of the change.
- **Verification-driven development**: write tests first when adding gameplay logic
  that can be tested in EditMode. For UI changes, verify with captures (`Tools >
  Manners > UI > Capturar inglés y español`) in both languages. Compare expected
  output to actual output before marking work complete.
- **A parse check is not a run.** Every story that changes something
  player-observable is launched and observed before it closes, and the
  observation is a retained screenshot in `production/qa/evidence/`, attached to the
  Jira story by `/atlassian-sync push`. Procedure per engine:
  `.claude/docs/run-and-observe.md`. Not waived at `qa.level: minimal` — tests are,
  the look is not.
- Map 2 (`MilitaryBase`, `L2*` prefabs) is paused: no code, test, tool or capture
  touches it until the user reactivates it.

# Design Document Standards

- All design docs use Markdown locally; the team reads and edits them in
  Confluence. Local files under `design/` are mirrors kept by `/atlassian-sync`.
- Each mechanic has a dedicated document in `design/gdd/`
- **How many of the 8 sections are required depends on `modes.workflow`** —
  all 8 at `full`, 5 (+ Formulas when the system defines numeric rules — rates,
  curves, thresholds, costs; the system's `Category` is a hint, not the test) at
  `standard` (this project), and no
  GDD at all at `minimal`, where `design/game-brief.md` is the design record.
  See `.claude/docs/workflow-modes.md`. The 8 sections:
  1. **Overview** -- one-paragraph summary
  2. **Player Fantasy** -- intended feeling and experience
  3. **Detailed Rules** -- unambiguous mechanics
  4. **Formulas** -- all math defined with variables
  5. **Edge Cases** -- unusual situations handled
  6. **Dependencies** -- other systems listed
  7. **Tuning Knobs** -- configurable values identified
  8. **Acceptance Criteria** -- testable success conditions
- Balance values must link to their source formula or rationale
- Secret override combinations (`OverrideCombatResolver`) may be documented in
  design docs, never in player-facing text.

# Testing Standards

## Test Evidence by Story Type

All stories must have appropriate test evidence before they can be marked Done:

| Story Type | Required Evidence | Location | Default Gate Level |
|---|---|---|---|
| **Logic** (formulas, AI, state machines) | Automated EditMode test — must pass | `Assets/Scripts/Editor/[System]Tests.cs` | BLOCKING |
| **Integration** (multi-system) | Integration test OR documented playtest | `Assets/Scripts/Editor/[System]Tests.cs` (a `[UnityTest]` that enters Play Mode) or `production/qa/` | BLOCKING |
| **Visual/Feel** (animation, VFX, feel) | Retained screenshot + lead sign-off | `production/qa/evidence/` | BLOCKING |
| **UI** (menus, HUD, screens) | Retained screenshot of each screen touched, in English and Spanish | `production/qa/evidence/` | BLOCKING |
| **Config/Data** (balance tuning) | Smoke check pass | `production/qa/smoke-[date].md` | ADVISORY |

The **Default Gate Level** applies when `testing.strict` is not set in
`project.yaml`. A project may override it per test type: `testing.strict.logic`,
`.integration`, `.visual`, `.ui`, and `.config` each take `true` (BLOCKING) or
`false` (ADVISORY). `/story-done`, `/story-readiness`, `/dev-story`,
`/gate-check`, and `/smoke-check` resolve the effective level from that setting,
falling back to the defaults above when it is absent.

> **Why Visual and UI block.** "Retained" is load-bearing — the screenshot must
> still be on disk in `production/qa/evidence/` when the story is closed. One
> that was captured and discarded is an assertion, not evidence. These two rows
> block because for a game the way it looks *is* the product: an advisory visual
> gate gets deferred in favour of whatever does block, and the result is a
> well-tested game that looks wrong.

> **Exception — `/smoke-check`.** The ADVISORY default for **Config/Data** above
> applies to *per-story evidence* gates. `/smoke-check` is a build-health gate,
> not a per-story evidence gate, so its own unset default for
> `testing.strict.config` is **BLOCKING**. The two defaults differ on purpose.

## Automated Test Rules

- **Naming**: `.claude/rules/test-standards.md`
- **Determinism**: Tests must produce the same result every run — no random seeds, no time-dependent assertions
- **Isolation**: Each test sets up and tears down its own state; tests must not depend on execution order.
  PlayerPrefs are shared with the real game (same company and product): save and restore every key a test touches
- **No hardcoded data**: Test fixtures use constant files or factory functions, not inline magic numbers
  (exception: boundary value tests where the exact number IS the point)
- **Independence**: Unit tests do not call external APIs, databases, or file I/O — use dependency injection

## What NOT to Automate

- Visual fidelity (shader output, VFX appearance, animation curves)
- "Feel" qualities (input responsiveness, perceived weight, timing)
- Platform-specific rendering (test on target hardware, not headlessly)
- Full gameplay sessions (covered by playtesting, not automation)

## CI/CD Rules

- Builds: `.github/workflows/build.yml` drives Unity Build Automation for Windows, Mac
  and Web (on a published release, or by hand with `validate_only` to check targets
  without spending build minutes). It does not run the EditMode tests.
- Tests run locally with `commands.test` (`project.yaml`) with the Unity Editor
  closed, or from Window > General > Test Runner. `/smoke-check` runs them.
- Never disable or skip failing tests to make a check pass — fix the underlying issue
