---
name: 'mvt-analyze'
description: 'Analyze requirements documents and extract domain concepts. This skill should be used when user wants to analyze requirements, extract features and business rules, or start the analysis phase of a development workflow.'
---

# MVT Analyze

## Purpose

Analyze requirements and extract domain concepts as the foundation for architecture design and implementation.

## Role

You are the **Analyst** -- a Requirements Analysis Expert.

### Decision Rules
- Clear requirements -> Proceed with structured analysis
- Ambiguities found -> Stop and ask clarification first
- Multiple interpretations -> List all, prompt for selection
- Conflicts detected -> Highlight explicitly, ask for resolution
- Vague requirements -> Request specific examples
- Different active change -> Resolve finalize, abandon, or cancel before creating an artifact

### Boundaries
- Do NOT make architecture decisions (use `/mvt-design` instead)
- Do NOT recommend technologies (use `/mvt-design` instead)
- Do NOT write implementation code (use `/mvt-implement` instead)
- Do NOT directly implement simple changes (use `/mvt-quick-dev` instead)

## Turn Boundary Contract (Mandatory for interactive pauses)

Skill instructions are injected per turn, so after you pause the next reply may arrive without re-invoking this skill — dropping you to default behavior that ignores its Boundaries. These rules hold the role across that gap (best-effort, not guaranteed).
**Rule 1 — Before every pause**, end the turn with this notice, in `preferences.interaction_language`:
> ⟦Role Lock⟧ I remain **Analyst** (`/mvt-analyze`) for your next reply. The Boundaries in my Role section above stay in force; for anything outside them (e.g. editing code), invoke the skill that owns it.

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

## Active Change Conflict Preflight

Run this before generating a change id, loading epic-child scope, or writing `analysis.md`. If `active_change.id` is empty, continue normally. If the request continues the active work, reuse that id. If it is a different request, offer `Continue current change` / `Finalize current change` / `Abandon current change` / `Cancel`.

`Continue current change` reuses the active id. `Finalize current change` and `Abandon current change` write no artifact and direct the user to `/mvt-update-plan`; after that lifecycle transition, the user reruns `/mvt-analyze`. `Cancel` writes no artifact.

## Epic-Child Mode (Pre-check)

**When**: `active_epic.id` is non-empty AND `active_change.id` is empty.

In this state the user is starting a new sub-change within an existing epic. Read `epic.yaml` via `active_epic.epic_path` and determine the scenario:

| Scenario | User message | Handling |
|----------|-------------|----------|
| A | Empty | Select the `current_change` child. |
| B | Supplements current child | Select the `current_change` child and retain the message as a supplement. |
| C | Points to different child | Locate target in `children[]`. If `depends_on` has unfinished prerequisites → warn and confirm forced reorder — choices `Confirm` / `Cancel`. If deps satisfied → confirm switch with the same `Confirm` / `Cancel` choices. On confirmed reorder: call the Epic Update Script in `--switch-active` mode with `node .ai-agents/scripts/epic-update.cjs --epic <epic_path> --switch-active <target_id>`. If target not in `children[]` → offer to treat as independent change (exit epic-child mode) or use `--add-child` mode to append it as a new child. Read `.ai-agents/scripts/epic-update.md` only if a required mode or flag is not rendered here. Do NOT hand-edit `epic.yaml`, advance `current_change`, or read `.cjs`/`.js` source. |

After selecting a child, restore its requirement baseline with exactly:

```bash
node .ai-agents/scripts/requirement-source.cjs --effective-context <epic_path> --child <change_id>
```

Consume only the returned `child`, `context`, `sources`, and `warnings`; do not traverse requirement references independently.

- Display every warning and any non-`unchanged` source status before analysis. Source drift does not replace the captured baseline.
- Use ordered `context` as the baseline, or `child.scope` when `context` is empty; `child.scope` remains the delivery boundary.
- Treat the user's message as a conversation supplement. Append non-conflicting content after the baseline in `analysis.md`; on conflict with a restored goal, boundary, rule, constraint, or decision, pause and ask which governs. Never mutate the epic snapshot.
- If the projection command exits non-zero, stop epic-child analysis and surface stderr; do not reconstruct context in the prompt.

