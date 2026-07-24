# Epic Update Script — Full Reference

> **This file is the authoritative usage reference for `epic-update.cjs`.**
> Read this file before calling the script. Do NOT read the `.cjs` or `.js` source to learn flag names or semantics.

The script has five modes — use exactly one mode per invocation:

## Commands

```bash
# Mode 1 — Complete the current child and advance to the next sub-change
node .ai-agents/scripts/epic-update.cjs --epic <epic_path> --complete-child <change_id>

# Mode 2 — Set a child's status without advancing current_change
node .ai-agents/scripts/epic-update.cjs --epic <epic_path> --set-child-status <change_id> --child-status <active|pending|done|abandoned>

# Mode 3 — Switch the active child to a different one (reorder)
node .ai-agents/scripts/epic-update.cjs --epic <epic_path> --switch-active <change_id>

# Mode 4 — Add one or more children to an existing epic
node .ai-agents/scripts/epic-update.cjs --epic <epic_path> \
  --add-child <id> --child-title "<title>" --child-scope "<scope>" [--child-depends-on "dep1,dep2"] \
  [--add-child <id2> --child-title "<title2>" --child-scope "<scope2>" ...]

# Mode 5 — Validate an epic.yaml (read-only, no write)
node .ai-agents/scripts/epic-update.cjs --validate <epic_path>
```

## Argument values

| Argument | Value source | Example |
|----------|-------------|---------|
| `--epic` | `active_epic.epic_path` resolved from session.yaml | `".ai-agents/workspace/artifacts/epic-20260608-ecommerce-platform/epic.yaml"` |
| `--complete-child` | `active_change.id` of the child whose plan is fully done | `20260608-sub` |
| `--set-child-status` | target child `change_id` to re-status | `20260608-sub` |
| `--child-status` | new status (with `--set-child-status`): `active` / `pending` / `done` / `abandoned` | `done` |
| `--switch-active` | target child `change_id` to make the active one | `20260609-sub2` |
| `--add-child` | new child id (repeatable for multiple children in one invocation) | `20260620-sub3` |
| `--child-title` | title for the child added by the preceding `--add-child` | `"Cart"` |
| `--child-scope` | scope description for the child added by the preceding `--add-child` | `"Shopping cart CRUD and checkout flow"` |
| `--child-depends-on` | optional; comma-separated prerequisite child `change_id` values | `"20260608-sub"` |
| `--validate` | path to an `epic.yaml` to validate (read-only) | same as `--epic` |

## Parameter semantics

| Argument | When to use | Effect on `epic.yaml` |
|----------|-------------|------------------------|
| `--complete-child` | A child change's plan is fully done and the epic should advance | Sets the child `status: done`, advances `current_change` to the next `pending` child whose `depends_on` are all `done` (DAG-based). |
| `--set-child-status` + `--child-status` | Mark a child `done` or `abandoned` without advancing `current_change` (e.g. defer mode) | Sets the child's status only; `current_change` unchanged. |
| `--switch-active` | Reorder to a different child (dependencies permitting) | Sets the target child `active`, others `pending`, updates `current_change`. Rejects if the target's `depends_on` have unfinished prerequisites. |
| `--add-child` (+ `--child-title` / `--child-scope` / `--child-depends-on`) | Append one or more children to an existing epic | Adds entries to `children[]`; validates id uniqueness + DAG; defaults `project` to the sole project name when single-project. |
| `--validate` | Verify `epic.yaml` integrity (e.g. after `/mvt-decompose` writes it) | Read-only check; no write. Reports DAG/structure errors on stderr. |

## Output interpretation

- **Exit 0**: success. stdout is a single-line JSON object (mirrors `plan-update.cjs` protocol). Use the fields directly to render output. The file is already written — do NOT read it back to verify.
- **Exit 1**: failure. stderr carries a plain-text error (unknown mode, child not found, dependency unsatisfied, validation failure, parse/write error). The file was **not** modified. Report the error to the user and do not fabricate a success summary.
