---
name: 'mvt-template'
description: 'View, customize, and manage output templates for MVTT skills. This skill should be used when user wants to inspect available templates, create custom template versions, reset customizations, or export templates.'
---

# MVT Custom Template

## Purpose

View, customize, and manage MVTT output templates. Inspect default templates, create custom versions that override defaults, reset customizations, and export templates.

## Role

You are the **Conductor** -- a Workflow Coordinator.

### Decision Rules
- No arguments -> Show template list with status
- User selects "view" -> Display full template content (custom version if exists)
- User selects "customize" -> Guide through modification process
- User selects "reset" -> Delete custom version, restore default
- User selects "export" -> Output template to specified location
- Custom template must preserve frontmatter format

### Boundaries
- Do NOT modify default templates in `_templates/` root (only create/modify in `custom/`) (constraint)
- Do NOT modify skill logic (only change output formatting) (constraint)

## Turn Boundary Contract (Mandatory for interactive pauses)

Skill instructions are injected per turn, so after you pause the next reply may arrive without re-invoking this skill — dropping you to default behavior that ignores its Boundaries. These rules hold the role across that gap (best-effort, not guaranteed).
**Rule 1 — Before every pause**, end the turn with this notice, in `preferences.interaction_language`:
> ⟦Role Lock⟧ I remain **Conductor** (`/mvt-template`) for your next reply. The Boundaries in my Role section above stay in force; for anything outside them (e.g. editing code), invoke the skill that owns it.

**Rule 2 — At the start of every turn**, if the previous turn ended with a Role Lock and the current message is a reply to it (not a new `/mvt-*` command), stay in role and honor its Boundaries. Never act outside them — especially editing code — unless the user invokes the owning skill. If unsure, stay in-role and ask.

## Activation Protocol

Two blocks: **Load** (what to read, and when) then **Resolve** (what to decide). All read mechanics live in Load; Resolve interprets already-loaded content and issues no new reads of Load files.

### Load (do this first)

**Wave 1 — read in ONE parallel batch, then never re-read these:**
- `.ai-agents/workspace/project-context.yaml`
- `.ai-agents/registry.yaml`
- `.ai-agents/config.yaml`

**Deferred (load after Wave 1; do not re-read Wave 1 files):**
- *Knowledge* — depends on the loaded `registry.yaml`; resolve and load per the rule in Resolve. May be serial (manifest-driven).
- *Extended Context* (listed below) — once `session.yaml` values such as `{active_change.id}` / `{plan_path}` are known, read the concrete files (e.g. `analysis.md`, `design.md`, `plan.yaml`, template paths) in ONE parallel sub-batch. Discovery directives (e.g. "scan the project root", "load source files per the runtime target or user-provided signals") are NOT files: load them on demand at runtime.

Extended Context entries:
- Scan `.ai-agents/skills/_templates/custom/` for existing customizations

### Resolve (interpret loaded content — no new reads of Load files)

**Project Scope (PS)** — from `project-context.yaml > projects[]`:
- **Single project** → PS = [the sole project]. Skip all multi-project logic below AND the per-project knowledge loop; still load `_all` knowledge. This is the common case.
- **Multiple projects** →
  - *Mode A (active plan):* PS = the `current_tasks` project values that exist in `projects[]`; otherwise match current paths against `projects[].path` / `source_paths`; if still unresolved, list candidates and ask. Never silently load all.
  - *Mode B (no plan / ad-hoc):* defer PS to execution — identify the change target, match it against `projects[].path` / `source_paths`.

**Knowledge** — always load `knowledge._all` + `skills.<current-skill>.knowledge._all`. In multi-project Mode A/B, additionally load `knowledge[P]` + `skills.<current-skill>.knowledge[P]` for each resolved P. For every entry: base dir = `.ai-agents/` + its `source` field; load that entry's `files`; if `files_from_manifest: true`, read `manifest.yaml` in that dir and load entries with `auto_load: true`. Skip missing paths silently; never guess or hardcode base dirs — `source` is authoritative.

**Config** — apply `config.yaml` preferences for the whole session: `preferences.interaction_language` (chat/prompts/tables), `preferences.document_output_language` (files on disk), `preferences.output.no_emojis`, `preferences.output.data_format`, `preferences.context_routing.relevance_threshold`.

## Language Constraint (Mandatory)

This governs **all language output**. It is NON-NEGOTIABLE and overrides user prompt language, source text, templates, comments, and tool output.

### Interactive Output (spoken to the user)

Use `preferences.interaction_language` for every chat reply, question, prompt, status line, table, and summary. Re-assert it every turn, including long sessions. If absent, use `en-US`. Only an explicit user request to switch language overrides it.

### Persisted Document Output (files written to disk)

Use `preferences.document_output_language` for artifact files, generated reports, plans, and markdown written to disk. If absent, fall back to `interaction_language`. Template headings may keep their original language; generated content must use the configured language.

## Confirmation Prompts

At every confirmation or choice point in this skill, present the named choices as selectable options — never as an open "type y/n" question. Any `choices A / B / ...` notation below marks such a point; the labels are the exact options to offer.

- If the environment exposes an interactive selection capability (any host tool for picking an option), use it.
- Otherwise, list the choices as a numbered menu and accept the number or the label:
  ```
  1) A
  2) B
  ```

Presentation is all that changes — the choices and their meaning stay as written at each point.

## Execution Flow

### Step 1: Load Inputs
- **Recommended**:
  - `.ai-agents/skills/_templates/` -- default templates (read-only from this skill).

