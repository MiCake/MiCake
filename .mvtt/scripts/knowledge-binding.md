# Knowledge Binding Script - Full Reference

> **This file is the authoritative usage reference for `knowledge-binding.cjs`.**
> Read this file before calling the script. Do NOT read the `.cjs` or `.js` source to learn flag names or semantics.

The script owns every change to knowledge bindings: the knowledge files under `.mvtt/knowledge/`, the bindings in `.mvtt/registry.yaml` and the entries in `.mvtt/knowledge/core/manifest.yaml`. A mutating command changes all of them as one all-or-nothing transaction. Never edit `registry.yaml` or the core manifest by hand to add, remove, move or rename knowledge, and never create or delete managed knowledge files outside this script.

## Prerequisites

- Run from inside an MVTT project; `.mvtt/registry.yaml` must exist (`mvtt install`) and parse as YAML with a `skills` mapping.
- `.mvtt/workspace/project-context.yaml` enables project `--scope` values and `list` orphan detection; without it only `--scope _all` is accepted and orphan detection is skipped.
- `.mvtt/knowledge/core/manifest.yaml` is read when present; a missing manifest counts as empty and is created when a core file is added.

## Concepts

- **Path**: a knowledge file is identified by its path relative to `.mvtt/knowledge/`, always with `/`, for example `principle/team-rules.md` or `core/user/style.md`.
- **Managed locations**: only `principle/`, `project/` and `core/user/` are managed. Anything under `core/_framework/`, any path segment named `_generated`, absolute paths, `..` segments, and names with path separators, a leading or trailing dot, surrounding spaces, more than 200 characters, or Windows-invalid characters are refused.
- **Binding**: a registry entry `{type: static, source: knowledge/<dir>/, files: [<name>]}` placed in a map: `knowledge.<scope>` (shared by all skills) or `skills.<skill>.knowledge.<scope>`. `<scope>` is `_all` or a project name from `project-context.yaml`. The script writes no `id`. Registry entries it does not own (`files_from_manifest`, no `files` list, or a `source` outside `knowledge/`) are never read, rewritten or counted.
- **Core files**: a file under `core/user/` has exactly one manifest entry `{path: user/<name>, origin: user, auto_load: true}`, and a file under `principle/` or `project/` has none. A core file is loaded only when `knowledge._all` holds the entry `{id: core, source: knowledge/core/, files_from_manifest: true}`. `add --core` and `move --core` add that entry automatically when it is missing and report it in `registry.added`; the entry stays when the last core file is removed.
- **Tokens**: `ceil(characters / 4)`, as in the existing context reports.

## Command: add

```bash
node .mvtt/scripts/knowledge-binding.cjs add \
  --name <file-name> \
  [--from <source-file>] \
  (--type principle|project | --core) \
  [--scope <_all|project>] [--shared] [--skill <skill>,<skill>] \
  [--on-conflict overwrite|register-existing] \
  [--dry-run]
```

| Flag | Required | Semantics |
|---|---|---|
| `--name` | yes | File name only, for example `team-rules.md`. |
| `--from` | when the file is not already at its target | Read-only source file; it is copied into the managed tree and never changed or moved. When it already is the target file, the file is registered in place. An unreadable path is an error. |
| `--type` | unless `--core` | `principle` or `project`: the directory the file goes to. No default. |
| `--core` | no | Store under `core/user/`, add the manifest entry and wire core. Excludes `--type`, `--scope`, `--shared` and `--skill`. |
| `--scope` | with `--shared` or `--skill` | `_all` or a project name. No default. A project name must exist in `project-context.yaml`. |
| `--shared` | no | Bind for all skills (`knowledge.<scope>`). |
| `--skill` | no | Comma list of skills (`skills.<skill>.knowledge.<scope>`); each must exist in `registry.yaml`. May be combined with `--shared`. |
| `--on-conflict` | no | Resolution for a `file_differs` conflict. |
| `--dry-run` | no | Print the plan as JSON; write nothing. |

With neither `--shared` nor `--skill` (and no `--core`) the file is only stored and not bound. Re-adding identical content with identical bindings changes nothing and returns `changed: false`.

## Command: remove

```bash
node .mvtt/scripts/knowledge-binding.cjs remove --path <relative-path> [--dry-run]
```

| Flag | Required | Semantics |
|---|---|---|
| `--path` | yes | Managed path, for example `principle/team-rules.md`. |
| `--dry-run` | no | Print the plan; write nothing. |

Drops the file from every registry map and, for a core file, from the manifest; trims a multi-file entry to its other files and drops the entry when it becomes empty; then removes the file. If the file is already missing, the references are still dropped, the file action is `unchanged` and a warning is returned. It is an error only when there is neither a file nor a reference.

## Command: move

```bash
node .mvtt/scripts/knowledge-binding.cjs move \
  --path <relative-path> \
  [--type principle|project | --core | --none] \
  [--scope <_all|project>] [--shared] [--skill <skill>,<skill>] \
  [--dry-run]
```

