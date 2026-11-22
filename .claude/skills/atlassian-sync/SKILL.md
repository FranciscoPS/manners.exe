---
name: atlassian-sync
description: "Keeps the local mirrors of Jira and Confluence in step: status compares both sides, pull brings remote changes down, push publishes local stories, docs, status changes and QA evidence after confirmation. Run it when the user asks; Confluence pages are pushed only when the user types /atlassian-sync push or approves it inside /actualizar-docs."
argument-hint: "[status | pull [all|jira|confluence|<path>] | push [all|jira|confluence|<path>]]"
user-invocable: true
allowed-tools: Read, Glob, Grep, Write, Edit, AskUserQuestion, Bash(bash .claude/scripts/atlassian-mirror.sh *), Bash(git status*), Bash(git diff*), mcp__atlassian__getAccessibleAtlassianResources, mcp__atlassian__atlassianUserInfo, mcp__atlassian__discover, mcp__atlassian__executeRead, mcp__atlassian__getContentFormatGuide, mcp__atlassian__getJiraIssue, mcp__atlassian__searchJiraIssuesUsingJql, mcp__atlassian__getConfluenceContent, mcp__atlassian__searchConfluence
---

!`bash "${CLAUDE_SKILL_DIR}/../../hooks/yaml-helper.sh" resolve_config --keys automation,automation_always_ask`

Resolved above — use as-is. If the block did not render, read `project.yaml` directly.

# Atlassian Sync

Jira (project `MAN`) and Confluence (folder `manners.exe`) are the source of truth; the
files under `design/`, `docs/architecture/` and `production/` are the mirrors the
framework's skills read. This skill compares the two sides and moves changes in the
direction the user approves. Formats, tools, sync states and safe-write rules: read
`.claude/docs/atlassian-mapping.md` now, once.

Talk to the user in Spanish. Every question uses the question tool; a skipped or
dismissed question is not an answer: write nothing and ask again in plain text.
Writing to Jira or Confluence publishes to the whole team: it runs only after the user
says yes to that exact batch, whatever `modes.automation` is (`external_calls`).

**Verdicts** (one, at the end): `IN SYNC` — both sides match; `SYNCED` — the approved
changes were applied; `CONFLICTS` — items changed on both sides are waiting for a
decision; `NOT ASSESSED` — items that could not be read or compared (named, with the
reason); `BLOCKED` — no connection or no setup, nothing done. `CONFLICTS` outranks
`NOT ASSESSED`, which outranks `SYNCED` and `IN SYNC`; `BLOCKED` outranks all.

## Phase 1: Parse Arguments and Check Prerequisites

`$ARGUMENTS`: blank or `status` → compare and report, write nothing. `pull` / `push`,
followed by `all` (default), `jira`, `confluence` or one file path.

1. Read `production/atlassian.yaml` and `.claude/docs/atlassian-mapping.md`.
2. No `mcp__atlassian__*` tools → `BLOCKED — Atlassian MCP not connected`, with the
   sign-in steps from the mapping doc §1. Stop.
3. `setup.completed` empty, or no `jira.status_map` / `confluence.pages` →
   `BLOCKED — run /atlassian-setup first`, unless this run was started by
   `/atlassian-setup` itself (it maps first, then calls `pull all`).
4. `getAccessibleAtlassianResources` → cloudId; it must match `site.cloud_id`.

## Phase 2: Local Inventory

Run `bash .claude/scripts/atlassian-mirror.sh list` and
`bash .claude/scripts/atlassian-mirror.sh unmapped`. Keep every row: path, source,
id, `remote_version`, local state. An `absent` folder is reported as absent, never as
"nothing to sync". A file with a header that does not parse (`unclosed`) is
`NOT ASSESSED — header`.

## Phase 3: Remote Inventory

Read, in as few calls as the tools allow:

- **Jira**: `searchJiraIssuesUsingJql` with `project = <project_key> ORDER BY rank`,
  fields key, summary, issuetype, status, updated, parent, sprint, the estimate field,
  labels, components, issuelinks; page through all results. Then the active sprint of
  `jira.board_id` (`getJiraBoardSprintData` / `listJiraBoardSprints`).
