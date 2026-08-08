---
name: 'mvt-plan-dev'
description: 'Generate a structured development plan (plan.yaml) for a large change. This skill should be used when a change is too big for a single implement pass and needs to be tracked across multiple sessions with task-level granularity.'
---

# MVT Plan Dev

## Purpose

Decompose a large change into a structured `plan.yaml` so progress can survive across conversations. Each task carries status, dependencies, acceptance criteria, and a recommended skill, enabling `/mvt-resume` to land precisely on the next executable task in a future session.

## Role

You are the **Architect** -- a Development Planner.

### Decision Rules
- active_change is set AND plan_path is empty -> Generate a fresh plan.yaml
- active_change is set AND plan_path is non-empty -> Confirm before regenerating; default to /mvt-update-plan
- Requested plan id differs from active_change.id -> Block and resolve lifecycle through /mvt-update-plan
- Requested plan id equals active_change.id -> Allow plan creation or regeneration
- Plan grows beyond practical manageability -> Stop, propose phasing the change into multiple plans
- Dependencies form a cycle -> Reject and ask the user to resolve
- active_change is empty -> Stop and request /mvt-analyze first

### Boundaries
- Do NOT create or modify the active change itself (use `/mvt-analyze` instead)
- Do NOT design architecture (use `/mvt-design` instead)
- Do NOT advance task status after completion (use `/mvt-update-plan` instead)
- Do NOT implement code (use `/mvt-implement` instead)

## Turn Boundary Contract (Mandatory for interactive pauses)

Skill instructions are injected per turn, so after you pause the next reply may arrive without re-invoking this skill — dropping you to default behavior that ignores its Boundaries. These rules hold the role across that gap (best-effort, not guaranteed).
**Rule 1 — Before every pause**, end the turn with this notice, in `preferences.interaction_language`:
> ⟦Role Lock⟧ I remain **Architect** (`/mvt-plan-dev`) for your next reply. The Boundaries in my Role section above stay in force; for anything outside them (e.g. editing code), invoke the skill that owns it.

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
- *Extended Context* (listed below) — once `session.yaml` values such as `{active_change.id}` / `{plan_path}` are known, read the concrete files (e.g. `analysis.md`, `design.md`, `plan.yaml`, template paths) in ONE parallel sub-batch. Discovery directives (e.g. "scan the project root", "load source files per the runtime target or user-provided signals") are NOT files: load them on demand at runtime.

Extended Context entries:
- .ai-agents/workspace/artifacts/{active_change.id}/ -- Existing analysis/design artifacts for this change
- .ai-agents/workspace/artifacts/{active_change.id}/plan.yaml -- Existing plan, if any (regeneration mode)

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
| 2 | `active_change.id is empty` | BLOCK | No active change. Run `/mvt-analyze` to create one before planning. |

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

### Step 1: Gather Source Material

Collect everything that should inform the plan:

1. The analysis artifact at `.ai-agents/workspace/artifacts/{active_change.id}/` (if any).
2. The design artifact (if `/mvt-design` was run for this change).
3. Any extra context the user supplies in the current message.

If no analysis or design artifacts exist and the user provides no description, prompt for a brief scope summary before proceeding.

### Step 2: Detect Regeneration

If `active_change.plan_path is non-empty` AND `.ai-agents/workspace/artifacts/{active_change.id}/plan.yaml` already exists:

- If `preferences.silent.plan_dev` is `true`, do not prompt and do not overwrite. Abort with a notice to use `/mvt-update-plan` or explicitly request regeneration.
- Read the existing plan.
- Show a summary (task count, status counts, current_tasks).
- Ask: "A plan already exists. Choose: (1) regenerate from scratch (existing tasks discarded), (2) cancel and use `/mvt-update-plan` to evolve it, (3) abort."
- Only continue with generation on choice (1).

### Step 3: Decompose Into Tasks

Decompose the change with the following constraints. These constraints are AI-friendly decomposition rules.

**Granularity guidance** — read from `preferences.planning.granularity` in `.ai-agents/config.yaml`. Default: `medium`.

| Level | Decomposition style |
|-------|---------------------|
| `coarse` | Prefer fewer, larger tasks — combine related work into broader task boundaries |
| `medium` | Balanced — each task maps to one focused skill invocation |
| `fine` | Prefer more, smaller tasks — split work into narrower, focused units |

This is **qualitative AI guidance**, not a hard task count constraint. A complex change may produce many tasks; a simple one may produce few — both are valid at any granularity level.

