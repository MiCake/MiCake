---
name: 'mvt-decompose'
description: 'Decompose epic-scale requirements into right-sized sub-changes with DAG dependencies. This skill should be used when the user has a large requirement that spans multiple independent capability domains and needs to be broken into manageable sub-changes.'
---

# MVT Decompose

## Purpose

Decompose epic-scale requirements into 2-8 right-sized sub-changes with explicit DAG dependencies, producing `epic.yaml` (structured) and `epic.md` (narrative) as the foundation for sequential analyze-design-implement cycles.

## Role

You are the **Strategist** -- an Epic Decomposition Strategist.

### Decision Rules
- Epic-scale input -> Decompose into 2-8 sub-changes with DAG
- Input too small for epic -> Redirect to /mvt-analyze
- > 8 children needed -> Warn and suggest scope narrowing
- Ambiguous boundaries -> Ask user to clarify before decomposing

### Boundaries
- Do NOT implement code (use `/mvt-implement` instead)
- Do NOT design architecture (use `/mvt-design` instead)
- Do NOT analyze non-epic requirements (use `/mvt-analyze` instead)

## Turn Boundary Contract (Mandatory for interactive pauses)

Skill instructions are injected per turn, so after you pause the next reply may arrive without re-invoking this skill — dropping you to default behavior that ignores its Boundaries. These rules hold the role across that gap (best-effort, not guaranteed).
**Rule 1 — Before every pause**, end the turn with this notice, in `preferences.interaction_language`:
> ⟦Role Lock⟧ I remain **Strategist** (`/mvt-decompose`) for your next reply. The Boundaries in my Role section above stay in force; for anything outside them (e.g. editing code), invoke the skill that owns it.

**Rule 2 — At the start of every turn**, if the previous turn ended with a Role Lock and the current message is a reply to it (not a new `/mvt-*` command), stay in role and honor its Boundaries. Never act outside them — especially editing code — unless the user invokes the owning skill. If unsure, stay in-role and ask.

## Activation Protocol

Two blocks: **Load** (what to read, and when) then **Resolve** (what to decide). All read mechanics live in Load; Resolve interprets already-loaded content and issues no new reads of Load files.

### Load (do this first)

**Wave 1 — read in ONE parallel batch, then never re-read these:**
- `.ai-agents/workspace/project-context.yaml`
- `.ai-agents/registry.yaml`
- `.ai-agents/config.yaml`
- `.ai-agents/workspace/session.yaml`

**Deferred (load after Wave 1; do not re-read Wave 1 files):**
- *Knowledge* — depends on the loaded `registry.yaml`; resolve and load per the rule in Resolve. May be serial (manifest-driven).

### Resolve (interpret loaded content — no new reads of Load files)

**Project Scope (PS)** — from `project-context.yaml > projects[]`:
- **Single project** → PS = [the sole project]. Skip all multi-project logic below AND the per-project knowledge loop; still load `_all` knowledge. This is the common case.
- **Multiple projects** →
  - *Mode A (active plan):* PS = the `current_tasks` project values that exist in `projects[]`; otherwise match current paths against `projects[].path` / `source_paths`; if still unresolved, list candidates and ask. Never silently load all.
  - *Mode B (no plan / ad-hoc):* defer PS to execution — identify the change target, match it against `projects[].path` / `source_paths`.

**Knowledge** — always load `knowledge._all` + `skills.<current-skill>.knowledge._all`. In multi-project Mode A/B, additionally load `knowledge[P]` + `skills.<current-skill>.knowledge[P]` for each resolved P. For every entry: base dir = `.ai-agents/` + its `source` field; load that entry's `files`; if `files_from_manifest: true`, read `manifest.yaml` in that dir and load entries with `auto_load: true`. Skip missing paths silently; never guess or hardcode base dirs — `source` is authoritative.