| Flag | Required | Semantics |
|---|---|---|
| `--path` | yes | Managed path of an existing file. |
| `--type` | when the file leaves core | Move the file to `principle/` or `project/`. Used alone, it keeps the current bindings and re-points them to the new path. |
| `--core` | no | Move the file into `core/user/`. Drops every registry binding, adds the manifest entry and wires core. A file already under `core/user/` without a manifest entry is adopted the same way (file action `unchanged`). Excludes `--type`, `--scope`, `--shared`, `--skill` and `--none`. |
| `--shared`, `--skill`, `--scope` | no | The bindings the file should have after the move, with the same rules as `add`. |
| `--none` | no | Remove every binding and keep the file. Excludes `--scope`, `--shared` and `--skill`. A core file cannot be unbound in place: use `--type <type> --none`. |
| `--dry-run` | no | Print the plan; write nothing. |

`move` is declarative: the flags describe the state after the move. The script removes the bindings that no longer apply and adds the new ones; bindings that stay are untouched. The file itself moves only when `--type` or `--core` changes its top-level location, and then it lands at `<type>/<file name>` or `core/user/<file name>`. A multi-file entry is split: the moved file gets its own entry and its siblings stay. At least one target flag is required. The result `path` is the resulting path and `files[].from` carries the old one.

## Command: rename

```bash
node .mvtt/scripts/knowledge-binding.cjs rename --path <relative-path> --to <new-file-name> [--dry-run]
```

| Flag | Required | Semantics |
|---|---|---|
| `--path` | yes | Managed path of an existing file. |
| `--to` | yes | New file name only; the directory stays. Same name rules as `--name`. |
| `--dry-run` | no | Print the plan; write nothing. |

Rewrites the file name in every reference, keeping its position in a multi-file entry, and the manifest path of a core file. A name that differs only in letter case on a case-insensitive file system is refused; rename to a different name first, then to the final one.

## Command: list

```bash
node .mvtt/scripts/knowledge-binding.cjs list
```

Read-only; takes no flags and writes nothing (no backups).

## Conflicts

A change that would overwrite an existing file is reported as a conflict instead of being applied.

| Kind | Raised by | Resolutions |
|---|---|---|
| `file_differs` | `add` when the target file exists with different bytes than `--from` | `overwrite` (replace the file) or `register-existing` (keep the file, add the bindings), chosen with `--on-conflict` |
| `destination_exists` | `move` or `rename` when the destination file exists | none (`resolutions` is empty): choose another name, or remove the existing file first |

`--dry-run` lists conflicts in `conflicts[]` and exits 0. Applying with an unresolved conflict writes nothing and exits 1 with a message starting `CONFLICT <kind>:`. Ask the user which resolution to use, then re-run `add` with `--on-conflict`.

## Dry run and apply

Run the command with `--dry-run` first to show the user exactly what will change, then run it again without `--dry-run`. The apply recomputes the plan from the current state, so it matches the preview unless files changed in between. A dry run has the same JSON shape with `dryRun: true` and no backups.

Applying runs in order: back up the registry and, when it changes, the core manifest to `.mvtt/.backup/` (kept after success); create the new files; write the manifest and then the registry; retire the old files. Any failure undoes the completed steps in reverse. A process killed midway never leaves a reference to a missing file, but may leave extra files that `list` shows as `unbound` and a `knowledge-txn-*` directory under `.mvtt/.backup/` to delete by hand.

## Output interpretation

