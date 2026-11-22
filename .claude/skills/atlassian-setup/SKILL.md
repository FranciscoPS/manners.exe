---
name: atlassian-setup
description: "Connects manners.exe to Jira (project MAN, board 34) and Confluence (folder manners.exe): discovers statuses, fields, sprints and pages, maps them to the framework's files, pulls everything and completes the project setup from that content. Run once, or after the board or the folder changes."
argument-hint: "[full | jira | confluence | config]"
user-invocable: true
allowed-tools: Read, Glob, Grep, Write, Edit, AskUserQuestion, Skill, Bash(bash .claude/scripts/atlassian-mirror.sh *), Bash(bash .claude/scripts/config-set.sh *), Bash(bash .claude/scripts/set-stage.sh *), Bash(bash .claude/scripts/project-state.sh*), Bash(bash "*/.claude/skills/atlassian-setup/../../hooks/yaml-helper.sh" resolve_config *), mcp__atlassian__getAccessibleAtlassianResources, mcp__atlassian__atlassianUserInfo, mcp__atlassian__discover, mcp__atlassian__executeRead, mcp__atlassian__getContentFormatGuide, mcp__atlassian__getJiraIssue, mcp__atlassian__searchJiraIssuesUsingJql, mcp__atlassian__getConfluenceContent, mcp__atlassian__searchConfluence
---

!`bash "${CLAUDE_SKILL_DIR}/../../hooks/yaml-helper.sh" resolve_config --keys automation,automation_always_ask,workflow,review_mode`

Resolved above — use as-is. If the block did not render, read `project.yaml` directly.

# Atlassian Setup

Brings manners.exe's Jira and Confluence into the framework: the team's stories and
documents become the local files the framework's skills read, and the project's
configuration is completed from them instead of from a questionnaire. Formats, tools
and safe-write rules: read `.claude/docs/atlassian-mapping.md` now, once.

**Reads** Jira and Confluence (read tools only, no permission prompts). **Writes**
`production/atlassian.yaml`, the mirror files (through `/atlassian-sync pull`) and
`project.yaml` keys (through `config-set.sh` / `set-stage.sh`), each after a yes.
**Never writes** to Jira or Confluence: publishing is `/atlassian-sync push`.

Talk to the user in Spanish. Every question uses the question tool; a skipped or
dismissed question is not an answer: write nothing and ask again in plain text.

**Verdicts** (one, at the end): `COMPLETE` — everything reachable was mapped, pulled
and configured; `CONCERNS` — done, with gaps the user accepted (listed);
`NOT ASSESSED` — part of Jira or Confluence could not be read (named, with the reason);
`BLOCKED` — no connection, nothing done. `NOT ASSESSED` outranks `COMPLETE` and
`CONCERNS`; `BLOCKED` outranks all.

## Phase 1: Parse Arguments and Check the Connection

`$ARGUMENTS`: blank or `full` runs Phases 2–7. `jira` runs 2, 3, 5 (Jira only), 6, 7.
`confluence` runs 2, 4, 5 (Confluence only), 6, 7. `config` runs only Phases 6–7 from
the mirrors already on disk.

1. Read `production/atlassian.yaml`, `project.yaml` and
   `.claude/docs/atlassian-mapping.md`. Missing `production/atlassian.yaml` →
   `BLOCKED — production/atlassian.yaml not found`; it ships with the project.
2. Unless the mode is `config`: check that `mcp__atlassian__*` tools exist. None →
   `BLOCKED — Atlassian MCP not connected` with the sign-in steps from the mapping doc
   (`/mcp` → `atlassian` → sign in; restart Claude Code if it is not listed). Stop.
3. If `setup.completed` is set, say so and that this run refreshes the mapping; keep
   every existing mapping unless the user changes it.

## Phase 2: Site and Access

1. `getAccessibleAtlassianResources` → the cloudId of `site.url`. Not in the list →
   `BLOCKED — this account has no access to blackdoorstudios.atlassian.net`.
2. `atlassianUserInfo` → who is signed in; say it in one line (name only).
3. Probe each target and record `ok` or `NOT ASSESSED — <reason>`:
   - Jira project `jira.project_key` (`searchJiraIssuesUsingJql` with
     `project = MAN ORDER BY updated DESC`, one result)
   - Board `jira.board_id` (`getJiraBoardConfig` through `discover`/`executeRead`)
   - Confluence folder `confluence.root_folder_id` (`getConfluenceContent`)
4. Keep the cloudId for Phase 5's write of `site.cloud_id`.

## Phase 3: Jira Discovery

Read, do not write yet:

1. **Issue types** of the project (`listJiraProjectIssueTypesMetadata`) → propose
   `jira.issue_types` (`Epic` → `epic`, `Historia`/`Story` → `story`, `Tarea`/`Task` →
   `task`, `Error`/`Bug` → `bug`, `Subtarea`/`Sub-task` → `subtask`). An unknown type →
   ask what it is.
2. **Statuses and board columns** (`getJiraBoardConfig`, `listJiraStatuses`) →
   propose `jira.status_map` onto `Not Started | Ready | In Progress | In Review |
   Complete | Blocked`, column by column: first column → `Not Started` (or `Ready` when
   the board has a separate "Ready"/"Listo" column), the done category → `Complete`.
3. **Transitions**: for one open issue per type, `listJiraIssueTransitions` → propose
   `jira.transitions` (framework status → transition name). A status no transition
   reaches is listed as `not reachable`, not guessed.
4. **Fields** (`getJiraIssueTypeMetaWithFields` for story and bug) → ids for story
   points or estimate, sprint, parent/epic link, and an acceptance-criteria field if the
   team has one → `jira.fields`.
