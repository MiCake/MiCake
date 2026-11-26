# Epic Update Script — Full Reference

> **This file is the authoritative usage reference for `epic-update.cjs`.**
> Read this file before calling the script. Do NOT read the `.cjs` or `.js` source to learn flag names or semantics.

The script has six modes — use exactly one operation per invocation:

## Commands

```bash
# Mode 1 — Complete the current child and advance to the next sub-change
node .mvtt/scripts/epic-update.cjs --epic <epic_path> --complete-child <change_id>

# Mode 2 — Complete the child but defer activation of the next sub-change
node .mvtt/scripts/epic-update.cjs --epic <epic_path> --complete-child <change_id> --defer-next

# Mode 3 — Abandon a child and advance to the next eligible sub-change
node .mvtt/scripts/epic-update.cjs --epic <epic_path> --abandon-child <change_id>

# Mode 4 — Switch the active child to a different one (reorder)
node .mvtt/scripts/epic-update.cjs --epic <epic_path> --switch-active <change_id>

# Mode 5 — Add one or more children to an existing epic
node .mvtt/scripts/epic-update.cjs --epic <epic_path> \
  --add-child <id> --child-title "<title>" --child-scope "<scope>" [--child-project "proj1,proj2"] [--child-depends-on "dep1,dep2"] [--child-context-refs "ctx-001,ctx-002"] \
  [--add-child <id2> --child-title "<title2>" --child-scope "<scope2>" ...]

# Mode 6 — Re-attribute an existing child to different project(s)
node .mvtt/scripts/epic-update.cjs --epic <epic_path> --set-child-project <change_id> --child-project "proj1,proj2"

# Read-only validation (no write)
node .mvtt/scripts/epic-update.cjs --validate <epic_path>
```

## Argument values

| Argument | Value source | Example |
|----------|-------------|---------|
| `--epic` | `active_epic.epic_path` resolved from session.yaml | `".mvtt/workspace/artifacts/epic-20260608-ecommerce-platform/epic.yaml"` |
| `--complete-child` | `active_change.id` of the child whose plan is fully done | `20260608-sub` |
| `--defer-next` | use only with `--complete-child` when the next ready child should not activate yet | flag only |
| `--abandon-child` | target child `change_id` that should be abandoned | `20260608-sub` |
| `--switch-active` | target child `change_id` to make the active one | `20260609-sub2` |
| `--add-child` | new child id (repeatable for multiple children in one invocation) | `20260620-sub3` |
| `--child-title` | title for the child added by the preceding `--add-child` | `"Cart"` |
| `--child-scope` | scope description for the child added by the preceding `--add-child` | `"Shopping cart CRUD and checkout flow"` |
| `--child-project` | with `--add-child`: comma-separated names from `project-context.yaml > projects[].name`; **required** when the workspace has more than one project, optional when it has one. With `--set-child-project`: required, same value rules | `"catalog-api"` |
| `--set-child-project` | target child `change_id` whose `project` should be replaced | `20260608-sub` |
| `--child-depends-on` | optional; comma-separated prerequisite child `change_id` values | `"20260608-sub"` |
| `--child-context-refs` | **required** when the epic has a `requirement_context`; optional otherwise; comma-separated `requirement_context` item ids the new child needs | `"ctx-001,ctx-002"` |
| `--validate` | path to an `epic.yaml` to validate (read-only) | same as `--epic` |

## Parameter semantics

| Argument | When to use | Effect on `epic.yaml` |
|----------|-------------|------------------------|
| `--complete-child` | A child change's plan is fully done and the epic should advance | Sets the child `status: done`, advances `current_change` to the next `pending` child whose `depends_on` are all `done` (DAG-based). |
| `--complete-child` + `--defer-next` | A child is done but the next ready child should wait for explicit resume | Sets the child `status: done`, clears `current_change`, and leaves pending children pending. The epic remains `in_progress` unless all children are terminal. |
| `--abandon-child` | An active or pending child should not be completed | Sets the child `status: abandoned`, sets `completed_at`, and advances to the first dependency-ready pending child using the same deterministic array order as completion. |
| `--switch-active` | Reorder to a different child (dependencies permitting) | Sets the target child `active`, others `pending`, updates `current_change`. Rejects if the target's `depends_on` have unfinished prerequisites. |
| `--add-child` (+ `--child-title` / `--child-scope` / `--child-project` / `--child-depends-on` / `--child-context-refs`) | Append one or more children to an existing epic | Adds entries to `children[]`; validates id uniqueness + DAG. `project` comes from `--child-project`, every name must exist in `projects[].name`; without it, a single-project workspace defaults to the sole project and a multi-project workspace **fails** listing the valid names. Without a readable `project-context.yaml`, `project` falls back to `["default"]`. For epics with a `requirement_context`, `--child-context-refs` is **required** and every referenced item id must exist in `requirement_context.items`; otherwise it stays optional (and is preserved when supplied). |
| `--set-child-project` + `--child-project` | Correct a child attributed to the wrong project (e.g. a legacy `["default"]`) | Replaces the child's `project` only; status and `current_change` unchanged. Every name must exist in `projects[].name`; fails when `project-context.yaml` is unreadable. |
| `--validate` | Verify `epic.yaml` integrity (e.g. after `/mvt-decompose` writes it) | Read-only check; no write. Reports DAG/structure errors on stderr. On success, adds a `warnings` array when a child's `project` names are not in `projects[].name`; repair those with `--set-child-project`. |

When every child is terminal, the epic becomes `abandoned` if every child is abandoned; otherwise it becomes `done`.

## Common sequences

- **Choose the next child instead of the array-order default.** After `--complete-child <id> --defer-next`, run `--switch-active <chosen>`. After `--abandon-child <id>`, which auto-activates the first ready child, run `--switch-active <chosen>`; the switch demotes the auto-activated child back to `pending`.
- **Switch away from the active child without finishing it.** Pause the session entry first with `session-update.cjs --pause-change`, then run `--switch-active <other>`, which returns the previous child to `pending`.

## Completed children are not reopened

A child that is `done` or `abandoned` stays that way: `--switch-active` accepts only `pending` or `active` targets, and no mode moves a finished child back. Follow-up work is a new child: `--add-child <id> ... --child-depends-on <original child>`.

## Output interpretation

- **Exit 0**: success. stdout is a single-line JSON object (mirrors `plan-update.cjs` protocol). It includes `epic_status` and `current_change`, but never a `session_sync` field. Use the fields directly to render output. The file is already written — do NOT read it back to verify.
- **Exit 1**: failure. stderr carries a plain-text error (unknown mode, child not found, dependency unsatisfied, missing or unknown project, validation failure, parse/write error). The file was **not** modified. Report the error to the user and do not fabricate a success summary.

`epic-update.cjs` writes only `epic.yaml`. The workflow must use `session-update.cjs` for all session lifecycle transitions after a successful epic mutation.
