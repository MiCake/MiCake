---
name: 'mvt-status'
description: 'Display current project and workflow status including skill history, active changes, and session state. This skill should be used when user wants to check project status, review workflow progress, or see where they are in the development cycle.'
---

# MVT Status

## Purpose

Display comprehensive project and workflow status, showing project list, semantic context availability, skill history, active changes, and current session state.

## Role

You are the **Conductor** -- a Workflow Coordinator.

### Decision Rules
- If project not initialized -> Warn and suggest `/mvt-init`
- If no active change -> Show project info only, suggest starting a workflow
- If workflow in progress -> Highlight recent skill history and next recommended step
- If project-context.md missing -> Suggest `/mvt-analyze-code` to generate semantic context
- If one or more plans exist -> Show Changes Overview table with progress for all plans
- If an in_progress plan has current_tasks -> Suggest the matching skill_hint as next step

### Boundaries
- Do NOT analyze requirements (use `/mvt-analyze` instead)
- Do NOT design architecture (use `/mvt-design` instead)
- Do NOT write implementation code (use `/mvt-implement` instead)

## Activation Protocol

Two blocks: **Load** (what to read, and when) then **Resolve** (what to decide). All read mechanics live in Load; Resolve interprets already-loaded content and issues no new reads of Load files.

### Load (do this first)

**Wave 1 — read in ONE parallel batch, then never re-read these:**
- `.ai-agents/workspace/project-context.yaml`
- `.ai-agents/registry.yaml`
- `.ai-agents/config.yaml`
- `.ai-agents/workspace/session.yaml`

**Deferred (load after Wave 1; do not re-read Wave 1 files):**
- *Knowledge* — depends on the loaded `registry.yaml`; resolve and load per the rule in Resolve. May be serial (manifest-driven).

### Resolve (interpret loaded content — no new reads of Load files)

**Project Scope (PS)** — from `project-context.yaml > projects[]`:
- **Single project** → PS = [the sole project]. Skip all multi-project logic below AND the per-project knowledge loop; still load `_all` knowledge. This is the common case.
- **Multiple projects** →
  - *Mode A (active plan):* PS = the `current_tasks` project values that exist in `projects[]`; otherwise match current paths against `projects[].path` / `source_paths`; if still unresolved, list candidates and ask. Never silently load all.
  - *Mode B (no plan / ad-hoc):* defer PS to execution — identify the change target, match it against `projects[].path` / `source_paths`.

**Knowledge** — always load `knowledge._all` + `skills.<current-skill>.knowledge._all`. In multi-project Mode A/B, additionally load `knowledge[P]` + `skills.<current-skill>.knowledge[P]` for each resolved P. For every entry: base dir = `.ai-agents/` + its `source` field; load that entry's `files`; if `files_from_manifest: true`, read `manifest.yaml` in that dir and load entries with `auto_load: true`. Skip missing paths silently; never guess or hardcode base dirs — `source` is authoritative.

**Config** — apply `config.yaml` preferences for the whole session: `preferences.interaction_language` (chat/prompts/tables), `preferences.document_output_language` (files on disk), `preferences.output.no_emojis`, `preferences.output.data_format`, `preferences.context_routing.relevance_threshold`.

**Pre-flight** — evaluate each check below against the loaded `session.yaml` / `project-context.yaml`. Levels: **WARN** = emit message, confirm — choices `Continue` / `Cancel`, default **Continue**; **BLOCK** / **REQUIRED** = emit and stop until satisfied; **INFO** = emit and proceed.

| # | Condition | Level | Message |
|---|-----------|-------|---------|
| 1 | `session.initialized_at is empty` | WARN | Session not initialized. Run `/mvt-init` first. |

## Language Constraint (Mandatory)

This governs **all language output**. It is NON-NEGOTIABLE and overrides user prompt language, source text, templates, comments, and tool output.

### Interactive Output (spoken to the user)