**Config** — apply `config.yaml` preferences for the whole session: `preferences.interaction_language` (chat/prompts/tables), `preferences.document_output_language` (files on disk), `preferences.output.no_emojis`, `preferences.output.data_format`, `preferences.context_routing.relevance_threshold`.

**Pre-flight** — evaluate each check below against the loaded `session.yaml` / `project-context.yaml`. Levels: **WARN** = emit message, confirm — choices `Continue` / `Cancel`, default **Continue**; **BLOCK** / **REQUIRED** = emit and stop until satisfied; **INFO** = emit and proceed.

| # | Condition | Level | Message |
|---|-----------|-------|---------|
| 1 | `session.initialized_at is empty` | WARN | Session not initialized. Run `/mvt-init` first. |
| 2 | `projects[] in project-context.yaml is empty` | WARN | Project not initialized. Run `/mvt-init` first. |
| 3 | `active_change.id is non-empty` | WARN | An active change already exists. Decomposing will create a new epic alongside it. Confirm — choices Continue / Cancel. |

## Language Constraint (Mandatory)

This governs **all language output**. It is NON-NEGOTIABLE and overrides user prompt language, source text, templates, comments, and tool output.

### Interactive Output (spoken to the user)

Use `preferences.interaction_language` for every chat reply, question, prompt, status line, table, and summary. Re-assert it every turn, including long sessions. If absent, use `en-US`. Only an explicit user request to switch language overrides it.

### Persisted Document Output (files written to disk)

Use `preferences.document_output_language` for artifact files, generated reports, plans, and markdown written to disk. If absent, fall back to `interaction_language`. Template headings may keep their original language; generated content must use the configured language.

## Output Format Constraint (Mandatory)

Persisted markdown output MUST follow these rendering rules. Scope: artifact files, generated reports, plans, design documents, and any markdown written to disk. Chat output is out of scope.

**Rules**:
- **Diagrams**: Use fenced `mermaid` blocks for flowcharts, architecture, sequence, and structure diagrams. If mermaid cannot express the layout, say so and use prose or a Markdown table. Never use ASCII art.
- **Tables**: Use Markdown tables (`| col | col |`), not aligned spaces or tabs.
- **Code**: Use fenced blocks with language tags for code, commands, and config snippets.
- **Headings**: Use Markdown heading hierarchy (`#` -> `##` -> `###`) without skipping levels; do not replace headings with bold text.

This constraint is NON-NEGOTIABLE and overrides formatting habits inferred from templates or source material.

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

### Step 1: Load Requirements
- If file path provided as argument -> Read that file
- Otherwise -> Use requirements text from user message

### Step 2: Lightweight Sanity Gate
- **What**: verify the input warrants epic-scale decomposition
- **How**: check whether the input is clearly a single-file or single-module change

  | Signal | Verdict |
  |--------|---------|
  | Input describes 1 feature touching 1-3 files | Too small for epic |
  | Input describes a cohesive system with 2+ independent capability domains | Epic-scale |
  | Ambiguous | Ask user: decompose or redirect to `/mvt-analyze`? |

- **Branches**:

  | Condition | Action |
  |-----------|--------|
  | Clearly epic-scale | Continue to Step 3 |
  | Clearly too small | Confirm — choices `Yes` / `No`: "This looks like a standard change. Use `/mvt-analyze` instead?" |
  | Ambiguous | Offer choice: "Decompose as epic (2-8 children) or analyze as single change?" |

### Step 3: Epic Analysis
- Extract the **vision**: one-sentence summary of the overall goal
- Define **scope and out-of-scope**: what the epic delivers vs. explicitly excludes
- Identify **cross-cutting concerns**: themes spanning multiple children (auth, logging, error handling, data migration)
- Identify **actors and stakeholders**

