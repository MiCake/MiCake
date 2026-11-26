# Plan Update Script — Full Reference

> **This file is the authoritative usage reference for `plan-update.cjs`.**
> Read this file before calling the script. Do NOT read the `.cjs` or `.js` source to learn flag names or semantics.

## Command

```bash
node .mvtt/scripts/plan-update.cjs \
  --plan "<active_change.plan_path>" \
  --task <task_id> \
  --projects "<comma,separated,project,names>" \
  [--status <pending|in_progress|done|blocked|skipped>] \
  [--artifacts "<comma,separated,paths>"] \
  [--notes "<free-form text>"] \
  [--deliverables-pointer current] \
  [--mark-deliverable-stale <task_id>[,task_id2,...]] \
  [--dispatched <set|clear>]
```

Read-only validation mode:

```bash
node .mvtt/scripts/plan-update.cjs --validate "<draft-plan-path>" [--projects "<comma,separated,project,names>"]
```

Draft-install mode (writes the plan; no `--task` needed):

```bash
node .mvtt/scripts/plan-update.cjs --install-draft "<draft-plan-path>" --plan "<active_change.plan_path>" [--projects "<comma,separated,project,names>"]
```

Read-only dispatch-planning mode (no `--task` needed):

```bash
node .mvtt/scripts/plan-update.cjs --dispatch-plan "<active_change.plan_path>" --skill-hint <skill> [--max-agents <N>]
```

Include `--artifacts`, `--notes`, `--deliverables-pointer`, `--mark-deliverable-stale`, and `--dispatched` only when the skill's logic determines they apply; omit each flag otherwise.

## Argument values

