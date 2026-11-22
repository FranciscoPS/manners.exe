# Jira and Confluence mapping (manners.exe)

How the framework's local artifacts map to Jira and Confluence. Read on demand by
`/atlassian-setup` and `/atlassian-sync`; ids and per-project choices live in
`production/atlassian.yaml`, the header format in `.claude/scripts/atlassian-mirror.sh --help`.

Jira and Confluence are the source of truth. The local files exist because every
framework skill reads local files in fixed formats. Talk to the user in their language
(Spanish for this team); file contents keep the language they came with.

## 1. The connection

The MCP server `atlassian` (`.mcp.json`, Atlassian Rovo MCP, `https://mcp.atlassian.com/v2/mcp`)
acts with the signed-in person's own Jira and Confluence permissions (OAuth, `/mcp`).

- **No `mcp__atlassian__*` tool available** → the server is not connected or not signed
  in. Stop with `NOT ASSESSED — Atlassian MCP not connected` and tell the user: run
  `/mcp`, choose `atlassian`, sign in with their Atlassian account in the browser, then
  run the skill again (restart Claude Code if the server is not listed). Never fall back
  to scraping URLs or asking for an API token.
- Call `getAccessibleAtlassianResources` first: it returns the `cloudId` for
  `blackdoorstudios.atlassian.net`. Pass that cloudId to every other tool.
- v2 shows a few **primary** tools directly (`getJiraIssue`, `searchJiraIssuesUsingJql`,
  `createJiraIssue`, `editJiraIssue`, `transitionJiraIssue`, `addOrEditJiraIssueComment`,
  `getConfluenceContent`, `searchConfluence`, `createConfluenceContent`,
  `updateConfluenceContent`, `discover`, `executeRead`, `executeWrite`). The rest are
  reached through `discover` + `executeRead` / `executeWrite`: `listJiraBoards`,
  `getJiraBoardConfig`, `listJiraBoardSprints`, `getJiraBoardSprintData`,
  `getJiraBoardIssueData`, `listJiraStatuses`, `listJiraIssueTransitions`,
  `listJiraProjectIssueTypesMetadata`, `getJiraIssueTypeMetaWithFields`,
  `getJiraProjectVersions`, `listJiraIssueComments`, `createJiraIssueLink`,
  `uploadAttachmentToJiraIssue`, `manageJiraSprint`, `listConfluenceContent`,
  `getConfluenceSpace`, `listConfluenceContentVersions`, `getContentFormatGuide`.
- Tool names and parameters are read from the tool schemas (or `discover`), never
  guessed. When a name here does not exist, find the equivalent with `discover` and
  say which one was used.
- Reads are free to run. **Every write** (create, edit, transition, comment, upload,
  sprint change) is shown to the user first and runs only after a yes for that batch
  (`modes.automation_always_ask` includes `external_calls`). `executeDestructive`
  (deletes) is never used by these skills.

## 2. Jira → local files

| Jira | Local file | Format |
|---|---|---|
| Epic | `production/epics/<epic-slug>/EPIC.md` | `/create-epics` epic format |
| Story, Task | `production/epics/<epic-slug>/story-<number>-<slug>.md` | `.claude/docs/templates/story.md` |
| Sub-task | a `## Sub-tasks (Jira)` checklist inside its parent story | — |
| Bug | `production/qa/bugs/BUG-<number>.md` | `.claude/docs/templates/bug-report.md` |
| Active sprint | `production/sprint-status.yaml` + `production/sprints/sprint-<NNN>.md` | `.claude/docs/templates/sprint-status.yaml`, `sprint-plan.md` |
| Version (release) | `production/milestones/<version-slug>.md` | `.claude/docs/templates/milestone-definition.md` |

- `<number>` is the numeric part of the key (`MAN-123` → `story-123-…`), so file names
  stay stable when the backlog is re-ranked; the sprint's rank order lives in
  `sprint-status.yaml`. `<slug>` is the summary in lowercase ASCII with hyphens, accents
  removed, at most 6 words. A story with no epic goes to `jira.no_epic_slug`.
- Story header lines, in the template's order: `# Story <number>: <summary>`, then
  `> **Epic**`, `> **Status**` (through `jira.status_map`), `> **Layer**` (leave for
  `/story-readiness` unless a label says it), `> **Type**` (from labels or components
  through `jira.type_labels` when set, else leave it for `/story-readiness`),
  `> **Estimate**` (story points or original estimate), `> **Jira**: [MAN-123](<url>)`,
  `> **Sprint**`, `> **Last Updated**` (Jira `updated`).
- `jira.mirror_scope: open` (this project): epics and open issues get files; closed
  issues are listed in their epic's `## Hecho en Jira` section and stay in Jira.
  `all`: closed issues get files too.
- Status: `jira.status_map`, then two project rules — a "Por hacer" issue in the active
  sprint (or the next one when none is active) is `jira.todo_in_sprint` (`Ready`), and a
  Flagged issue (`jira.fields.flagged`) is `jira.flagged_is` (`Blocked`): MAN has no
  blocked status. Pushing `Blocked` sets the flag; leaving it clears the flag.
- `sprint-status.yaml`: one entry per issue in the active sprint (the next one when none
  is active), in board rank order (`jira.fields.rank`); `id` is the Jira key, `file` the
  story path, `status` through the yaml mapping in the template's header, `priority`
  through `jira.priority_map`, `owner` the assignee's display name, `estimate_days` 0
  (MAN estimates in story points). Its header `id` is the sprint id.
- `production/epics/index.md` is derived from the epics (header `id: MAN`, `kind: index`)
  and regenerated by every pull; it is never pushed.
