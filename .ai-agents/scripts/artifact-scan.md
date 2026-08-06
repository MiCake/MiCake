# Artifact Scan Script - Full Reference

> **This file is the authoritative usage reference for `artifact-scan.cjs`.**
> Read this file before calling the script. Do NOT read the `.cjs` or `.js` source to learn flag names or semantics.

## Command

```bash
node .ai-agents/scripts/artifact-scan.cjs --mode <plans|change-dirs|files>
```

## Modes

| Mode | Entries | Scope |
|---|---|---|
| `plans` | Immediate `plan.yaml` files | Live immediate child change directories only. |
| `change-dirs` | Live immediate child change directories | Does not recurse. |
| `files` | All files below live immediate child change directories | Recurses within each live change directory. |

The scanner resolves the project root by locating `.ai-agents`. It excludes the `_archived` directory before enumeration. Entries are sorted and workspace-relative using `/` separators.

## Output Interpretation

- **Exit 0**: stdout is a single-line JSON object:

  ```json
  {
    "ok": true,
    "mode": "plans",
    "entries": [".ai-agents/workspace/artifacts/change-id/plan.yaml"]
  }
  ```

- **Exit 1**: stderr explains an invalid mode, missing project root, or filesystem failure. Do not infer partial entries.

This command is read-only. Callers must consume its `entries` output directly and must not run a fallback recursive artifact scan.