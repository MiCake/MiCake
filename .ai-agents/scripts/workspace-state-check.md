# Workspace State Check Script - Full Reference

> **This file is the authoritative usage reference for `workspace-state-check.cjs`.**
> Read this file before calling the script. Do NOT read the `.cjs` or `.js` source to learn behavior or finding codes.

## Command

```bash
node .ai-agents/scripts/workspace-state-check.cjs
```

The checker resolves the project root by locating `.ai-agents`, then reads `workspace/session.yaml` and referenced plans. It never writes session, plan, epic, or history files.

## Finding Codes

| Code | Meaning | Recommended action |
|---|---|---|
| `EMPTY_CHANGE_ID` | An indexed `changes[]` entry has no id. | `prune_empty_entry` |
| `PLAN_DONE_INDEX_ACTIVE` | A non-current indexed change remains `active` while its plan is `done`. | `set_status_done` |
| `PLAN_PATH_MISSING` | A non-empty indexed plan path cannot be found. | `manual_review` |
| `PLAN_INVALID` | A referenced plan cannot be parsed or lacks the expected plan shape. | `manual_review` |
| `EPIC_REFERENCE_UNRESOLVED` | An indexed change references an epic not known to the session. | `manual_review` |

`PLAN_DONE_INDEX_ACTIVE` is never emitted for `session.active_change.id`: a completed plan can remain the active review or test context until its lifecycle is explicitly finalized.

## Output Interpretation

- **Exit 0**: stdout is a single-line JSON object:

  ```json
  {
    "ok": true,
    "findings": [
      {
        "code": "PLAN_DONE_INDEX_ACTIVE",
        "change_id": "change-id",
        "recommended_action": "set_status_done"
      }
    ]
  }
  ```

- **Exit 1**: stderr reports a missing project root, missing session file, or session parse failure. The checker has not written any state.

Consumers may present findings for confirmation. Only `session-update.cjs` applies selected repairs.