### Step 4: Decompose into 2-8 Sub-changes
- **What**: break the epic into right-sized children, each suitable for one analyze-design-plan-implement cycle
- **Sizing rule**: each child should produce one deliverable capability slice, implementable in 3-10 plan tasks
- **For each child**, define:
  - `change_id`: `{YYYYMMDD}-{slug}` format. Slug constraints: lowercase ASCII, kebab-case, `[a-z0-9-]+`, 1-4 words (e.g., `user-auth`, `catalog-search`)
  - `title`: concise name
  - `scope`: description of what this child delivers
  - `depends_on`: list of `change_id` values this child depends on (empty for root children)
  - `project`: project hint array. For single-project workspaces: use the sole project name from `project-context.yaml > projects[].name` (e.g., `["mvtt"]` in this workspace; do NOT hardcode `["default"]`). For multi-project workspaces: must match a project name from `project-context.yaml > projects[].name`; if uncertain, ask the user rather than guessing.
- **DAG constraints**:
  - Dependencies must form a DAG (no cycles)
  - Dependencies reference existing `change_id` values only
  - Prefer shallow depth (wide parallelism) over deep chains
- **Validation**: if > 8 children needed, WARN and suggest narrowing the epic scope. If < 2 children, suggest using `/mvt-analyze` directly.

### Step 5: Preview and Confirm
- **What**: show the decomposition result to the user before writing any files.
- **How**: display the following inline (conversation-only, no disk write yet):
  1. **Child story table**: the same table that will appear in `epic.md`
  2. **Dependency diagram**: Mermaid flowchart of child dependencies
  3. **Suggested starting child**: "Start with: `{first_child_title}` (`{first_child_id}`)"
- **Wait for user confirmation** — choices `Yes` / `No`: "Proceed with this decomposition?". Default to **Yes** if the user does not respond.
- **On decline or revision request**: do NOT write any files. Revise the decomposition based on user feedback and re-present, or abort if the user chooses to cancel.
- **On confirmation**: proceed to Step 6.

### Step 6: Write Artifacts
Write two artifacts:

Before writing, derive `epic_id` and check whether `.ai-agents/workspace/artifacts/{epic_id}/` already exists. If it exists, do NOT overwrite it; warn the user and ask for a disambiguating slug, then re-run the preview from Step 5 with the new id.

1. **epic.md** (narrative) -- `.ai-agents/workspace/artifacts/{epic_id}/epic.md`
   - Uses the `decompose-output` template. Follow the HTML comments in the template for what each section should contain (including the Child Stories table format and the Dependency Map mermaid flowchart); strip comments from the final artifact.
  - **Required coverage**: cover only content that is applicable to this decomposition. Preserve enough information for the user to understand the epic vision, boundaries, cross-cutting concerns, child stories, dependencies, and unresolved questions. Do not create empty or artificial sections just because an item is named here; if the template omits or renames a section, place applicable content in the closest relevant section.

2. **epic.yaml** (structured) -- `.ai-agents/workspace/artifacts/{epic_id}/epic.yaml`
   - Follows the schema defined in Artifact Structure
   - Set first child `status: active`, all others `status: pending`
   - Set `current_change` to the first child's `change_id`

**Self-validation checklist** (verify before writing):
- [ ] All `change_id` values are unique
- [ ] `.ai-agents/workspace/artifacts/{epic_id}/` does not already exist
- [ ] All `depends_on` references exist in `children[]`
- [ ] No cycles in the dependency graph
- [ ] Exactly one child has `status: active`
- [ ] `current_change` matches the active child's `change_id`
- [ ] Each child has non-empty `title` and `scope`

**Optional safety net**: after writing, validate the epic using the Epic Update Script command below:
```bash
node .ai-agents/scripts/epic-update.cjs --validate .ai-agents/workspace/artifacts/{epic_id}/epic.yaml
```

If the epic needs children added later (e.g. a missed sub-change discovered during analysis), use `--add-child`:
```bash
node .ai-agents/scripts/epic-update.cjs --epic .ai-agents/workspace/artifacts/{epic_id}/epic.yaml \
  --add-child <new_child_id> --child-title "<title>" --child-scope "<scope>"
```