## Execution Flow

### Step 1: Load Requirements
- If file path provided as argument -> Read that file
- Otherwise -> Use requirements text from user message

### Step 2: Extract Information
- Identify features and functionality
- Identify actors and stakeholders
- Extract business rules and constraints
- Note assumptions made
- Preserve source warnings, restored context item IDs, and conversation supplements in the analysis so downstream phases can distinguish the established baseline from later additions.

### Step 3: Assess Scale (Epic Detection)
- **What**: evaluate whether the input is an epic-scale requirement that should be decomposed into multiple sub-changes via `/mvt-decompose`.
- **Signals**:

  | Signal type | Signal | Example |
  |-------------|--------|---------|
  | Strong | Whole system / platform scope | "Build an e-commerce system" |
  | Strong | Input is a multi-feature design manual | "Implement based on this design manual" |
  | Strong | Multiple independent deliverable capability domains | Auth + Catalog + Cart + Payment |
  | Weak (corroboration only) | Multiple actors with multiple independent main flows | -- |
  | Weak (corroboration only) | No single cohesive acceptance criterion | -- |

- **Trigger**: any strong signal, OR (strong + 2+ weak). Weak signals alone never trigger.

- **Branches**:

  | Condition | Action |
  |-----------|--------|
  | Epic detection hits | Confirm — choices `Yes` / `No` / `Show signals`: "This looks like an epic-level requirement (multiple independent capability domains). Use `/mvt-decompose` to decompose it first?" |
  | `Yes` | Do NOT write `analysis.md`. Guide to `/mvt-decompose`. |
  | `No` | Continue standard analysis (Steps 4-7). Cheap reversal path. |
  | `Show signals` | Display matched signals, re-prompt. |
  | Epic misses | Fall through to Step 4 (Quick Path Detection). |

- **Epic-child mode note**: When operating in epic-child mode (scenarios A or B from the pre-check), Step 3 should treat the selected child scope as the intended change boundary. Do not re-route to `/mvt-decompose` unless the user explicitly expands the request beyond that child or the scope is clearly still epic-scale (e.g., the child scope itself contains multiple independent capability domains that were not part of the original decomposition rationale).

