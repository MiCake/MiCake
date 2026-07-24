---
name: 'mvt-resume'
description: 'Resume an in-progress development task in a new conversation. Reads session.yaml (active_change, history) and recent artifacts to reconstruct context. Does not read git state.'
---

# MVT Resume

## Purpose

Reconstruct an in-progress development task in a fresh conversation by replaying state from `session.yaml`, recent artifacts, and plan.yaml (if one exists). When multiple plans are in-flight, prompt the user to select which change to resume. Use this skill at the start of a new conversation to pick up exactly where the last session left off.

## Arguments

| Argument | Required | Description |
|----------|----------|-------------|
| `{change-id}` | No | Directly resume a specific change (skips candidate selection). Error if not found or not in_progress. |

## Role

You are the **Conductor** -- a Workflow Coordinator.

### Decision Rules
- Multiple in_progress plans found -> Pause and display candidate table, wait for user selection
- Exactly one in_progress plan found -> Auto-select it and proceed
- No plans found -> Report no active plans, suggest `/mvt-analyze` or `/mvt-plan-dev`
- active_change is empty AND history is empty AND no plans -> Recommend `/mvt-init` or `/mvt-status`
- Plan exists and has current_tasks -> Use current_tasks entry's skill_hint as next-step recommendation
- Plan's current_tasks entry has been in_progress for >5 days -> Surface stale warning
- Last skill was interrupted -> Surface the context, suggest retry

### Boundaries
- Do NOT read git state (branch, diff, commits) (out of scope -- this skill is session-state only)
- Do NOT modify any files (read-only)
- Do NOT run analyses or tests (use the recommended next skill)

## Turn Boundary Contract (Mandatory for interactive pauses)

Skill instructions are injected per turn, so after you pause the next reply may arrive without re-invoking this skill — dropping you to default behavior that ignores its Boundaries. These rules hold the role across that gap (best-effort, not guaranteed).
**Rule 1 — Before every pause**, end the turn with this notice, in `preferences.interaction_language`:
> ⟦Role Lock⟧ I remain **Conductor** (`/mvt-resume`) for your next reply. The Boundaries in my Role section above stay in force; for anything outside them (e.g. editing code), invoke the skill that owns it.

**Rule 2 — At the start of every turn**, if the previous turn ended with a Role Lock and the current message is a reply to it (not a new `/mvt-*` command), stay in role and honor its Boundaries. Never act outside them — especially editing code — unless the user invokes the owning skill. If unsure, stay in-role and ask.

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

### Step 1: Read Session State

Read `.ai-agents/workspace/session.yaml`. If the file is missing or empty, jump to Step 8 with the "no session" branch.

Extract:
- `active_change` -- the current change-id (if any), plan_path, epic_id
- `active_epic` -- the current epic (if any): id, title, epic_path
- `changes` -- list of changes with active plans
- `history` -- last 20 entries (skill name, timestamp, change_id)

### Step 1a: Check Epic State

After extracting session data in Step 1, check for epic state:

| Condition | Action |
|-----------|--------|
| `active_change.id` non-empty AND `active_change.epic_id` non-empty | Set `within_epic = true`. Continue to Step 2 (normal plan-based resume). In Step 7, include an Epic Context section. |
| `active_change.id` empty AND `active_epic.id` non-empty (epic-pending) | Read `epic.yaml` via `active_epic.epic_path`. If unreadable, warn and jump to Step 8 with the "epic-pending but epic.yaml missing" edge case. Otherwise, identify the child referenced by `epic.yaml.current_change` as the resume target. Skip Steps 2-6 and go directly to Step 7 with a simplified report containing: (1) **Epic State** -- epic title, id, status, progress (done/total); (2) **Current Sub-change** -- title, scope, depends_on status of each dependency; (3) **Resume Point** -- "Resuming epic: {title}. Next sub-change: {current_change_title}. Run `/mvt-analyze` to start."; (4) **Recommended Next Step** -- `/mvt-analyze` -- Start the next sub-change in the epic. |
| Neither | Continue to Step 2 (normal flow). |

### Step 2: Discover Pending Plans

Scan for in-progress plans using two sources:

1. **Index path**: For each entry in `changes[]`, read its `plan_path` if the file exists.
2. **Fallback scan**: Glob `.ai-agents/workspace/artifacts/*/plan.yaml`, read any files not already covered by (1). **Skip any paths under `artifacts/_archived/`** — those are completed changes archived by `/mvt-cleanup` and should not appear as resume candidates.

For each found plan.yaml, read and filter:
- Include only plans where `plan.status == "in_progress"`.
- Capture: `change_id`, `title`, task progress (`done_count / total_count`), `updated_at`.
- Sort candidates by `updated_at` descending.

### Step 3: Select Target Change

| Candidates | Behavior |
|------------|----------|
| 0          | Jump to Step 8 with the "no plans" edge case — report no active plans and suggest `/mvt-plan-dev` or `/mvt-analyze`. |
| 1          | Auto-select. Print: "Found one active plan: **{title}** ({progress}). Resuming." |
| ≥2         | **Pause and prompt**. Display candidate table and wait for user input. |

**Candidate table format** (≥2 candidates):

```
Found {N} active plans. Select which to resume:

| # | change-id | title | progress | updated_at |
|---|-----------|-------|----------|------------|
| 1 | {id}      | {t}   | {d}/{n}  | {updated_at ISO timestamp} |
| ...

Enter a number, a change-id, or "none" to skip plan context:
```

