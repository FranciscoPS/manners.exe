#!/usr/bin/env bash
# atlassian-mirror.sh -- observations about the local files that mirror Jira and
# Confluence in manners.exe, plus the one write the sync needs: the header.
#
# Every mirrored file starts with a header block that names its source:
#
#   Markdown                          YAML (production/sprint-status.yaml)
#   <!-- atlassian-mirror             # atlassian-mirror
#   source: confluence                # source: jira
#   id: 123456                        # id: 57
#   kind: page                        # kind: sprint
#   title: Overrides -- GDD           # url: https://...
#   url: https://...                  # remote_version: 2026-11-21T17:59:00Z
#   remote_version: 7                 # pulled_at: 2026-11-21T18:00:00Z
#   pulled_at: 2026-11-21T18:00:00Z   # body_sha1: <sha1>
#   body_sha1: <sha1>                 # /atlassian-mirror
#   -->
#
# The body is every line after the header. body_sha1 is the SHA-1 of the body as
# it was when the file was last pulled or pushed, with CR stripped from line
# ends, so `list` can tell a local edit from an untouched copy. No header key is
# called Status: story-status.sh reads the first `Status:` line of a story file.
#
# OBSERVATIONS, NEVER VERDICTS (CLAUDE.md). `list`, `check`, `hash` and
# `unmapped` print what they found; whether a changed file is pushed, kept or
# overwritten is the skill's call, after asking. `stamp` is the only write.
#
# Usage: bash .claude/scripts/atlassian-mirror.sh <command> [args] [--root DIR]
#   list                 every mirrored file, one row each:
#                        mirror <path> <source> <id> <kind> <remote_version>
#                        <pulled_at> local=<unchanged|changed|no-hash>
#   check <file>         the header of one file, one `field <key> <value>` row
#                        per key, then `body_sha1_now <hash>` and
#                        `local <unchanged|changed|no-hash|no-header|unclosed>`
#   hash <file>          the body hash only
#   unmapped             Markdown files in the mirror folders with no header:
#                        local work that is not in Jira or Confluence yet
#   stamp <file> k=v...  write or update the header with these keys and set
#                        body_sha1 to the current body; keys not given are kept
#   --root DIR           the project; defaults to the repo this script is in
#   -h, --help           this text
#
# Mirror folders: design/, docs/architecture/, production/epics/,
# production/sprints/, production/milestones/, production/qa/bugs/ and the file
# production/sprint-status.yaml. CLAUDE.md files and design/registry/ are
# framework files, never mirrors. A folder that does not exist prints
# `absent <folder>`, so a missing mirror never reads as an empty one. `list`
# ends with `files <n> changed <n> no-hash <n>`; `unmapped` with `unmapped <n>`.
#
# Exit codes: 0 done; 1 cannot read the root or the file; 2 usage error;
# 3 no SHA-1 tool (sha1sum, shasum or Python). bash 3.2 and BWK awk safe.

LC_ALL=C
export LC_ALL

usage() {
  [ -n "${1:-}" ] && printf 'atlassian-mirror.sh: %s\n' "$1" >&2
  printf 'Run with --help for usage.\n' >&2
  exit 2
}

ROOT=""
CMD=""
FILE=""
PAIRS=""
NPAIRS=0
while [ $# -gt 0 ]; do
  case "$1" in
    -h|--help) sed -n '2,/^$/p' "$0" | sed 's/^# \{0,1\}//'; exit 0 ;;
    --root) [ $# -ge 2 ] || usage "--root needs a directory"; ROOT="$2"; shift 2; continue ;;
    --root=*) ROOT="${1#--root=}"; shift; continue ;;
  esac
  if [ -z "$CMD" ]; then CMD="$1"
  elif [ -z "$FILE" ] && [ "$CMD" != list ] && [ "$CMD" != unmapped ]; then FILE="$1"
  elif [ "$CMD" = stamp ]; then
    case "$1" in *=*) ;; *) usage "stamp takes key=value pairs, not '$1'" ;; esac
    k="${1%%=*}"; v="${1#*=}"
    case "$k" in ""|*[!a-z0-9_]*) usage "bad key '$k': lowercase letters, digits and _ only" ;; esac
    [ "$k" = body_sha1 ] && usage "body_sha1 is computed from the body, not given"
    case "$v" in *"-->"*) usage "a value cannot contain -->" ;; esac
    case "$v" in *"
"*) usage "a value cannot contain a newline" ;; esac
    PAIRS="$PAIRS$k	$v
"
    NPAIRS=$((NPAIRS + 1))
  else usage "unexpected argument '$1'"
  fi
  shift
done

[ -n "$CMD" ] || usage "no command"
case "$CMD" in
  list|unmapped) ;;
  check|hash|stamp) [ -n "$FILE" ] || usage "$CMD needs a file" ;;
  *) usage "unknown command '$CMD'" ;;
esac
[ "$CMD" = stamp ] && [ "$NPAIRS" -eq 0 ] && usage "stamp needs at least one key=value"