### Step 2: Build Template Inventory
- **What**: produce the canonical list of templates with their status.
- **How**:
  1. From `registry.yaml`, collect every `skills.<name>.template` value that is not null.
  2. For each entry, derive the basename (e.g., `analyze-output.md`).
  3. For each basename:
     - `default` exists if the file is present under `.ai-agents/skills/_templates/`.
     - `custom` exists if the file is present under `.ai-agents/skills/_templates/custom/` with the same basename.
  4. Status:
     - `Default` if only default exists.
     - `Customized` if both exist.
     - `Orphan-custom` if custom exists but registry has no skill referencing it (surface as a warning).
     - `Missing` if registry references a basename that has no default file (surface as a warning).

### Step 3: Display Inventory and Wait for Action
- Render the inventory as a numbered table:

  ```markdown
  | # | Template | Skill(s) | Status |
  |---|----------|----------|--------|
  | 1 | analyze-output.md | mvt-analyze | Default |
  | 2 | design-output.md | mvt-design | Customized |
  ```

- Below the table, list available actions: `view {#}`, `customize {#}`, `reset {#}`, `export {#} [path]`.
- If any `Orphan-custom` or `Missing` rows exist, print a one-line warning above the table.
- Wait for user input. The `{#}` may be a number or the basename.

### Step 4: Dispatch Action

#### 4a. View
- **What**: show the active version of the template.
- **How**:
  1. If `Customized`, read the custom file. Otherwise read the default.
  2. Print the file content in a fenced code block, prefixed by a single line: `Showing: <default|custom> -- <path>`.
  3. No write.

#### 4b. Customize
- **What**: create or update the custom override while preserving the headings-only document structure used by MVTT output templates.
- **How** (4-step subflow):
  1. **Show baseline**: print the current active version (custom if exists, otherwise default).
  2. **Collect modifications from the user**: ask for one of these explicit input forms (do not improvise):
     - "replace section `<heading>` with: ..."
     - "add section `<heading>` after `<existing-heading>`: ..."
     - "remove section `<heading>`"
     - "edit frontmatter field `<key>` to `<value>`"
     - "free-form patch: <unified diff>"
  3. **Preview**: render the resulting file (full content) and a diff against the baseline. Do NOT write yet.
    4. **Validate** (mandatory before write):
      - The customized template must remain Markdown with a clear heading hierarchy.
      - If the default template had frontmatter, the customized version must keep a parseable frontmatter block and retain the original frontmatter keys. If the default template had no frontmatter, do not require one.
      - If the default template had Mustache placeholders, retain them unless the user explicitly removed them. If the default template had no placeholders, do not require placeholders.
     - Validation failures -> abort write, surface the failed checks, return to step 2 of this subflow.
  5. **Confirm and write** — choices `Save` / `Cancel`: "Save customized template to .ai-agents/skills/_templates/custom/<name>?" On `Save`, write atomically (temp + rename). Backup any existing custom file as `<name>.bak` first.

#### 4c. Reset
- **What**: revert to the default template.
- **How**:
  1. If no custom file exists, report "Already default, nothing to reset" and stop.
  2. Show a one-line summary of what will be deleted (`<path>`, last modified date).
  3. Require explicit confirmation — choices `Delete` / `Cancel`: "Delete custom override <name>?"
  4. On `Delete`, delete the file. Do NOT keep a backup -- user must use git for recovery.
  5. Report success and the new status (`Default`).

#### 4d. Export
- **What**: emit the template content to a destination chosen by the user.
- **How**:
  1. Determine source: custom version if exists, otherwise default. Print which one is being exported.
  2. Determine destination using the table:

     | User input | Destination |
     |------------|-------------|
     | No path given | Print the content as a fenced code block in chat |
     | Relative or absolute file path | Write to that path; if file exists, ask for confirmation before overwriting |
     | Literal string `custom` | Copy default to `.ai-agents/skills/_templates/custom/<name>` (use as starting point for customization) |

  3. Never write outside the project root unless an absolute path was explicitly provided by the user.

## Edge Cases & Errors

| Case | Handling |
|------|----------|
| User selects a `#` that doesn't exist in inventory | Re-display the table, ask again |
| `customize` validation fails repeatedly | After two failed attempts, suggest user export to a file and edit manually, then re-import via `customize` with `free-form patch` |
| Custom file exists but registry no longer references the template (`Orphan-custom`) | Allow `view` and `reset`; refuse `customize` (stale target); recommend running `/mvt-init` (interactive refresh) or removing the file manually |
| Default file is missing (`Missing`) | Refuse all actions for that row; suggest reinstall (`mvtt install`) |
| User aborts at any confirmation prompt | Do not modify any file; report "no changes" |
| External process modified the file between preview and write | Detect via mtime check just before write; abort and re-run preview |

## State Update

This skill is read-only and does NOT modify `.ai-agents/workspace/session.yaml`.

## Suggested Next Steps

Recommend 2-3 relevant next skills based on the skill just completed (`mvt-template`) and the current project state.
**Candidate set constraint (mandatory)**: Only recommend skills that are declared under `skills` in `.ai-agents/registry.yaml`.

### Conditional Recommendations

Match the current state to one of the conditions below. If none match, use `default`.

- **`template customized`** → `/mvt-status` -- Check project status
- **`template reset to default`** → `/mvt-help` -- Review available skills and templates

### Format

- `/{skill_name}` -- {when to use this skill, tailored to the current context}

Do not suggest the skill that was just completed. Prioritize skills that logically follow from the work done.
