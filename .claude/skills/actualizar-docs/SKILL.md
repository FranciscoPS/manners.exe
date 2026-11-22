---
name: actualizar-docs
description: "Updates manners.exe's documentation when the user asks: README.md, the project's own docs in Docs/ (UI_AUTHORING.md, OVERRIDE_COMBINATIONS.md, Performance/), the HTML manual published as an Artifact, and the Confluence pages through /atlassian-sync push. Only the user starts it; nothing else updates that documentation."
argument-hint: "[all | readme | docs | manual | confluence | <path>]"
user-invocable: true
disable-model-invocation: true
allowed-tools: Read, Glob, Grep, Write, Edit, AskUserQuestion, Artifact, Skill(artifact-design), Skill(atlassian-sync), Bash(git log*), Bash(git diff*), Bash(git status*), Bash(git show*), Bash(bash .claude/scripts/atlassian-mirror.sh *)
---

# Actualizar docs

The only route by which manners.exe's documentation is updated (CLAUDE.md, «Cómo trabajar
en este proyecto»). `disable-model-invocation: true` means Claude cannot start this skill:
the user types `/actualizar-docs`. No other skill, hook, agent or end-of-task step writes or
republishes the documentation below, even when it would help and even after "continúa". A
task that leaves it stale says so in one line at the end and names this command.

Talk to the user in Spanish. Every question uses the question tool; a skipped or dismissed
question is not an answer: write nothing and ask again in plain text. The approval question
in Phase 3 is asked whatever `modes.automation` is.

## The documentation

| Target | Files | How it is updated |
|---|---|---|
| `readme` | `README.md` | Edit, section by section |
| `docs` | `Docs/UI_AUTHORING.md`, `Docs/OVERRIDE_COMBINATIONS.md`, `Docs/Performance/**` | Edit, section by section |
| `manual` | Artifact «Manual manners.exe», https://claude.ai/artifact/1iDX84eqqdWnbwnc1pxm62 | Artifact read, edit, publish to the same URL |
| `confluence` | Confluence mirrors with local changes (`design/`, `docs/architecture/`, `production/`) | `/atlassian-sync push`, which asks again before publishing |

Not documentation for this skill, and never touched by it:

- what the framework's skills write when the user runs them: GDDs, ADRs, the systems index,
  the adoption plan, the registries and `production/session-state/`;
- the framework's guides in `Docs/` (`CLAUDE.md`, `WORKFLOW-GUIDE.md`,
  `COLLABORATIVE-DESIGN-PRINCIPLE.md`, `skill-flow-diagrams.md`, `engine-reference/`), the
  root `CLAUDE.md` and `.claude/`;
- the devlogs in `Docs/Devlog/`: they are written when the user asks for one, not updated.

**Verdicts** (one, at the end): `COMPLETE` — the approved changes were written;
`UP TO DATE` — every target in scope was read and nothing is stale; `NOT ASSESSED` — a
target could not be read or compared (named, with the reason). `NOT ASSESSED` outranks the
other two: a run that could not read part of its scope has not shown that part is current.

## Phase 1: Parse Arguments

Blank or `all` covers every target; `readme`, `docs`, `manual` or `confluence` covers that
one; a path covers that file, which must be in the table above. For a path outside it, say
it is not project documentation for this command and stop.

## Phase 2: Find What Changed

For each target in scope:

1. **Baseline**: the date of the last commit that touched the file
   (`git log -1 --format=%ad --date=iso -- <path>`); for the manual, its `updated` date in
   `Artifact` with `action: list`. A file with uncommitted changes takes today as its
   baseline, and the report says so.
2. **Changes since**: `git log --since=<baseline> --name-status --format='%h %ad %s'
   --date=short -- Assets ProjectSettings Packages`, plus `git status --porcelain` for what
   is not committed. Keep the files each document talks about: `README.md` ←
   `Assets/Scripts/Core/` and the patterns and rules it lists; `UI_AUTHORING.md` ←
   `Assets/Scripts/UI/`, `Assets/Prefabs/UI/`, `UIStyleApplier` and `Tools > Manners > UI`;
   `OVERRIDE_COMBINATIONS.md` ← `Assets/Scripts/Overrides/` and
   `Assets/Configurations/*/Overrides/`; `Performance/` ← the performance scripts, captures
   and graphics settings. Map 2 (`MilitaryBase`, `L2*`) is paused: changes there never make a
   document stale.
3. **Read the document by section**: Grep its `^#` headings, then Read those ranges. Check
   every statement that names code, an asset, a menu, a path, a value or a test against the
   current code. A statement is stale only when the code contradicts it; cite `file:line`.
   A new feature the document does not cover is a proposal, not a correction.
4. **Manual**: `Artifact` with `action: read` and the manual's URL, then the saved file it
   names; check it the same way.
5. **Confluence**: `bash .claude/scripts/atlassian-mirror.sh list` and `… unmapped`. The
   rows with `local=changed` and the unmapped files are what a push would publish.

A target that cannot be read (no git history, the Artifact read fails, the script is
missing) is `NOT ASSESSED — <reason>`; the others go on.

## Phase 3: Propose the Changes

Show, per target, a table: section · what it says now · what the code says (`file:line`) ·
proposed text. Also list the sections checked with nothing stale, so the user sees what was
read. Measured values (FPS, ms, memory, draw calls, build sizes) are never estimated: a value
only a new measurement could update is listed as `requiere medición` and left as it is.

Ask once with the question tool: "¿Actualizo estos [n] documentos?" — `Actualizar todo` /
`Elegir cuáles` / `No actualizar`. This is the "May I write" for the batch: nothing is
written before it, in any automation mode. `Elegir cuáles` asks again with one option per
document; `No actualizar` ends the run with nothing written.

## Phase 4: Write the Local Documents

- Edit section by section; never rewrite a whole file. Keep each document's language
  (Spanish), tone, headings and links, and add a new section only when the user approved it
  in Phase 3.
- Keep the project's rules while writing: the secret override combinations stay out of
  anything a player reads (internal documents may describe them), and Map 2 stays out.
- After each file, re-read the edited sections.

## Phase 5: Publish the Manual and Confluence

- **Manual**: load the `artifact-design` skill before editing the page. Edit the saved file
  from Phase 2, publish it with `Artifact` to the manual's URL (never as a new artifact) and
  read it back. A publish that fails leaves the manual as it was; report the error.
- **Confluence**: ask "¿Publico en Confluence los [n] espejos con cambios?" —
  `Sí, revisar el push` / `Ahora no`. On yes, run `/atlassian-sync push` with those paths
  through the Skill tool: it shows its own changeset and asks again before writing to
  Confluence. This skill never writes to Confluence directly.

## Phase 6: Report and Next Step

Report, in Spanish, at most 15 lines:

```
Verdict: [COMPLETE | UP TO DATE | NOT ASSESSED]
Actualizado:       [archivos y, si aplica, el manual (URL)]
Sin cambios:       [documentos revisados sin nada desactualizado]
Requiere medición: [valores que no se tocaron porque necesitan una medición nueva]
Sin evaluar:       [objetivo — motivo]
Confluence:        [publicado n | pendiente | nada pendiente]
```

The files changed on disk are not committed; offer a commit only when the user asks for it.
No next step is required: the user runs `/actualizar-docs` again whenever they want the
documentation brought up to date.
