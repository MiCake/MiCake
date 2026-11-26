# Session Update Script - Full Reference

> This file is the authoritative usage reference for `session-update.cjs`.

## Command

```bash
node .mvtt/scripts/session-update.cjs \
  --skill <skill-name> \
  --summary "<interaction-language summary>" \
  [lifecycle and maintenance flags]
```

Every invocation validates all flags and session-state preconditions before mutating `session.yaml`. A validation failure exits with code 1 and leaves the file unchanged.

## Change Lifecycle

| Flag | Effect |
|---|---|
| `--new-change <title> --change-id <id>` | Starts a new change only when no other active id exists, or refreshes the same active id. For an id whose index entry is `paused`, restores its `plan_path`, `epic_id` and `created_at` and sets the entry back to `active` (`--epic-id`, when given, overrides the stored `epic_id`). Fails when the id's index entry is `done` or `abandoned`. |
| `--pause-change` | Upserts the non-empty active change as `paused` (keeping `plan_path`, `epic_id` and `created_at`), then clears it. With `--new-change` for a different id, the switch is one atomic write. |
| `--update-change` | Upserts the non-empty active change as `active`. |
| `--close-change` | Upserts the non-empty active change as `done`, then clears it. |
| `--abandon-change` | Upserts the non-empty active change as `abandoned`, then clears it. |

`--update-change`, `--close-change`, `--abandon-change`, and `--pause-change` are mutually exclusive. `--new-change` cannot be combined with close or abandon.

- `--new-change` for a different id while a change is active fails unless `--pause-change` is also given.
- `--pause-change` requires an active change, cannot target the active id through `--new-change`, and cannot be combined with `--new-epic`, `--close-epic`, or `--abandon-epic`. With `--set-plan-path` it also needs `--new-change`, since the path applies to the newly registered change.
- A `done` or `abandoned` change is never reopened: use a new change id, or `epic-update --add-child` for an epic follow-up. The refusal applies only to ids that are not the current active change, and only while the index still holds the entry (entries truncated from `changes[]` cannot be recognised).
- Flags this script no longer implements, such as `--set-change-status` and `--set-epic-status`, are ignored without error like any unknown flag.

## Epic Lifecycle

| Flag | Effect |
|---|---|
| `--close-epic` | Upserts the non-empty active epic as `done`, then clears it. |
| `--abandon-epic` | Upserts the non-empty active epic as `abandoned`, then clears it. |

`--close-epic` and `--abandon-epic` are mutually exclusive. They can compose with a change close or abandon in the same invocation.

## Repair and Cleanup Flags

```bash
node .mvtt/scripts/session-update.cjs \
  --skill mvt-cleanup \
  --summary "Repair stale lifecycle entries" \
  --prune-empty-changes \
  --repair-change-statuses "change-a=done,change-b=abandoned" \
  --remove-change "archived-change"
```

- `--prune-empty-changes` removes invalid entries whose `id` is empty.
- `--repair-change-statuses` accepts only `active`, `paused`, `done`, or `abandoned`, and each id must already exist.
- `--remove-change <ids>` removes entries with matching `id` from `session.changes[]` (comma-separated for multiple ids); it does not touch `active_change`. Unknown ids are skipped silently; if no entry was removed because every id is unknown, a warning is written to stderr and the exit code remains 0.
- `--remove-epic <ids>` removes entries with matching `id` from `session.epics[]` (comma-separated for multiple ids); it does not touch `active_epic`. Unknown ids follow the same rule as `--remove-change`.
- `--truncate-history <N>` keeps the most recent N `history[]` entries; use the configured history limit.
- A change id cannot be repaired and removed in the same invocation.
- The active change being closed, abandoned, or paused cannot also be removed.

## Mutation Order

1. Validate all flags, lifecycle preconditions, repair ids, statuses, and collisions.
2. Set the initialized and synced timestamps.
3. Prune empty change ids.
4. Apply explicit change-status repairs.
5. Pause the active change (`--pause-change`).
6. Register, refresh, or restore the new change (`--new-change`), then apply `--set-plan-path`.
7. Apply one change lifecycle operation (`--update-change`, `--close-change`, or `--abandon-change`).
8. Apply the epic operations.
9. Remove confirmed archived ids.
10. Sort/truncate lifecycle indexes, append/truncate history, then atomically replace `session.yaml`.

Index retention: `history_limits.changes` limits the number of non-paused `changes[]` entries, oldest first. `paused` entries are never truncated and do not count toward the limit, so `changes[]` can exceed the configured limit by the number of paused entries.

## Output

- Success: exit 0 and `{"ok":true}` on stdout.
- Failure: exit 1 with a plain-text explanation on stderr; `session.yaml` is unchanged.