- **Exit 0**: stdout is a single-line JSON object.

  `add --name team-rules.md --from team-rules.md --type principle --shared --scope _all`:

  ```json
  {
    "ok": true,
    "command": "add",
    "dryRun": false,
    "changed": true,
    "path": "principle/team-rules.md",
    "files": [{ "path": "principle/team-rules.md", "action": "created" }],
    "registry": {
      "changed": true,
      "added": [{ "registryPath": "knowledge._all", "entry": { "type": "static", "source": "knowledge/principle/", "files": ["team-rules.md"] } }],
      "removed": [],
      "backup": ".mvtt/.backup/registry-2026-11-20T11-01-50-556Z.yaml"
    },
    "manifest": { "changed": false, "added": [], "removed": [], "backup": null },
    "tokens": 6,
    "conflicts": [],
    "warnings": []
  }
  ```

  `move --path principle/team-rules.md --scope api --skill mvt-review` (the file stays; the shared binding is replaced by a per-skill one). Same shape as `add`; only these fields differ:

  ```json
  {
    "files": [{ "path": "principle/team-rules.md", "action": "unchanged" }],
    "registry": {
      "added": [{ "registryPath": "skills.mvt-review.knowledge.api", "entry": { "type": "static", "source": "knowledge/principle/", "files": ["team-rules.md"] } }],
      "removed": [{ "registryPath": "knowledge._all", "entry": { "type": "static", "source": "knowledge/principle/", "files": ["team-rules.md"] } }]
    }
  }
  ```

  `rename --path principle/team-rules.md --to rules.md` (only the `files` entry differs in shape):

  ```json
  { "path": "principle/rules.md", "action": "renamed", "from": "principle/team-rules.md" }
  ```

  `add --name style.md --from style.md --core` on a registry with no core wiring: the manifest entry and the wiring entry both appear.

  ```json
  {
    "files": [{ "path": "core/user/style.md", "action": "created" }],
    "registry": { "added": [{ "registryPath": "knowledge._all", "entry": { "id": "core", "source": "knowledge/core/", "files_from_manifest": true } }] },
    "manifest": { "added": [{ "path": "user/style.md", "origin": "user", "auto_load": true }] }
  }
  ```

  A `--dry-run` of `add` whose target file exists with different content lists the conflict and still exits 0:

  ```json
  { "conflicts": [{ "kind": "file_differs", "path": "principle/rules.md", "resolutions": ["overwrite", "register-existing"] }] }
  ```

  `list`:

  ```json
  {
    "ok": true,
    "command": "list",
    "entries": [{
      "path": "principle/rules.md",
      "type": "principle",
      "bindings": [{ "kind": "skill", "scope": "api", "skill": "mvt-review", "registryPath": "skills.mvt-review.knowledge.api" }],
      "tokens": 6
    }],
    "core": [{ "path": "core/user/style.md", "autoLoad": true, "tokens": 6 }],
    "coreWired": true,
    "unbound": [],
    "dangling": [],
    "orphans": [],
    "totals": { "autoLoadedTokens": 12 },
    "warnings": []
  }
  ```

  Field notes:
  - `files[].action` is one of `created`, `overwritten`, `moved`, `renamed`, `deleted`, `unchanged`; `moved` and `renamed` also carry `from`.
  - `changed: false` means nothing was written; `registry.backup` and `manifest.backup` are `null` then.
  - `registry.added` and `registry.removed` list entries by `registryPath`, the dotted path of the map the entry lives in. A `rename` lists one removed and one added entry per binding.
  - `list` fields: `entries` are bound files under `principle/` and `project/` (a file bound twice has two `bindings`); `core` are manifest entries with `origin: user`; `coreWired` is true when `knowledge._all` holds the core entry, and `autoLoad` of a core file only takes effect then; `unbound` are files with no binding (a `core/user/` file with no manifest entry has `location: "core"`; `_generated` is skipped); `dangling` are references, in the registry or the manifest, whose file is missing; `orphans` are bindings under a project key that is no longer in `project-context.yaml`; `totals.autoLoadedTokens` counts bound files plus auto-loaded core files when core is wired.
  - Report `warnings` to the user, and report `conflicts` before applying.

- **Exit 1**: stderr holds one plain-text message and nothing was changed, or the change was undone. Do not infer partial success and do not retry by editing files by hand; surface the message. Forms:

  | Message starts with | Meaning |
  |---|---|
  | `Unknown command`, `Unknown flag`, `Unexpected argument`, `Missing value for --<flag>` | Invalid call. |
  | `Missing --type`, `Missing --scope`, `Missing --from` | A required flag is absent. |
  | `Invalid --type`, `Invalid --on-conflict`, `--scope requires --shared or --skill`, `--core cannot be combined with`, `--none cannot be combined with`, `Nothing to move` | Invalid flag combination or value. |
  | `Unknown project "<name>". Valid: ...`, `Unknown skill "<name>". Valid: ...` | The valid names are listed in the message. |
  | `Refusing <path>: ...` | Path outside the managed locations, invalid name, not a regular file, a core file that would be left without a location, or a case-only rename. |
  | `Refusing rename: ... already has the name ...` | The new name equals the current one. |
  | `--skill needs at least one skill name` | `--skill` was given an empty list. |
  | `... must be a YAML mapping ...`, `Unexpected shape: ...` | The registry or core manifest has the wrong structure. |
  | `Cannot validate --scope: ...` | `project-context.yaml` is missing or unreadable while a project scope was requested. |
  | `Cannot read --from`, `Cannot move`, `Cannot rename`, `Nothing to remove` | The file the command needs is missing. |
  | `CONFLICT <kind>: ...` | An unresolved conflict; see Conflicts. |
  | `Missing .mvtt/registry.yaml`, `Failed to parse <file>: ...`, `Not inside an MVTT project` | Project or file state. |
  | `Failed at <step>: ... Rolled back.` | A step failed and everything was undone. |
  | `Failed at <step>: ... Rollback incomplete; backups: <paths>` | Undo also failed; the paths hold the originals to restore by hand. |

## Known limits

- There is no locking; do not run two commands at the same time.
- The registry is rewritten with LF line endings and without comments inside the file; the leading comment block is kept. `last_updated` is not touched.