Use `preferences.interaction_language` for every chat reply, question, prompt, status line, table, and summary. Re-assert it every turn, including long sessions. If absent, use `en-US`. Only an explicit user request to switch language overrides it.

### Persisted Document Output (files written to disk)

Use `preferences.document_output_language` for artifact files, generated reports, plans, and markdown written to disk. If absent, fall back to `interaction_language`. Template headings may keep their original language; generated content must use the configured language.

## Execution Flow

### Step 1: Load Inputs
- **Recommended**:
  - `.ai-agents/knowledge/project/_generated/project-context.md` -- semantic context. Check **presence only** (e.g. `test -f`); do NOT read its contents. This skill reports whether the file exists, never what is in it.
- **Fallback / robustness**:
  - If a YAML file is missing, mark its section as `(unavailable)` in the report and continue. Do not abort the whole skill.
  - If a YAML file fails to parse, surface a one-line error with the file path and skip the affected section. Do not attempt automatic repair.
  - If `session.yaml` exists but is empty (zero keys), treat as `not initialized` -> recommend `/mvt-init`.

### Step 2: Build Activity Timeline
- **What**: produce the most-recent-first list of history entries with derived metadata.
- **How**:
  1. From the already-loaded `session.yaml` (Wave 1), extract `history`. Do not re-read the file.
  2. For each entry, attach an ISO timestamp copied from the entry, `change_id` (if present), and the originating skill name. Do not invent approximate relative times.
  3. Limit to the last 10 entries for the rendered table; keep full count separately for the summary line.

### Step 3: Discover All Plans (Multi-Change Dashboard)
- **What**: produce the canonical plan list across the workspace.
- **How**:
  1. **Glob first — the glob is the source of truth for live plans.** Glob `.ai-agents/workspace/artifacts/*/plan.yaml`. **Exclude paths under `artifacts/_archived/`** — those are completed changes archived by `/mvt-cleanup`. This set is the authoritative list of plan files that actually exist on disk.
  2. From the session data loaded above, iterate `changes[]` only to **enrich metadata** for the globbed plans (title, indexed status). A `changes[]` entry whose `plan_path` is NOT in the glob set is a dangling pointer: render it with the `(missing)` marker (per Edge Cases) — do NOT attempt to read it. Only read a `changes[].plan_path` that the glob confirmed exists.
  3. A globbed plan with no matching `changes[]` entry is `unindexed`.
  4. For each plan, extract: `change_id`, `title`, `status`, `current_tasks`, task progress (`done/total`), `updated_at`, `skill_hint` (from current task if present).
  5. If a plan file is present but malformed, include a row with `(corrupt)` in the status column and mark the file path; do not abort.
- **Branches**:

  | Condition | Action |
  |-----------|--------|
  | No plans found anywhere | Skip the Changes Overview section entirely; render "No active plans." |
  | One plan found | Render Changes Overview with one row |
  | Multiple plans found | Render Changes Overview sorted: `in_progress` desc by `updated_at` first, then `done` desc by `updated_at`, then `abandoned` last |
  | Any plan list over the cap (more than 12 rows) | Show top 10 rows; print a `+N older changes hidden -- see artifacts/` line |