- An EPIC.md's `> **Stories**` line, its `## Stories (Jira)` table and its
  `## Hecho en Jira` list come from the epic's children: every pull rebuilds them, even
  when the epic itself did not change (its header `remote_version` is the epic's own
  `updated`), and that rebuild is never pushed.
- Links: "blocks" / "is blocked by" → the story's `## Dependencies`; "relates to" → `## Context`.
- Jira attachments are not downloaded; they are listed as links in the story.

### The description marker

A Jira description has two parts, split at the first line that is exactly
`## Detalle técnico (Claude Code Game Studios)` (or a heading with that text):

- **Above it**: what the team wrote. Pulled verbatim into the story's
  `## Descripción (Jira)` section, never rewritten by a push.
- **Below it**: the framework's sections (Acceptance Criteria when the team has none,
  Implementation Notes, Out of Scope, Files, QA Test Cases, Test Evidence). Pull maps
  them back into the story's sections; push replaces only this part.

When the team's part already has acceptance criteria (a heading or list named
"Criterios de aceptación" / "Acceptance criteria"), they become the story's
`## Acceptance Criteria` as written, and a push never moves them.

## 3. Confluence → local files

Pages under the folder `confluence.root_folder_id`, at any depth, are classified once
by `/atlassian-setup` and recorded in `confluence.pages`. A new page found later is
proposed by `/atlassian-sync pull`, never placed silently.

| Page content | Local file |
|---|---|
| Concept, pitch, general GDD | `design/gdd/game-concept.md` |
| Design pillars | `design/gdd/game-pillars.md` |
| List of systems | `design/gdd/systems-index.md` |
| One system or mechanic (overrides, upgrades, enemies, waves, chests, shop, economy…) | `design/gdd/<system-slug>.md` |
| Art document, visual identity | `design/art/art-bible.md` |
| Sound and music | `design/audio/sound-bible.md` |
| HUD | `design/ux/hud.md` |
| Other UI and UX specs | `design/ux/<feature-slug>.md` |
| Accessibility | `design/accessibility-requirements.md` |
| Lore, characters, dialogue | `design/narrative/{lore,characters,dialogue}/<slug>.md` |
| Levels and maps | `design/levels/<level-slug>.md` |
| Architecture overview | `docs/architecture/architecture.md` |
| A technical decision | `docs/architecture/adr-<NNNN>-<slug>.md` |
| Roadmap, milestones | `production/milestones/<slug>.md` |
| Meeting notes, devlogs, personal drafts, anything else | not mirrored: `confluence.ignore` |

- A page whose content fits two rows goes where most of it fits; say why.
- Map 2 (`MilitaryBase`) pages are mirrored like the rest but flagged as paused in the
  report: no work is planned from them while the map is paused.
- Folders are traversed, not mirrored. Page hierarchy does not have to match the local tree.

### Content conversion

- Ask for the body in Markdown when the tool offers it (`getContentFormatGuide`); else
  read the storage format and convert headings, lists, tables, links, code, emphasis.
- Images and attachments become links to the Confluence attachment. Mentions become
  the person's display name. Jira issue macros become `MAN-123` links.
- Anything that does not convert (layouts, expand macros, embeds, diagrams, status
  lozenges with logic) becomes a visible line `> [Confluence: <macro> — ver la página]`
  and the page is recorded as `rich: true` in `confluence.pages`. A loss is always
  visible in the file, never silent.
- The local file keeps the page's own headings. The framework's GDD section names are
  not forced on a pulled page: `/adopt` and `/design-system retrofit` report and fill
  gaps afterwards, and those additions go back to Confluence with a push.

## 4. Sync states

`bash .claude/scripts/atlassian-mirror.sh list` gives the local side; the remote side is
the page's `snapshotToken` (`v:4` for a page, `d1:858` for a live doc, whose version
number does not move while it is edited) or the issue's `updated` timestamp, compared
for equality with the header's `remote_version`.

| Remote | Local | State | Action |
|---|---|---|---|
| same | unchanged | `IN SYNC` | nothing |
| newer | unchanged | `REMOTE AHEAD` | pull overwrites the file and stamps it |
| same | changed | `LOCAL AHEAD` | push candidate |
| newer | changed | `CONFLICT` | show both changes; the user picks: keep remote, push local, or merge |
| gone (deleted, moved, no permission) | any | `REMOTE MISSING` | report; never delete the local file without a yes |
| — | no header | `LOCAL ONLY` | push candidate: create the issue or page |
| could not read | any | `NOT ASSESSED` | report the reason; never treated as in sync |

`NOT ASSESSED` outranks `IN SYNC` and is outranked by `CONFLICT` in any summary.

## 5. Writing back

- **Jira**: summary, status (through `jira.transitions`; read `listJiraIssueTransitions`
  when a transition is missing), estimate, labels, sprint and the part of the
  description below the marker. A new story is created in `jira.project_key` under its
  epic (`parent`), with the team's part of the description empty unless the user writes one.
  `/story-done` evidence screenshots in `production/qa/evidence/` are uploaded to the
  story, and a short comment says what was verified.
- **Confluence, pages created from local files** (`origin: local` in `confluence.pages`):
  full update from the local Markdown, version + 1.
- **Confluence, pages the team wrote** (`origin: confluence`): update only the sections
  changed locally and keep the rest of the stored body as it was. When the page is
  `rich: true` or the change cannot be placed section by section, propose appending a
  section, or a child page, instead; a whole-page overwrite needs an explicit yes
  after the user has seen the diff.
- New pages are created under `confluence.new_pages_parent_id` (default: the root
  folder), titled like the local file's first heading.
- After every successful write, stamp the local file (`atlassian-mirror.sh stamp`) with
  the new `remote_version` and `pushed_at`, so the next `list` reads it as in sync.
- A write that fails leaves the local file and its header untouched and is reported
  with the error; the rest of the batch continues only if the user agrees.