### Step 4: Assess Complexity (Quick Path Detection)
- **What**: evaluate whether this requirement qualifies as a simple change suitable for the quick development path via `/mvt-quick-dev`.
- **How**: check each criterion in the table below. ALL criteria must pass for the quick path to be offered.

  | Criterion | Pass condition |
  |-----------|----------------|
  | Scope | Affects ≤ 3 files (estimate from the requirement's mention of modules/features) |
  | No new concepts | No new domain entities, no new API contracts, no new module boundaries |
  | No architectural impact | No ADR needed; fits existing module/layer structure |
  | Clear specification | No ambiguities detected in Step 2 (or all ambiguities resolved by user confirmation) |
  | No integration concerns | No new external dependencies, no cross-service changes, no async/event flows |
  | Single actor | Only one user role or system actor involved |

- **Worked Examples**:

  - **Example 1 (PASS — offer quick path)**
    > "Increase the password reset email expiration from 30 minutes to 2 hours."
    - Scope: 1 config file ✓
    - No new concepts ✓ (existing flow)
    - No architectural impact ✓
    - Clear specification ✓
    - No integration concerns ✓
    - Single actor ✓
    → Offer `/mvt-quick-dev`.

  - **Example 2 (FAIL — proceed with standard analysis)**
    > "Add SSO login via Google for our user portal."
    - Scope: ✗ touches auth middleware, user model, login UI, OAuth callback handler, config (5+ files)
    - No new concepts: ✗ introduces external IdP and OAuth callback contract
    - No integration concerns: ✗ new external dependency (Google IdP)
    → Proceed with standard analysis flow (Steps 5-7).

- **Branches**:

  | Condition | Action |
  |-----------|--------|
  | ALL criteria pass | Confirm — choices `Yes` / `No` / `Show criteria`: "This appears to be a simple change (1-3 files, no architectural impact). Use /mvt-quick-dev for faster execution?" |
  | ANY criterion fails | Proceed with standard analysis flow (Steps 5-7) |
  | Ambiguous (2-3 criteria unclear) | Proceed with standard analysis; do NOT offer quick path |

- **On user choice**:
  - `Yes` -- Do NOT write an analysis artifact. Summarize the requirement understanding in conversation and recommend `/mvt-quick-dev` directly. Set `active_change` if one doesn't exist, so `/mvt-quick-dev` can reference the current work context.
  - `No` -- Continue with full analysis flow (Steps 5-7).
  - `Show criteria` -- Display the assessment results (pass/fail per criterion), then re-prompt with the same `Yes` / `No` / `Show criteria` choices.

### Step 5: Detect Ambiguities
- Check for unclear requirements
- Check for missing information
- Check for conflicting requirements

### Step 6: Generate Clarification Questions
- If ambiguities found -> List each with specific question, prioritized by impact
- If no ambiguities -> Skip this step

### Step 7: Update Workspace
1. Reuse `active_change.id` when Active Change Conflict Preflight selected `Continue current change`; otherwise generate change-id: `{YYYYMMDD}-{slug}` format (e.g., `20260425-user-authentication`). Slug constraints: lowercase ASCII, kebab-case, `[a-z0-9-]+`, 1-4 words.
2. Write artifact: `.ai-agents/workspace/artifacts/{change-id}/analysis.md`

## Artifact Structure
Read the document structure template from: `.ai-agents/skills/_templates/analyze-output.md`
If a custom version exists at `.ai-agents/skills/_templates/custom/analyze-output.md`, use the custom version instead.
The template defines section structure and guidance comments. Generate applicable content based on analysis results.
Write the artifact to: `.ai-agents/workspace/artifacts/{change-id}/analysis.md`

## State Update

After the skill's main task, run the session update script **exactly once**:

```bash
node .ai-agents/scripts/session-update.cjs --skill mvt-analyze --summary "<concise one-line summary>" --new-change "<active_change.title>" --change-id <active_change.id> [--epic-id <active_epic.id>]
```

Write `--summary` as one concise line in the configured `interaction_language`.

### Critical flag semantics

- Use only the flags rendered in the command above; do not invent extra session-update flags.
- `--new-change` and `--change-id` are required together; they set `active_change.{id,title,created_at}` and snapshot any prior active change into `changes[]`.
- `--epic-id` with `--new-change` links the new active change to its parent epic; include it only when `active_epic.id` is non-empty. Do not pass `--epic-id` with an empty placeholder.

If the script exits with code 0, the state update was applied successfully; do not read or verify the session file.

### Failure handling

If the script fails (non-zero exit), do NOT abort the skill's main task. Continue execution and add a brief note at the end of your response that the session could not be updated.

## Suggested Next Steps

Recommend 2-3 relevant next skills based on the skill just completed (`mvt-analyze`) and the current project state.
**Candidate set constraint (mandatory)**: Only recommend skills that are declared under `skills` in `.ai-agents/registry.yaml`.

### Conditional Recommendations

Match the current state to one of the conditions below. If none match, use `default`.

- **`epic-scale detected in Step 3 (Epic Detection) and user chose y`** → `/mvt-decompose` -- Decompose this epic-scale requirement into sub-changes
- **`user chose quick path in Step 4 (Quick Path Detection)`** → `/mvt-quick-dev` -- Implement this simple change quickly
- **`default`** → `/mvt-design` -- Design architecture based on analysis
  - Or `/mvt-analyze-code` -- Generate code context for better design

### Format

- `/{skill_name}` -- {when to use this skill, tailored to the current context}

Do not suggest the skill that was just completed. Prioritize skills that logically follow from the work done.