5. **Labels and components** on the project's issues → propose `jira.type_labels` only
   for labels that clearly name a story type (`ui`, `vfx`, `balance`…); leave the rest
   unmapped, `/story-readiness` sets the type.
6. **Sprints** (`listJiraBoardSprints`): active, future, last closed — name, goal and
   dates. Two or more closed sprints → the usual length → propose
   `cadence.sprint_length` (`1w | 2w | 3w | 4w`, or `none` for a board without sprints).
7. **Versions** (`getJiraProjectVersions`): released and unreleased, with dates.
8. **Counts**: epics, stories and tasks, bugs, by status category.

Show the proposal as one table per key, with counts, and ask once:
"¿Guardo este mapeo de Jira en `production/atlassian.yaml`?" — `Guardarlo` /
`Cambiar algo` / `No guardar`. On `Guardarlo`, "May I write" is answered: write the
`jira` block and `site.cloud_id` with Edit, keeping comments.

## Phase 4: Confluence Discovery

1. List everything under `confluence.root_folder_id`, at any depth
   (`listConfluenceContent` / `getConfluenceContent` children, or `searchConfluence`
   with `ancestor = <root_folder_id>`), with id, type, title, version and last update.
   Pages the account cannot open are `NOT ASSESSED — no permission`, listed by title.
2. Read each page (body, or its first sections when it is long) and classify it with
   the table in the mapping doc, §3. Mark `rich: true` when it holds content that does
   not convert. Map 2 pages are mapped and flagged as paused.
3. Propose `confluence.pages` (id, title, local path, `origin: confluence`, `rich`) and
   `confluence.ignore` (id, title, reason) as one table, sorted by local path. Two pages
   that would land on the same path → propose a second path or a merge; ask.
4. Ask once: "¿Guardo este mapeo de Confluence?" — `Guardarlo` / `Cambiar algo` /
   `No guardar`. On `Guardarlo`, write the `confluence` block with Edit.

## Phase 5: First Pull

Invoke `/atlassian-sync pull all` with the Skill tool (or `pull jira` / `pull
confluence` in those modes). It converts and writes the mirrors, asks before writing
them, and stamps each one. Carry its per-item states into this skill's report; a
`NOT ASSESSED` item there is `NOT ASSESSED` here.

When the user declines the write, stop after Phase 7's report with verdict `CONCERNS`
and the reason: nothing was mirrored, so Phase 6 has no input.

## Phase 6: Project Setup from the Mirrors

Work out each value from the mirrored content, citing the file (and its Jira key or
Confluence page) it comes from; a value with no source stays unset and is listed as
`not found in Jira or Confluence`, never guessed.

| Key | Source |
|---|---|
| `project.goal` | concept, pitch or roadmap pages: selling, Steam, itch.io, publisher, event, portfolio — the `/start` Phase 4 table |
| `project.version` | the newest released Jira version, else the game-concept page |
| `project.stage` | `/start` Phase 4's Path D table, raised by Jira evidence: an active sprint of implementation stories and released versions → `Production`; a polish/release epic in progress with content complete → `Polish`. Never higher than the evidence; say why |
| `cadence.sprint_length`, `cadence.milestone_length` | Phase 3 sprints and versions |
| `platform.gamepad_support`, `platform.touch_support`, `platform.cert_tier`, `accessibility.target` | concept, technical or UX pages |
| `modes.rigor` | already set (`standard`): keep it; mention `full` only if the pages describe many interlocking systems and the user asks |

Show the card:

```
Lo que voy a configurar
  Etapa:    [stage]    ([por qué, con la fuente])
  Objetivo: [goal]     ([fuente] | no encontrado)
  Versión:  [version]  ([fuente] | no encontrado)
  Sprints:  [length]   ([n] sprints cerrados en el tablero 34)
  [una línea por cada otra clave encontrada]
  No encontrado: [claves sin fuente]
  /project-settings cambia cualquiera más adelante.
```

Ask: "¿Guardo esto en `project.yaml`?" — `Guardarlo` / `Cambiar algo` / `No guardar`.
On `Guardarlo`: `bash .claude/scripts/set-stage.sh "<stage>"` for the stage, then
`bash .claude/scripts/config-set.sh <key> <value>` for each other key, `--dry-run` first
when a key is new to the file. Check each read-back row. Replace the matching
`[TO BE CONFIGURED]` lines in `.claude/docs/technical-preferences.md` that the mirrors
answered (gamepad, touch), citing the source; ask "May I write" for that file in the
same question as the card.

## Phase 7: Report and Next Step

Write `setup.completed` (today's date) in `production/atlassian.yaml` when the verdict
is `COMPLETE` or `CONCERNS`. Append a short entry to
`production/session-state/active.md` (what was mapped, pulled and configured, what is
pending).

Report, in Spanish, at most 25 lines:

```
Verdict: [COMPLETE | CONCERNS | NOT ASSESSED | BLOCKED]
Jira:        [n] épicas · [n] historias/tareas · [n] bugs · sprint activo [name] ([n] historias)
Confluence:  [n] páginas reflejadas · [n] ignoradas · [n] con contenido que no se convierte
project.yaml: [keys written]
No evaluado: [item — reason], …
Pendiente:   [gaps the user accepted]
```

Recommended next step, one: `/adopt` (audits the format of the pulled GDDs and stories
and plans the gaps; its additions go back with `/atlassian-sync push`). After it:
`/sprint-status` for the active sprint, `/next-step` for the next story. Mention that
the new mirror files are uncommitted and can be committed when the user asks.