[ -n "$ROOT" ] || ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." 2>/dev/null && pwd)"
cd "$ROOT" 2>/dev/null || { printf 'atlassian-mirror.sh: cannot read root %s\n' "$ROOT" >&2; exit 1; }

DOCS=docs
[ -d docs ] || { [ -d Docs ] && DOCS=Docs; }

sha1_stdin() {
  if command -v sha1sum >/dev/null 2>&1; then sha1sum | awk '{ print $1 }'
  elif command -v shasum >/dev/null 2>&1; then shasum -a 1 | awk '{ print $1 }'
  elif command -v python3 >/dev/null 2>&1 && python3 -c 'import hashlib' >/dev/null 2>&1; then
    python3 -c 'import hashlib, sys; print(hashlib.sha1(sys.stdin.buffer.read()).hexdigest())'
  elif command -v python >/dev/null 2>&1 && python -c 'import hashlib' >/dev/null 2>&1; then
    python -c 'import hashlib, sys; print(hashlib.sha1(sys.stdin.buffer.read()).hexdigest())'
  else
    return 3
  fi
}

body_lines() {
  awk '
    NR == 1 {
      l = $0; sub(/\r$/, "", l)
      if (l == "<!-- atlassian-mirror") { mode = "md"; next }
      if (l == "# atlassian-mirror") { mode = "yaml"; next }
    }
    mode != "" && !done {
      l = $0; sub(/\r$/, "", l)
      if ((mode == "md" && l == "-->") || (mode == "yaml" && l == "# /atlassian-mirror")) done = 1
      next
    }
    { sub(/\r$/, ""); print }
  ' "$1"
}

body_hash() {
  command -v sha1sum >/dev/null 2>&1 || command -v shasum >/dev/null 2>&1 \
    || command -v python3 >/dev/null 2>&1 || command -v python >/dev/null 2>&1 \
    || { printf 'atlassian-mirror.sh: no SHA-1 tool (sha1sum, shasum or Python)\n' >&2; exit 3; }
  body_lines "$1" | sha1_stdin || { printf 'atlassian-mirror.sh: the SHA-1 tool failed\n' >&2; exit 3; }
}

header_fields() {
  awk '
    NR == 1 {
      l = $0; sub(/\r$/, "", l)
      if (l == "<!-- atlassian-mirror") { mode = "md"; next }
      if (l == "# atlassian-mirror") { mode = "yaml"; next }
      exit
    }
    {
      l = $0; sub(/\r$/, "", l)
      if ((mode == "md" && l == "-->") || (mode == "yaml" && l == "# /atlassian-mirror")) { closed = 1; exit }
      if (mode == "yaml") sub(/^# ?/, "", l)
      p = index(l, ":")
      if (p == 0) next
      k = substr(l, 1, p - 1); v = substr(l, p + 1)
      sub(/^[ \t]+/, "", v); sub(/[ \t]+$/, "", v)
      print k "\t" v
    }
    END {
      if (mode == "") print "__state\tno-header"
      else if (!closed) print "__state\tunclosed"
      else print "__state\tok"
    }
  ' "$1"
}

field_of() { printf '%s\n' "$1" | awk -F '\t' -v k="$2" '$1 == k { print $2; exit }'; }

local_state() {
  fields="$1"; file="$2"
  state=$(field_of "$fields" __state)
  [ "$state" = ok ] || { printf '%s' "$state"; return; }
  stored=$(field_of "$fields" body_sha1)
  [ -n "$stored" ] || { printf 'no-hash'; return; }
  now=$(body_hash "$file") || exit 3
  if [ "$now" = "$stored" ]; then printf 'unchanged'; else printf 'changed'; fi
}

mirror_files() {
  for d in design "$DOCS/architecture" production/epics production/sprints production/milestones production/qa/bugs; do
    if [ ! -d "$d" ]; then printf 'absent\t%s\n' "$d"; continue; fi
    find "$d" -type f \( -name '*.md' -o -name '*.yaml' -o -name '*.yml' \) \
      ! -name CLAUDE.md ! -path 'design/registry/*' 2>/dev/null | LC_ALL=C sort | sed 's/^/file\t/'
  done
  if [ -f production/sprint-status.yaml ]; then printf 'file\tproduction/sprint-status.yaml\n'
  else printf 'absent\tproduction/sprint-status.yaml\n'; fi
}

case "$CMD" in
  hash)
    [ -f "$FILE" ] || { printf 'atlassian-mirror.sh: no such file %s\n' "$FILE" >&2; exit 1; }
    body_hash "$FILE"
    ;;

  check)
    [ -f "$FILE" ] || { printf 'atlassian-mirror.sh: no such file %s\n' "$FILE" >&2; exit 1; }
    fields=$(header_fields "$FILE")
    printf 'path\t%s\n' "$FILE"
    printf '%s\n' "$fields" | awk -F '\t' '$1 != "__state" { print "field\t" $1 "\t" $2 }'
    printf 'body_sha1_now\t%s\n' "$(body_hash "$FILE")"
    printf 'local\t%s\n' "$(local_state "$fields" "$FILE")"
    ;;

  list)
    files=0; changed=0; nohash=0
    while IFS='	' read -r kind path; do
      if [ "$kind" = absent ]; then printf 'absent\t%s\n' "$path"; continue; fi
      fields=$(header_fields "$path")
      [ "$(field_of "$fields" __state)" = no-header ] && continue
      st=$(local_state "$fields" "$path")
      files=$((files + 1))
      [ "$st" = changed ] && changed=$((changed + 1))
      [ "$st" = no-hash ] && nohash=$((nohash + 1))
      printf 'mirror\t%s\t%s\t%s\t%s\t%s\t%s\tlocal=%s\n' "$path" \
        "$(field_of "$fields" source)" "$(field_of "$fields" id)" "$(field_of "$fields" kind)" \
        "$(field_of "$fields" remote_version)" "$(field_of "$fields" pulled_at)" "$st"
    done <<EOF
