# manners.exe — Adaptations of Claude Code Game Studios

Read on demand, mainly when upgrading the framework. Installed: **v1.3.0**, template
commit `abc0de79d6ceb0b5d7aa693945849b3937afbe6a` (2026-11-09), on 2026-11-21.
License: MIT, `.claude/LICENSE`.

## What was installed

- `.claude/` complete (agents, skills, hooks, scripts, rules, docs, templates, genres,
  engines, `statusline.sh`, `settings.json`)
- `studio-tests/` (QA for the framework's own skills and agents)
- `docs/` of the template into the project's existing `Docs/` (the same folder on
  Windows and macOS): `CLAUDE.md`, `COLLABORATIVE-DESIGN-PRINCIPLE.md`,
  `WORKFLOW-GUIDE.md`, `skill-flow-diagrams.md`, `architecture/tr-registry.yaml`,
  `registry/architecture.yaml`, `engine-reference/README.md`, `engine-reference/unity/`
- `design/CLAUDE.md`, `design/registry/entities.yaml`
- `.worktreeinclude`; `.gitignore` and `.gitattributes` entries

## What was left out, and why

| Left out | Why |
|---|---|
| Agents `godot-*`, `ue-*`, `unreal-specialist` (10) | Unity-only project; they only added noise to the agent list. An upgrade with `git checkout template/main -- .claude` brings them back: delete them again |
| `docs/engine-reference/{godot,unreal,web}/` | Same reason |
| `src/` | Unity compiles only `Assets/` |
| `production/.gdignore` | Godot only |
| `README.md`, `CHANGELOG.md`, `UPGRADING.md`, `CONTRIBUTING.md`, `SECURITY.md`, `.github/` of the template | They describe the template repository. `/write-changelog` writes the game's own `docs/CHANGELOG.md`. Upgrade notes: the template's `UPGRADING.md` on GitHub |
| `docs/migration-guide-v1.1.md` | Only for projects coming from v1.0 |

## Framework files changed for this project (merge by hand on upgrade)

| File | Change |
|---|---|
| `CLAUDE.md` | Project rules first, then the framework sections; Unity stack; `guided` collaboration wording; Jira and Confluence section |
| `project.yaml` | Project values (engine, specialists, naming, performance, commands, modes) |
| `.claude/docs/coding-standards.md` | No C# comments, project patterns instead of DI, Jira keys in commits, tests in `Assets/Scripts/Editor/`, CI = Unity Build Automation |
| `.claude/docs/directory-structure.md` | Unity layout of this repository |
| `.claude/docs/technical-preferences.md` | Filled for Unity and this project; forbidden patterns and allowed libraries from `CLAUDE.md` and the README |
| `.claude/rules/engine-code.md`, `gameplay-code.md`, `ai-code.md`, `ui-code.md` | Paths extended to `Assets/Scripts/<System>/`; content follows the README patterns |
| `.claude/rules/test-standards.md`, `shader-code.md`, `scene-files.md` | Project test style, WebGL/URP shaders, Unity scene rules with `Tools > Manners` |
| `.claude/agents/unity-*.md` (5) | A `## manners.exe` section before `## Core Responsibilities` |
| `Docs/engine-reference/unity/VERSION.md` | `Installed at pin time` and `Project Pinned` rows |

`.claude/settings.json` is the template's, unmodified: it sets `"defaultMode": "default"`
(sessions in this project start in ask-before-acting mode) and the deny rules
(`rm -rf`, force push, `git reset --hard`, `git clean -f`, recursive `Remove-Item`, `.env`).

## Added by the project (an upgrade never touches them)

- `.mcp.json` — Atlassian Rovo MCP server (`https://mcp.atlassian.com/v2/mcp`, OAuth)
- `production/atlassian.yaml` — Jira and Confluence ids and mappings
- `.claude/skills/atlassian-setup/`, `.claude/skills/atlassian-sync/`
- `.claude/skills/actualizar-docs/` — the only route that updates the project documentation
  (README, the project's docs in `Docs/`, the Artifact manual, Confluence); it sets
  `disable-model-invocation: true`, so only the user can start it
- `.claude/scripts/atlassian-mirror.sh` — mirror headers (observations + `stamp`)
- `.claude/rules/atlassian-mirror.md`, `.claude/docs/atlassian-mapping.md`
- this file

## Upgrading

Use the template's Strategy A2 (selective checkout; the repositories share no history):

```bash
git remote add template https://github.com/Donchitos/Claude-Code-Game-Studios.git   # once
git fetch template main
git checkout template/main -- .claude
git checkout HEAD -- <each file in the "changed" table above>   # then re-apply new upstream changes by hand
```

Then delete the Godot and Unreal agents again, diff `CLAUDE.md` and `project.yaml` by
hand (`git diff HEAD template/main -- CLAUDE.md`), update `framework.version` in
`project.yaml`, and check the project's own skills still lint:
`bash .claude/scripts/skill-lint.sh atlassian-setup`, `… atlassian-sync` and `… actualizar-docs`.