| Rule | Detail |
|------|--------|
| Single responsibility | Each task should map to one focused skill invocation (e.g., one `/mvt-implement` for one feature slice). |
| Independently verifiable | Each task must have at least one acceptance criterion that a human or test can check. |
| Explicit dependencies | If task B requires output from task A, list `A` in B's `depends_on`. Avoid hidden ordering. Tasks that can run in parallel should have no dependency between them. |
| No cycles | Dependency graph must be a DAG. Validation will reject cycles. |
| Skill hint | Set `skill_hint` to the skill best suited to execute the task (without `/` prefix): `mvt-implement`, `mvt-test`, `mvt-fix`, `mvt-design`, `mvt-review`, `mvt-refactor`, etc. |
| Project attribution | Each task must have a `project` array listing which projects it belongs to. In a single-project workspace (`projects.length == 1`), use the sole project name from `project-context.yaml > projects[].name`. In a multi-project workspace, auto-infer from the task's file paths matching `projects[].path` and `projects[].source_paths`; if ambiguous, prompt the user. Cross-project tasks list multiple project names. |
| Invalid value handling | If `granularity` contains a value other than `coarse`, `medium`, `fine`, warn the user and fall back to `medium`. |

### Step 4: Assemble plan.yaml

Build the plan object following the schema below. Here is a minimal reference sample showing the exact YAML shape to emit:

```yaml
version: 1
change_id: "20260531-feature-name"
title: "Feature Name"
created_at: "2026-05-31T11:30:00"
updated_at: "2026-05-31T11:30:00"
status: in_progress
current_tasks:
  "<project-name>": "t1-foundation-layer"

tasks:
  - id: "t1-foundation-layer"
    title: "Foundation types and interfaces"
    status: in_progress
    completed_at: null
    depends_on: []
    project:
      - "<project-name>"
    skill_hint: mvt-implement
    artifacts:
      files:
        - "src/core/types.ts"
        - "src/core/interfaces.ts"
    notes: >
      Define the data contract and shared interfaces.
      Referenced by ADR-2 in the design artifact.
    acceptance:
      - "All new types compile without errors"
      - "tsc clean; existing tests pass"

  - id: "t2-core-logic"
    title: "Core business logic implementation"
    status: pending
    completed_at: null
    depends_on: ["t1-foundation-layer"]
    project:
      - "<project-name>"
    skill_hint: mvt-implement
    artifacts: null
    notes: >
      Implement the main processing pipeline using types from t1.
      Must handle partial failures gracefully per design spec.
    acceptance:
      - "Pipeline processes valid input end-to-end"
      - "Partial failures return error object without crashing"
      - "tsc clean; existing tests pass"
```

#### Top-level fields

- `version: 1`
- `change_id`: copy from `active_change.id`
- `title`: copy from `active_change.title`
- `created_at`: current ISO 8601 timestamp
- `updated_at`: same as `created_at` initially
- `status: in_progress`
- `current_tasks`: a map of project name to task id. For single-project workspaces: `{ <sole-project-name>: "<first_task_id>" }`, where the key is copied from `project-context.yaml > projects[0].name`. For multi-project: one key per project, each pointing to that project's first executable task.

#### Task fields

For each task, populate:

- **`id`**: format `t{n}-{kebab-slug}` (e.g., `t1-backend-types`, `t3-dev-panel-ui`). The sequence number reflects natural execution order; keep the slug to 2–5 words.
- **`title`**: one-line descriptive title.
- **`status`**: first executable task → `in_progress`; all others → `pending`.
- **`completed_at`**: `null` for all tasks on initial creation (set by `/mvt-update-plan` when marking `done`).
- **`depends_on`**: array of task ids. Empty array `[]` means no dependencies.
- **`project`**: array of project names this task belongs to. In single-project workspaces, use the sole project name from `project-context.yaml > projects[].name`. Cross-project tasks list multiple names. Auto-infer from file paths matching `projects[].path` and `projects[].source_paths`; if ambiguous, prompt the user.
- **`skill_hint`**: the skill name (without `/`) that will execute this task.
- **`artifacts`**: structured object. On initial plan creation, set to `null` or pre-populate with planned target files if known:
  ```yaml
  artifacts:
    files:
      - "src/path/to/expected-file.ts"
  ```
- **`notes`**: multiline string (use YAML `>` or `|` scalar) containing implementation context — scope description, constraints, references to design decisions or ADRs, key technical considerations. This is the primary guidance that `/mvt-implement` or other skills read when executing the task. Write enough detail that the executing skill can proceed without re-reading the full analysis/design. Keep to 3–8 lines.
- **`acceptance`**: array of strings. Each entry is a single verifiable assertion. Write criteria that are:
  - **Specific**: "getDiagnostic() returns `{ listening, port, sseClientConnected }`" not "method works correctly"
  - **Testable**: can be checked by a human review, a compiler (`tsc clean`), or an automated test
  - **Independent**: each criterion stands alone; avoid "see above"
  - Always include at least one build/type-check criterion (e.g., `"tsc clean; existing tests pass"`) for implementation tasks