$(mirror_files)
EOF
    printf 'files %d changed %d no-hash %d\n' "$files" "$changed" "$nohash"
    ;;

  unmapped)
    n=0
    while IFS='	' read -r kind path; do
      if [ "$kind" = absent ]; then printf 'absent\t%s\n' "$path"; continue; fi
      case "$path" in *.md) ;; *) continue ;; esac
      [ "$(field_of "$(header_fields "$path")" __state)" = no-header ] || continue
      n=$((n + 1))
      printf 'unmapped\t%s\n' "$path"
    done <<EOF
$(mirror_files)
EOF
    printf 'unmapped %d\n' "$n"
    ;;

  stamp)
    [ -f "$FILE" ] || { printf 'atlassian-mirror.sh: no such file %s\n' "$FILE" >&2; exit 1; }
    h=$(body_hash "$FILE") || exit 3
    case "$FILE" in *.yaml|*.yml) defmode=yaml ;; *) defmode=md ;; esac
    tmp="$FILE.stamp.$$"
    if UPD="$PAIRS" H="$h" DEFMODE="$defmode" awk '
      function emit_header(   i, k) {
        if (emitted) return
        emitted = 1
        for (i = 1; i <= nu; i++) {
          k = uorder[i]
          if (!(k in seen)) { seen[k] = 1; korder[++nk] = k }
          kval[k] = upd[k]
        }
        if (mode == "yaml") {
          print "# atlassian-mirror"
          for (i = 1; i <= nk; i++) print "# " korder[i] ": " kval[korder[i]]
          print "# body_sha1: " ENVIRON["H"]
          print "# /atlassian-mirror"
        } else {
          print "<!-- atlassian-mirror"
          for (i = 1; i <= nk; i++) print korder[i] ": " kval[korder[i]]
          print "body_sha1: " ENVIRON["H"]
          print "-->"
        }
      }
      BEGIN {
        n = split(ENVIRON["UPD"], rows, "\n")
        for (i = 1; i <= n; i++) {
          if (rows[i] == "") continue
          p = index(rows[i], "\t")
          uk = substr(rows[i], 1, p - 1); uv = substr(rows[i], p + 1)
          if (!(uk in upd)) uorder[++nu] = uk
          upd[uk] = uv
        }
      }
      NR == 1 {
        l = $0; sub(/\r$/, "", l)
        if (l == "<!-- atlassian-mirror") { mode = "md"; inh = 1; next }
        if (l == "# atlassian-mirror") { mode = "yaml"; inh = 1; next }
        mode = ENVIRON["DEFMODE"]
        emit_header()
      }
      inh {
        l = $0; sub(/\r$/, "", l)
        if ((mode == "md" && l == "-->") || (mode == "yaml" && l == "# /atlassian-mirror")) {
          inh = 0; emit_header(); next
        }
        if (mode == "yaml") sub(/^# ?/, "", l)
        p = index(l, ":")
        if (p > 0) {
          k = substr(l, 1, p - 1); v = substr(l, p + 1); sub(/^[ \t]+/, "", v)
          if (k != "body_sha1" && !(k in seen)) { seen[k] = 1; korder[++nk] = k; kval[k] = v }
        }
        next
      }
      { print }
      END {
        if (NR == 0) mode = ENVIRON["DEFMODE"]
        emit_header()
      }
    ' "$FILE" > "$tmp"; then
      mv "$tmp" "$FILE" || { rm -f "$tmp"; printf 'atlassian-mirror.sh: could not replace %s\n' "$FILE" >&2; exit 1; }
    else
      rm -f "$tmp"; printf 'atlassian-mirror.sh: could not write %s\n' "$FILE" >&2; exit 1
    fi
    fields=$(header_fields "$FILE")
    printf 'stamped\t%s\tbody_sha1=%s\tlocal=%s\n' "$FILE" "$(field_of "$fields" body_sha1)" "$(local_state "$fields" "$FILE")"
    ;;
esac
exit 0