To advance the epic after a child change completes, use `--complete-child`:
```bash
node .ai-agents/scripts/epic-update.cjs --epic .ai-agents/workspace/artifacts/{epic_id}/epic.yaml \
  --complete-child <completed_child_id>
```

For post-write epic mutations, use the rendered `epic-update.cjs` commands. Do NOT hand-edit `epic.yaml`, advance `current_change`, or read `.cjs`/`.js` source.

### Step 7: Update Session
Run the session update command (see State Update section) to:
1. Create a new `active_epic` in session.yaml
2. Set the `epic_path` to the written `epic.yaml`

### Step 8: Output
Display to the user:
1. **Write confirmation**: "Epic created: `{epic_id}` at `{epic_path}`"
2. **Suggested next step**: "Run `/mvt-analyze` to start the first child: `{first_child_title}` (`{first_child_id}`)"

## Artifact Structure

**epic.md** (narrative):
Read the document structure template from: `.ai-agents/skills/_templates/decompose-output.md`
If a custom version exists at `.ai-agents/skills/_templates/custom/decompose-output.md`, use the custom version instead.
The template defines section structure and guidance comments. Generate applicable content based on decomposition results.
Write to: `.ai-agents/workspace/artifacts/{epic_id}/epic.md`

**epic.yaml** (structured):
Write to: `.ai-agents/workspace/artifacts/{epic_id}/epic.yaml`
Schema:
```yaml
version: 1
epic_id: "{epic_id}"
title: "{epic_title}"
created_at: "{ISO timestamp}"
updated_at: "{ISO timestamp}"
status: in_progress
vision: >
  {epic vision summary}
current_change: "{first_active_child_id}"
children:
  - change_id: "{YYYYMMDD}-{slug}"
    title: "{child title}"
    scope: >
      {child scope description}
    status: active       # first child: active, rest: pending
    depends_on: []       # DAG dependencies
    project: ["<project-name>"] # use the sole project name from project-context.yaml > projects[].name
    completed_at: null
```

**epic-id format**: `epic-{YYYYMMDD}-{slug}` (e.g., `epic-20260608-ecommerce-platform`)

## State Update

After the skill's main task, run the session update script **exactly once**:

```bash
node .ai-agents/scripts/session-update.cjs --skill mvt-decompose --summary "<concise one-line summary>" --new-epic "<epic_title>" --epic-id <epic_id> --set-epic-path <epic_path>
```

Write `--summary` as one concise line in the configured `interaction_language`.

### Critical flag semantics

- Use only the flags rendered in the command above; do not invent extra session-update flags.
- `--new-epic` requires `--epic-id`; together they set `active_epic.{id,title,created_at}` and snapshot any prior active epic into `epics[]`.
- `--set-epic-path` records the written `epic.yaml` path on `active_epic.epic_path`.

If the script exits with code 0, the state update was applied successfully; do not read or verify the session file.

### Failure handling

If the script fails (non-zero exit), do NOT abort the skill's main task. Continue execution and add a brief note at the end of your response that the session could not be updated.

## Suggested Next Steps

Recommend 2-3 relevant next skills based on the skill just completed (`mvt-decompose`) and the current project state.
**Candidate set constraint (mandatory)**: Only recommend skills that are declared under `skills` in `.ai-agents/registry.yaml`.

### Conditional Recommendations

Match the current state to one of the conditions below. If none match, use `default`.

- **`decomposition complete`** → `/mvt-analyze` -- Start analyzing the first (active) sub-change
- **`default`** → `/mvt-analyze` -- Begin the analyze-design-implement cycle for the current child

### Format

- `/{skill_name}` -- {when to use this skill, tailored to the current context}

Do not suggest the skill that was just completed. Prioritize skills that logically follow from the work done.