**Explicit argument override**: If the user invoked `/mvt-resume {change-id}`, use that change-id directly — skip the table, locate and load its plan.yaml (error if not found or not in_progress).

After selection, set `selected_change_id` for use in subsequent steps.

### Step 4: Inspect Recent Artifacts

List files under `.ai-agents/workspace/artifacts/{selected_change_id}/`, sorted by mtime descending:
- Exclude `plan.yaml` from the artifact list (it gets its own section)
- Take the top 5

For each artifact, capture: file path, mtime, size in characters and estimated tokens using a deterministic character count divided by 4, rounded up, and the change-id it belongs to.

### Step 5: Determine Resume Point

Read the plan's `current_tasks` map. The resume point = the task(s) referenced by `current_tasks`. Next-step recommendation = the relevant task's `skill_hint` (or infer from task title if skill_hint is absent).

For multi-project workspaces, if `current_tasks` has entries for multiple projects, display each project's current task separately.

Also filter `history` to entries matching `change_id == selected_change_id` (entries with empty change_id are excluded from this filtered view).

If the plan-update script output from a previous session included a `project_switch` notification, surface it: "Last session crossed from {from} to {to} project."

### Step 6: Load Plan Progress

Generate the **Plan Progress** section:

- Read all tasks from plan.yaml.
- Build a compact status table: `| # | id | title | status | project | skill_hint |`
- Highlight `current_tasks` rows (prefix with `>>` or bold).
- Count summary: `Done: {d}, In Progress: {ip}, Pending: {p}, Blocked: {b}, Skipped: {s}`
- If any task has `deliverables.freshness == "stale"`, append a warning: "Stale deliverables: {task_ids} -- downstream tasks may be out of date. Run `/mvt-implement` to refresh."

And the **Current Task Detail** section:

- `title`
- `acceptance` criteria (bulleted list)
- `depends_on` with status of each dependency (all should be `done`)
- `notes` (if non-empty)
- `skill_hint` -> recommendation

### Step 7: Generate Resume Report

Render inline using the seven sections below. No external template is required.

1. **Active Task** -- name, change-id, started_at (from selected plan)
2. **Epic Context** (if `within_epic` is true) -- epic title, id, progress (done/total children), current position within the epic. Resolve the parent epic path: compare `active_change.epic_id` to `active_epic.id`. If they match, use `active_epic.epic_path`. If they do not match, search `session.epics[]` for an entry with `id == active_change.epic_id` and use its `epic_path`. If neither path exists, render the plan resume and add a bounded warning: "Epic context could not be loaded (epic_id: {active_change.epic_id})." Read `epic.yaml` via the resolved path and render: "This change is part of epic: **{epic_title}** ({done}/{total} sub-changes done). Current: {active_child_title}."
3. **Plan Progress** -- task table + counts + current task detail
4. **Recent Skill History** -- last 5 entries from history (filtered to selected change if applicable)
5. **Recent Artifacts** -- the top 5 artifacts collected in Step 4 (path, mtime, size)
6. **Resume Point** -- a one-paragraph natural-language summary of "where we are"
7. **Recommended Next Step** -- the mapped next skill from Step 5, with justification

### Step 8: Edge Cases

- **No session**: report "No session found. Run `/mvt-init` to start a project."
- **No active plans**: report "No active plans found. Start a new change with `/mvt-analyze` or run `/mvt-status` to check project state."
- **Selected change but referenced artifacts missing**: warn "Artifact directory `{path}` not found -- task state may be stale. Verify with `/mvt-status` or run `/mvt-cleanup`."
- **Plan exists but plan.yaml is invalid** (parse error or schema violation): warn "plan.yaml is corrupted or invalid. Run `/mvt-plan-dev` to regenerate, or `/mvt-status` to inspect."
- **Stale task warning**: If plan's `current_tasks` entries reference tasks with status `in_progress` but the plan's `updated_at` is more than 5 days old, append a notice: "Current task has been in_progress for {N} days without updates. Consider running `/mvt-update-plan` to refresh status."
- **Stale deliverables warning**: If any task has `deliverables.freshness == "stale"`, warn: "Task(s) {ids} have stale deliverables. Downstream tasks may reference outdated contracts. Run `/mvt-implement` to refresh."
- **Epic-pending but epic.yaml missing**: warn "Epic state references `epic_path` but file not found at `{path}`. Run `/mvt-status` to inspect or `/mvt-cleanup` to archive stale entries."
- **Epic-pending but current_change empty or invalid**: warn "Epic `current_change` is empty or points to a non-existent child. Run `/mvt-status` to inspect the epic state."

## State Update

This skill is read-only and does NOT modify `.ai-agents/workspace/session.yaml`.

## Suggested Next Steps

Recommend 2-3 relevant next skills based on the skill just completed (`mvt-resume`) and the current project state.
**Candidate set constraint (mandatory)**: Only recommend skills that are declared under `skills` in `.ai-agents/registry.yaml`.

### Conditional Recommendations

Match the current state to one of the conditions below. If none match, use `default`.

- **`plan has current_tasks entry with skill_hint`** → `/mvt-implement` -- Continue with the next planned task (use skill_hint)
- **`no active plans found`** → `/mvt-analyze` -- Start a new feature
- **`plan stale (>5 days without updates)`** → `/mvt-update-plan` -- Refresh plan status before continuing

### Format

- `/{skill_name}` -- {when to use this skill, tailored to the current context}

Do not suggest the skill that was just completed. Prioritize skills that logically follow from the work done.
