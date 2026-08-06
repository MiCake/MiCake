# Session Update Script - Full Reference

> This file is the authoritative usage reference for `session-update.cjs`.

## Command

```bash
node .ai-agents/scripts/session-update.cjs \
  --skill <skill-name> \
  --summary "<interaction-language summary>" \
  [lifecycle and maintenance flags]
```

Every invocation validates all flags and session-state preconditions before mutating `session.yaml`. A validation failure exits with code 1 and leaves the file unchanged.

## Change Lifecycle

| Flag | Effect |
|---|---|
| `--new-change <title> --change-id <id>` | Starts a new change only when no other active id exists, or refreshes the same active id. |
| `--update-change` | Upserts the non-empty active change as `active`. |
| `--close-change` | Upserts the non-empty active change as `done`, then clears it. |
| `--abandon-change` | Upserts the non-empty active change as `abandoned`, then clears it. |

`--update-change`, `--close-change`, and `--abandon-change` are mutually exclusive. `--new-change` cannot be combined with close or abandon.

## Epic Lifecycle

| Flag | Effect |
|---|---|
| `--close-epic` | Upserts the non-empty active epic as `done`, then clears it. |
| `--abandon-epic` | Upserts the non-empty active epic as `abandoned`, then clears it. |

`--close-epic` and `--abandon-epic` are mutually exclusive. They can compose with a change close or abandon in the same invocation.

## Repair and Cleanup Flags

```bash
node .ai-agents/scripts/session-update.cjs \
  --skill mvt-cleanup \
  --summary "Repair stale lifecycle entries" \
  --prune-empty-changes \
  --repair-change-statuses "change-a=done,change-b=abandoned" \
  --remove-change "archived-change"
```

- `--prune-empty-changes` removes invalid entries whose `id` is empty.
- `--repair-change-statuses` accepts only `active`, `done`, or `abandoned`, and each id must already exist.
- A change id cannot be repaired and removed in the same invocation.
- The active change being closed or abandoned cannot also be removed.

## Mutation Order

1. Validate all flags, lifecycle preconditions, repair ids, statuses, and collisions.
2. Prune empty change ids.
3. Apply explicit change-status repairs.
4. Apply one change lifecycle operation.
5. Apply one epic lifecycle operation.
6. Remove confirmed archived ids.
7. Sort/truncate lifecycle indexes, append/truncate history, then atomically replace `session.yaml`.

## Output

- Success: exit 0 and `{"ok":true}` on stdout.
- Failure: exit 1 with a plain-text explanation on stderr; `session.yaml` is unchanged.