| Argument | Value source | Example |
|----------|-------------|---------|
| `--plan` | `active_change.plan_path` resolved from session.yaml | `".mvtt/workspace/artifacts/chg-001/plan.yaml"` |
| `--task` | the `task_id` being updated (resolved by the skill's Step 1) | `t1` |
| `--status` | optional; the new status: `pending` / `in_progress` / `done` / `blocked` / `skipped` | `done` |
| `--projects` | comma-separated project names from `project-context.yaml > projects[].name` (single-project: the sole project name) | `"web,api"` |
| `--artifacts` | optional; comma-separated paths to append (the script de-duplicates) | `"src/auth.ts,src/auth.test.ts"` |
| `--notes` | optional; overwrites the task's `notes` | `"blocked on API spec"` |
| `--deliverables-pointer` | optional; set to `current` to record that this task's deliverables section is written | `current` |
| `--mark-deliverable-stale` | optional; comma-separated downstream task ids whose deliverables are now stale | `"t3,t4"` |
| `--validate` | optional read-only validation target; accepts the plan path as its value | `".mvtt/workspace/artifacts/chg-001/plan.yaml"` |
| `--dispatched` | optional; `set` records `dispatched_at` on an `in_progress` task, `clear` removes it | `set` |
| `--install-draft` | path of a draft plan to validate and install at `--plan`; must differ from `--plan` | `".mvtt/workspace/artifacts/chg-001/plan.draft.yaml"` |
| `--dispatch-plan` | read-only dispatch-planning target; accepts the plan path as its value | `".mvtt/workspace/artifacts/chg-001/plan.yaml"` |
| `--skill-hint` | required with `--dispatch-plan`; only tasks with this `skill_hint` are candidates | `mvt-implement` |
| `--max-agents` | optional with `--dispatch-plan`; maximum batch size, an integer of at least 1 (default 1) | `3` |

## Parameter semantics

| Argument | When to use | Effect on `plan.yaml` |
|----------|-------------|------------------------|
| `--task` + optional `--status` | Use `--status` for status transitions; omit it for metadata-only updates such as deliverables freshness | When present, sets the task status; sets `completed_at` to now when `done`, else `null`; refreshes `plan.updated_at`. When omitted, status, `completed_at`, DAG advancement, and `current_tasks` are left unchanged. |
| `--projects` | Always (per-project validation) | Drives per-project DAG advancement of `current_tasks` and per-project validation. Required for correct multi-project plans. |
| `--artifacts` | Task produced or touched files | Appends + de-duplicates paths into the task's `artifacts.files`; handles `artifacts: null`. |
| `--notes` | Task needs a free-form note | Overwrites the task's existing `notes`. |
| `--deliverables-pointer` + `--mark-deliverable-stale` | Task wrote its deliverables section (e.g. `/mvt-implement` Step 8) | Records the deliverables pointer on the task; marks the listed downstream tasks' deliverables as stale so `/mvt-resume` and `/mvt-status` surface a warning. Use both flags together in a single invocation. |
| `--validate` | `/mvt-plan-dev` has assembled a draft plan before final write | Parses and validates the plan, prints JSON on success, and never writes the file. |
| `--install-draft` | `/mvt-plan-dev` has a draft that should become `plan.yaml` | Validates the draft exactly like `--validate`; on success writes its text unchanged to `--plan` (creating the directory, replacing any existing plan) through a temp file and rename, then deletes the draft. On failure nothing is written and the draft is kept. |
| `--dispatched` | A main agent claims a task for a sub-agent (`set`) or releases it (`clear`) | `set` stores `dispatched_at` (requires `in_progress` after any `--status` in the same call, so `--status in_progress --dispatched set` claims a pending task in one call); `clear` removes it. Never recomputes `current_tasks` on its own. Any status change away from `in_progress` removes `dispatched_at` automatically. |
| `--dispatch-plan` | A skill decides whether to dispatch tasks to sub-agents in parallel | Prints a batch plan (see below) and never writes the file. |

## What the script does (deterministically)

1. **Apply**: when `--status` is present, sets the task status and `completed_at` and removes `dispatched_at` unless the new status is `in_progress`; applies `--dispatched`; appends + de-duplicates `--artifacts`; overwrites `--notes`; refreshes `plan.updated_at`.
2. **Recompute `current_tasks`** only when `--status` is present (per-project independent advancement): for each project, points at the first `in_progress` task in plan order, or advances the first `pending` task whose `depends_on` are all in `resolvedIds` (done + skipped; blocked does NOT satisfy). Several tasks of one project may be `in_progress`; `current_tasks` is the focus pointer, not the set. Detects `project_switch` when advancement crosses a project boundary. Plan done -> `current_tasks = {}`.
3. **Validate** the mutated plan (unique ids, valid `depends_on` references, DAG/no-cycle per project, every task has acceptance, `completed_at` consistency, `dispatched_at` only on `in_progress` tasks, `current_tasks` validity, project naming constraint, task project membership).
4. **Write atomically** (temp + rename) only if validation passes.

## Dispatch planning (`--dispatch-plan`)

Rules, applied in `tasks[]` order:

1. **Candidates**: tasks whose `skill_hint` equals `--skill-hint` and that are either `in_progress` without `dispatched_at`, or `pending` with every `depends_on` `done` or `skipped`.
2. **Interrupted**: `in_progress` tasks with `dispatched_at`, any skill. They are never candidates.
3. **Scope**: `artifacts.files` with `\` turned into `/` and leading `./` removed. A missing or empty list, or any entry containing `*`, is unknown scope.
4. **Write-capable skills** (every skill except `mvt-review`): unknown-scope candidates are deferred as `unknown-scope` (they run only serially). A candidate whose scope equals, or is a directory prefix of, a batch member's scope (either direction) is deferred as `overlap`. Otherwise, a full batch defers it as `cap`.
5. **`mvt-review`**: no scope or overlap check; candidates fill the batch up to `--max-agents`, and the rest are deferred as `cap`.

## Output interpretation

- **Exit 0**: success. stdout is a single-line JSON object, e.g.:
  ```json
  {"ok":true,"task":{"id":"t1","title":"...","old_status":"in_progress","new_status":"done"},"current_tasks":{"default":"t2"},"in_progress":["t2"],"ready":["t3"],"plan_status":"in_progress","progress":{"done":1,"total":4},"warning":null,"project_switch":null}
  ```
  Use these fields directly to render output. The file is already written — do NOT read it back to verify. `in_progress` lists every `in_progress` task; `ready` lists `pending` tasks whose dependencies are all `done` or `skipped`. If `warning` is non-null, surface it. If `project_switch` is non-null, note the project boundary crossing.
- **Exit 1**: failure. stderr carries the error (invalid status, task not found, validation failure, parse/write error). The file was **not** modified. Report the error to the user and do not fabricate a success summary.
- **Read-only validation**: exit 0 stdout is `{"ok":true,"plan_status":"...","tasks":N}`. Exit 1 stderr carries parse or validation errors; no file is modified.
- **Draft install**: exit 0 stdout is `{"ok":true,"plan_status":"...","tasks":N,"installed":"<plan path>","draft_removed":true}`; `draft_removed` is `false` when the draft could not be deleted. Exit 1 stderr carries parse or validation errors, or a missing or identical draft path; the plan file is not touched.
- **Read-only dispatch planning**: exit 0 stdout is
  ```json
  {"ok":true,"batch":[{"id":"t3","title":"...","project":["web"],"files":["src/a.ts"],"dependents":["t6"]}],"deferred":[{"id":"t5","reason":"overlap","with":"t3"}],"interrupted":[{"id":"t2","dispatched_at":"2026-11-19T08:00:00.000Z"}]}
  ```
  `dependents` lists the tasks whose `depends_on` includes that batch task. `reason` is `overlap` (with `with`), `cap`, or `unknown-scope`. Exit 1 means a missing `--skill-hint`, an invalid `--max-agents`, or an unreadable plan; no file is modified.