- **Confluence**: everything under `confluence.root_folder_id` with id, title, type and
  version number (as in `/atlassian-setup` Phase 4).

Anything that fails or returns no permission is `NOT ASSESSED — <reason>` for those
items; the rest go on.

## Phase 4: Compare

Give every item one state from the mapping doc §4 (`IN SYNC`, `REMOTE AHEAD`,
`LOCAL AHEAD`, `CONFLICT`, `REMOTE MISSING`, `LOCAL ONLY`, `NOT ASSESSED`), plus:

- issues in Jira with no local file → `REMOTE NEW` (pull creates them)
- pages under the folder in neither `confluence.pages` nor `confluence.ignore` →
  `REMOTE NEW` (pull proposes a path; never placed silently)
- `LOCAL ONLY` files → where push would create them: a story under its epic in Jira, a
  design or architecture page in Confluence

Show one table, grouped by state, with path, Jira key or page title, and what changed.
In `status` mode stop here: go to Phase 7.

## Phase 5: Pull

Applies to `REMOTE AHEAD` and `REMOTE NEW` items in scope.

1. For each `REMOTE NEW` page, propose a path from the mapping doc §3 (or `ignore`).
2. For each `CONFLICT`, show the remote change and the local change side by side and
   ask: `Quedarme con Jira/Confluence` / `Publicar lo local` (moves it to Phase 6) /
   `Combinar` (write a merged file, then push it) / `Dejarlo para después`.
3. Ask once: "¿Escribo estos [n] archivos desde Jira y Confluence?" listing the
   paths, plus the new entries for `confluence.pages`/`ignore` — this is the
   "May I write" for the batch. `LOCAL AHEAD` files are never overwritten by a pull.
4. Write each file in the framework format (mapping doc §2–3), then stamp it:
   `bash .claude/scripts/atlassian-mirror.sh stamp <path> source=<jira|confluence>
   id=<key or page id> kind=<kind> title=<title> url=<url>
   remote_version=<version or updated> pulled_at=<now, UTC ISO 8601>`.
   Check the `stamped … local=unchanged` row; anything else is reported.
5. Rebuild `production/sprint-status.yaml` and `production/sprints/sprint-<NNN>.md`
   from the active sprint, then stamp them. Stories that left the sprint keep their
   files.
6. Add new page mappings to `production/atlassian.yaml` with Edit.

## Phase 6: Push

Applies to `LOCAL AHEAD` and `LOCAL ONLY` items in scope, and conflicts the user chose
to publish.

1. Build the changeset, per item: the Jira issue to create or edit (fields, the
   description part below the marker, a status transition through `jira.transitions`,
   evidence files from `production/qa/evidence/` to attach and the comment), or the
   Confluence page to create or update (for team-written pages: the sections that
   change, as a diff; `rich: true` pages get an appended section or child page, per the
   mapping doc §5).
2. Show the whole changeset and ask once: "¿Publico estos [n] cambios en Jira y
   Confluence?" — `Publicar todo` / `Elegir cuáles` / `No publicar`. A whole-page
   overwrite of a team-written page needs its own explicit yes after its diff.
3. Run the approved writes one at a time. After each success, stamp the local file
   with the new `id` (for created items), `remote_version` and `pushed_at`; record a
   created page in `confluence.pages` with `origin: local`. A failure leaves that file
   and its header as they were; report it and ask before continuing.
4. Never delete in Jira or Confluence; a local file that was removed is reported, not
   mirrored as a deletion.

## Phase 7: Report and Next Step

Report, in Spanish, at most 25 lines:

```
Verdict: [IN SYNC | SYNCED | CONFLICTS | NOT ASSESSED | BLOCKED]
Traído:     [n] archivos ([paths or counts by folder])
Publicado:  [n] cambios ([MAN-… created/updated, pages created/updated])
Pendiente:  [n] conflictos · [n] solo locales · [n] sin evaluar ([reasons])
```

Then the next step: after a pull, `/sprint-status` or `/next-step`; after `/adopt`
or a retrofit, `/actualizar-docs confluence` (Confluence is published only when the user
asks); with conflicts, `/atlassian-sync pull <path>`
once decided. The mirror files changed on disk are not committed: offer a commit
(message with the Jira keys involved) only when the user asks for it.