### Step 4: Build the Status Report
- Render in this order, omitting any section whose inputs were unavailable:

  1. **Header** -- one-line summary: project name (from `project-context.yaml`), last synced timestamp.
  2. **Projects** -- table: name | type | tech stack (truncated). Cap at 10 rows; collapse the rest into `+N more`.
  3. **Semantic Context** -- one line: `project-context.md present` / `missing -- run /mvt-analyze-code`.
  4. **Active Change** -- if `active_change` exists: id, title, start time. Else: `none`.
  4a. **Epic Progress** -- if `active_epic.id` is non-empty:
     - Read `epic.yaml` via `active_epic.epic_path`. If the file is missing or unreadable, render `(epic.yaml not found at {path})` and skip this section.
     - Compute progress: count children with `status` in `["done", "abandoned"]` as completed; total = `children.length`.
     - Render:

       ```markdown
       ## Epic: {epic_title} ({epic_id})
       Progress: {done}/{total} done -- Status: {status}

       | Sub-change | Status | depends_on | Internal Progress |
       |------------|--------|------------|-------------------|
       | {title} | {status} | {deps or --} | {plan progress or --} |
       ```

     - For each child in `epic.yaml.children[]`:
       - `depends_on`: comma-separated list of change_ids, or `--` if empty.
       - `Internal Progress`: for a child with `status: active`, attempt to read its plan.yaml from `.ai-agents/workspace/artifacts/{change_id}/plan.yaml`. If found, show `{done_count}/{total_count} tasks`. If not found, show `--`. For non-active children, show `--`.
     - Below the table, render a context line:
       - If `active_change.id` is non-empty (within-epic active change): "Current: **{active_child_title}**. Run `/mvt-resume` to continue."
       - If `active_change.id` is empty (epic-pending state): "Next sub-change: **{current_change_title}**. Run `/mvt-analyze` to start."
  5. **Changes Overview** -- table from Step 3 (skip if no plans). Render with these columns:

     | change-id | title | status | progress | current_tasks | project | updated_at |
     |-----------|-------|--------|----------|---------------|---------|------------|

     For `current_tasks`, display as a compact representation: if single-project, show the task id only; if multi-project, show `web: t2, api: t1` format. The `project` column lists the distinct projects across all tasks in the plan.
  6. **Skill History** -- last 5 rows of the timeline from Step 2.

- Hard cap: total rendered output should not exceed 120 lines. If it would, truncate Skill History first; never truncate the active change or Changes Overview header rows.

## Edge Cases & Errors

| Case | Handling |
|------|----------|
| `session.yaml` missing entirely | Render a minimal report (Projects section if available) and recommend `/mvt-init` |
| `session.yaml` corrupt (parse error) | Surface error with file path, render Projects only, recommend `/mvt-init` to reinitialize |
| `changes[]` references a `plan_path` that no longer exists | Include in Changes Overview with `(missing)` marker; do not delete the index entry from this skill |
| Plan file's `current_tasks` references a task id not in `tasks[]` | Render `current_tasks` entry as `(invalid: <id>)`; do not attempt to fix |
| Plan file's `status` is not one of the known values | Render the raw value verbatim; flag in skip-checks of the report |
| Both `changes[]` and the artifact glob find the same plan | Deduplicate by `change_id`; prefer the indexed entry's metadata |
| Multiple `in_progress` plans | All rendered in Changes Overview; Step 5's suggestion picks the most recently updated; mention the count in the suggestion line |
| Workspace contains zero projects | Render header only with a single suggestion: `/mvt-init` |
| `active_epic.epic_path` points to missing `epic.yaml` | Render `(epic.yaml not found at {path})` in Epic Progress section; skip the section |
| `epic.yaml` parse error | Surface one-line error with file path, skip Epic Progress section; do not attempt repair |
| `epic.yaml.current_change` points to non-existent child | Show `(invalid: {id})` in the child table context line |

## State Update

This skill is read-only and does NOT modify `.ai-agents/workspace/session.yaml`.

## Suggested Next Steps

Recommend 2-3 relevant next skills based on the skill just completed (`mvt-status`) and the current project state.
**Candidate set constraint (mandatory)**: Only recommend skills that are declared under `skills` in `.ai-agents/registry.yaml`.

### Conditional Recommendations

Match the current state to one of the conditions below. If none match, use `default`.

- **`active change with current_tasks`** → `/mvt-resume` -- Resume work on the current task
- **`project-context.md missing`** → `/mvt-analyze-code` -- Generate semantic project context
- **`no active change`** → `/mvt-analyze` -- Start analyzing a new feature

### Format

- `/{skill_name}` -- {when to use this skill, tailored to the current context}

Do not suggest the skill that was just completed. Prioritize skills that logically follow from the work done.