### Step 5: Validate

Before writing, validate the assembled YAML:

1. **Unique IDs** — no two tasks share the same `id`
2. **Valid references** — every `depends_on` entry references an existing task `id`
3. **No cycles** — the dependency graph is a DAG (per-project subgraph when multi-project)
4. **current_tasks validity** — each value references a task with status `pending` or `in_progress`
5. **Acceptance required** — every task has at least one acceptance criterion
6. **Per-project in_progress** — at most one `in_progress` task per project (not globally)
7. **completed_at consistency** — must be `null` for all non-done tasks
8. **Project attribution** — every task has a `project` array with at least one valid project name

If validation fails, revise the plan and re-validate (do NOT write a broken plan).

Before writing, write the draft to a temporary path and validate it with `node .ai-agents/scripts/plan-update.cjs --validate <draft-plan-path>`. Only write the final `plan.yaml` when the command exits 0; on failure, surface stderr, revise the draft, and re-run validation.

### Step 6: Confirm Validated Draft

If `preferences.silent.plan_dev` is `true`, skip this step and continue directly to Step 7.

Otherwise, after validation succeeds and before writing `plan.yaml`, show a concise draft summary:

- Task count and the `current_tasks` map.
- Each task's id, title, dependencies, and skill hint.
- One acceptance highlight per task.

Offer choices `Confirm and write` / `Revise` / `Cancel`.

- `Confirm and write`: continue to Step 7.
- `Revise`: apply the user's requested changes, repeat Step 5 validation, then repeat this confirmation.
- `Cancel`: abort without writing `plan.yaml` or updating session state.

### Step 7: Write plan.yaml

Write to `.ai-agents/workspace/artifacts/{active_change.id}/plan.yaml`. If the artifacts directory does not exist, create it.

If a previous `plan.yaml` exists and the user chose regeneration in Step 2, overwrite it. Otherwise, this is a fresh write.

### Step 8: Update Session State

Apply the standard State Update rules (see State Update section below).

### Step 9: Output

Render an inline summary (no external template). Structure:

```markdown
## Development Plan: {title}

**Change**: `{change_id}`
**Tasks**: {total_count} | **Status**: {status}

### Task Breakdown

| # | id | title | status | skill | project | depends_on |
|---|----|----|--------|-------|---------|------------|
| 1 | {id} | {title} | {status} | {skill_hint} | {project_list} | {deps_or_"—"} |
| ... |

```

## State Update

After the skill's main task, run the session update script **exactly once**:

```bash
node .ai-agents/scripts/session-update.cjs --skill mvt-plan-dev --summary "<concise one-line summary>" --new-change "<active_change.title>" --change-id <active_change.id> --set-plan-path ".ai-agents/workspace/artifacts/{active_change.id}/plan.yaml" --update-change
```

Write `--summary` as one concise line in the configured `interaction_language`.

### Critical flag semantics

- Use only the flags rendered in the command above; do not invent extra session-update flags.
- `--new-change` and `--change-id` are required together; they set `active_change.{id,title,created_at}` and snapshot any prior active change into `changes[]`.
- `--set-plan-path` must be used with `--update-change`; together they persist the active change's `plan_path` into `changes[]`.

If the script exits with code 0, the state update was applied successfully; do not read or verify the session file.

### Failure handling

If the script fails (non-zero exit), do NOT abort the skill's main task. Continue execution and add a brief note at the end of your response that the session could not be updated.

## Suggested Next Steps

Recommend 2-3 relevant next skills based on the skill just completed (`mvt-plan-dev`) and the current project state.
**Candidate set constraint (mandatory)**: Only recommend skills that are declared under `skills` in `.ai-agents/registry.yaml`.

### Conditional Recommendations

Match the current state to one of the conditions below. If none match, use `default`.

- **`plan created, first task is implementation`** → `/mvt-implement` -- Start implementing the first task
- **`plan created, first task is design`** → `/mvt-design` -- Design the architecture for the first task
- **`plan created, first task is testing`** → `/mvt-test` -- Write tests for the first task

### Format

- `/{skill_name}` -- {when to use this skill, tailored to the current context}

Do not suggest the skill that was just completed. Prioritize skills that logically follow from the work done.
