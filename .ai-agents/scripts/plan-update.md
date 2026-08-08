# Plan Update Script — Full Reference

> **This file is the authoritative usage reference for `plan-update.cjs`.**
> Read this file before calling the script. Do NOT read the `.cjs` or `.js` source to learn flag names or semantics.

## Command

```bash
node .ai-agents/scripts/plan-update.cjs \
  --plan "<active_change.plan_path>" \
  --task <task_id> \
  --projects "<comma,separated,project,names>" \
  [--status <pending|in_progress|done|blocked|skipped>] \
  [--artifacts "<comma,separated,paths>"] \
  [--notes "<free-form text>"] \
  [--deliverables-pointer current] \
  [--mark-deliverable-stale <task_id>[,task_id2,...]]
```

Read-only validation mode:

```bash
node .ai-agents/scripts/plan-update.cjs --validate "<draft-plan-path>" [--projects "<comma,separated,project,names>"]
```

Include `--artifacts`, `--notes`, `--deliverables-pointer`, and `--mark-deliverable-stale` only when the skill's logic determines they apply; omit each flag otherwise.

## Argument values

| Argument | Value source | Example |
|----------|-------------|---------|
| `--plan` | `active_change.plan_path` resolved from session.yaml | `".ai-agents/workspace/artifacts/chg-001/plan.yaml"` |
| `--task` | the `task_id` being updated (resolved by the skill's Step 1) | `t1` |
| `--status` | optional; the new status: `pending` / `in_progress` / `done` / `blocked` / `skipped` | `done` |
| `--projects` | comma-separated project names from `project-context.yaml > projects[].name` (single-project: the sole project name) | `"web,api"` |
| `--artifacts` | optional; comma-separated paths to append (the script de-duplicates) | `"src/auth.ts,src/auth.test.ts"` |
| `--notes` | optional; overwrites the task's `notes` | `"blocked on API spec"` |
| `--deliverables-pointer` | optional; set to `current` to record that this task's deliverables section is written | `current` |
| `--mark-deliverable-stale` | optional; comma-separated downstream task ids whose deliverables are now stale | `"t3,t4"` |
| `--validate` | optional read-only validation target; accepts the plan path as its value | `".ai-agents/workspace/artifacts/chg-001/plan.yaml"` |

## Parameter semantics

| Argument | When to use | Effect on `plan.yaml` |
|----------|-------------|------------------------|
| `--task` + optional `--status` | Use `--status` for status transitions; omit it for metadata-only updates such as deliverables freshness | When present, sets the task status; sets `completed_at` to now when `done`, else `null`; refreshes `plan.updated_at`. When omitted, status, `completed_at`, DAG advancement, and `current_tasks` are left unchanged. |
| `--projects` | Always (per-project validation) | Drives per-project DAG advancement of `current_tasks` and per-project validation. Required for correct multi-project plans. |
| `--artifacts` | Task produced or touched files | Appends + de-duplicates paths into the task's `artifacts.files`; handles `artifacts: null`. |
| `--notes` | Task needs a free-form note | Overwrites the task's existing `notes`. |
| `--deliverables-pointer` + `--mark-deliverable-stale` | Task wrote its deliverables section (e.g. `/mvt-implement` Step 8) | Records the deliverables pointer on the task; marks the listed downstream tasks' deliverables as stale so `/mvt-resume` and `/mvt-status` surface a warning. Use both flags together in a single invocation. |
| `--validate` | `/mvt-plan-dev` has assembled a draft plan before final write | Parses and validates the plan, prints JSON on success, and never writes the file. |

## What the script does (deterministically)

1. **Apply**: when `--status` is present, sets the task status and `completed_at`; appends + de-duplicates `--artifacts`; overwrites `--notes`; refreshes `plan.updated_at`.
2. **Recompute `current_tasks`** only when `--status` is present (per-project independent advancement): for each project, finds the `in_progress` task or advances the first `pending` task whose `depends_on` are all in `resolvedIds` (done + skipped; blocked does NOT satisfy). Detects `project_switch` when advancement crosses a project boundary. Plan done -> `current_tasks = {}`.
3. **Validate** the mutated plan (unique ids, valid `depends_on` references, DAG/no-cycle per project, one `in_progress` per project, every task has acceptance, `completed_at` consistency, `current_tasks` validity, project naming constraint, task project membership).
4. **Write atomically** (temp + rename) only if validation passes.

## Output interpretation

- **Exit 0**: success. stdout is a single-line JSON object, e.g.:
  ```json
  {"ok":true,"task":{"id":"t1","title":"...","old_status":"in_progress","new_status":"done"},"current_tasks":{"default":"t2"},"plan_status":"in_progress","progress":{"done":1,"total":4},"warning":null,"project_switch":null}
  ```
  Use these fields directly to render output. The file is already written — do NOT read it back to verify. If `warning` is non-null, surface it. If `project_switch` is non-null, note the project boundary crossing.
- **Exit 1**: failure. stderr carries the error (invalid status, task not found, validation failure, parse/write error). The file was **not** modified. Report the error to the user and do not fabricate a success summary.
- **Read-only validation**: exit 0 stdout is `{"ok":true,"plan_status":"...","tasks":N}`. Exit 1 stderr carries parse or validation errors; no file is modified.
