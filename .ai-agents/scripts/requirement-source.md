# Requirement Source Script — Full Reference

Authoritative usage reference for `requirement-source.cjs`. Read this before calling the script — do NOT read the `.cjs`/`.js` source for flag names or semantics. The script is read-only (never modifies sources or epic artifacts); use exactly one mode per invocation. Exit 0 = single-line JSON on stdout; exit 1 = plain-text error on stderr; no files modified.

## Commands

```bash
node .ai-agents/scripts/requirement-source.cjs --fingerprint <source_path>
node .ai-agents/scripts/requirement-source.cjs --verify-epic <epic_path>
node .ai-agents/scripts/requirement-source.cjs --effective-context <epic_path> --child <change_id>
```

## Modes

| Mode | When to use | Output / Effect |
|------|-------------|-----------------|
| `--fingerprint` | `/mvt-decompose` captures a supplied file source | `{ok, reference, reference_base, fingerprint, size}`. Normalized stored reference, base (`workspace`/`absolute`), raw-byte SHA-256, byte size. Exit 1 if missing, unreadable, or not a regular file. |
| `--verify-epic` | Check an epic's context schema and source drift | `{ok, sources[], warnings[]}`. One `unchanged`/`changed`/`missing`/`unverifiable` status per source. Exit 0 even with drift; exit 1 only for structural errors. |
| `--effective-context` | `/mvt-analyze` / `/mvt-resume` start a child | `{ok, child, context[], sources[], warnings[]}`. `context` = global refs then child refs, deduplicated by first occurrence. |

Epics without a `requirement_context`: empty `context` + `sources`, plus a `context_unavailable` warning; `child.scope` is the fallback baseline.

## Path resolution

- Workspace files → workspace-root-relative POSIX paths (no `.`/`..`); external files → normalized absolute POSIX paths (`reference_base: "absolute"`).
- Relative refs resolve from the workspace root found by walking up from `epic.yaml` (fallback: epic dir), never from process cwd → cwd-independent.
- `--fingerprint` finds the root by walking up from the source file (fallback: process cwd).

## Context handling

| `requirement_context` | Children `context_refs` | Handling |
|-----------------------|------------------------|----------|
| absent | — | Empty `context` + `sources`, `context_unavailable` warning; `child.scope` fallback |
| present, valid | optional (validated if present) | Normal projection |
| present, invalid | — | Exit 1 (schema error) |

When `requirement_context` is present: source/item ids must be unique; every `source_ids`, `global_refs`, and `children[].context_refs` entry must reference an existing id; a file source needs a non-empty reference + `sha256:<hex>` fingerprint; a conversation source needs `reference: "conversation"` and no fingerprint; relative references must not contain `.`/`..`; `global_refs` is optional (default `[]`). Newly created epics must include `requirement_context` (non-empty `sources` + `items`) and at least one `context_refs` per child.

## Source statuses

| Status | Meaning |
|--------|---------|
| `unchanged` | Readable regular file whose raw-byte SHA-256 matches the stored fingerprint; conversation sources are always `unchanged` |
| `changed` | Readable regular file whose raw-byte SHA-256 differs (line-ending changes count as changes) |
| `missing` | Resolved path does not exist |
| `unverifiable` | Exists but is not a readable regular file (permission or file-type failures) |

`warnings[]` only for non-`unchanged` states and missing-context fallback. Drift is data, not a failure — the snapshot stays the execution baseline.
