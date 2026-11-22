---
paths:
  - "design/**/*.md"
  - "docs/architecture/**/*.md"
  - "Docs/architecture/**/*.md"
  - "production/epics/**/*.md"
  - "production/sprints/**/*.md"
  - "production/milestones/**/*.md"
  - "production/qa/bugs/**/*.md"
  - "production/sprint-status.yaml"
  - "production/atlassian.yaml"
---

# Jira and Confluence Mirror

The team works in Jira (project `MAN`, board 34) and Confluence (folder `manners.exe`):
those are the source of truth. Files in these folders are the local copies the
framework's skills read. Mapping and formats: `.claude/docs/atlassian-mapping.md`.

- A file that starts with `<!-- atlassian-mirror` (`# atlassian-mirror` in YAML) is a
  mirror; its header names the Jira key or Confluence page it comes from. Never edit
  the header by hand: `bash .claude/scripts/atlassian-mirror.sh stamp` writes it after a
  pull or push. Never add a `Status:` key to it.
- Before a decision rests on a mirror, check it is current: `atlassian-mirror.sh list`
  shows local edits and `/atlassian-sync status` compares with Jira and Confluence. A
  mirror that could not be compared is `NOT ASSESSED`, not current.
- Framework skills may edit mirrors (`/story-done`, `/design-system retrofit`, …). The
  edit stays local until the user publishes it: Confluence pages with `/actualizar-docs`
  (or `/atlassian-sync push` typed by the user), Jira issues with `/atlassian-sync push`.
  When you finish, say which mirrors changed; offer the push for Jira issues only. Never
  start a Confluence push on your own initiative, and never push without the user's yes
  for that batch.
- A new framework artifact in these folders (story, GDD, ADR, sprint plan, bug) has no
  header until `/atlassian-sync push` creates it in Jira or Confluence and stamps it.
- What the team wrote in Jira or Confluence is never deleted or rewritten without
  explicit confirmation: the framework's additions go below the description marker in
  Jira and in their own sections or child pages in Confluence.
- `production/atlassian.yaml` holds ids and mappings: change it through
  `/atlassian-setup` or `/atlassian-sync`, after showing the change. It holds no secrets.
