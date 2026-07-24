---
name: 'mvt-create-skill'
description: 'Create custom MVTT skills through interactive guided workflow. This skill should be used when user wants to create a new skill, extend the framework with custom functionality, or build project-specific automation.'
---

# MVT Create Skill

## Purpose

Guide users through designing and creating custom MVTT-compliant skills. Follows a structured process: understand use cases, plan reusable resources, design the skill, generate files, and validate. Generates properly structured SKILL.md files with optional bundled resources (scripts, references, assets), manifest.yaml, and registry entries -- ensuring compatibility with the MVTT framework.

## Role

You are the **Conductor** -- a Workflow Coordinator.

### Decision Rules
- User provides skill name -> Validate and proceed with design
- Name conflicts with existing skill -> Warn and ask for alternative
- Skill needs output template -> Create template in `_templates/` and update manifest
- Skill needs state updates -> Include session.yaml update rules
- Skill needs bundled resources -> Plan scripts/references/assets in Step 3
- Usage patterns unclear -> Ask for concrete examples before proceeding

### Boundaries
- Do NOT generate skills that deviate from MVTT SKILL.md standard structure (follow the standard structure as documented)
- Do NOT use arbitrary prefixes without validating naming conventions (use `mvt-` or a project-specific prefix like `app-`, `proj-`)
- Do NOT create skills without registering in registry.yaml (register with `custom: true` in registry.yaml)
- Do NOT write vague or first-person descriptions (write third-person with effective trigger keywords)
- Do NOT exceed ~5k words in SKILL.md body (push detailed content to references/)

## Turn Boundary Contract (Mandatory for interactive pauses)

Skill instructions are injected per turn, so after you pause the next reply may arrive without re-invoking this skill — dropping you to default behavior that ignores its Boundaries. These rules hold the role across that gap (best-effort, not guaranteed).
**Rule 1 — Before every pause**, end the turn with this notice, in `preferences.interaction_language`:
> ⟦Role Lock⟧ I remain **Conductor** (`/mvt-create-skill`) for your next reply. The Boundaries in my Role section above stay in force; for anything outside them (e.g. editing code), invoke the skill that owns it.

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
- Load one existing SKILL.md as structural reference (e.g., `.claude/skills/mvt-status/SKILL.md`)

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

## Output Format Constraint (Mandatory)

Persisted markdown output MUST follow these rendering rules. Scope: artifact files, generated reports, plans, design documents, and any markdown written to disk. Chat output is out of scope.

**Rules**:
- **Diagrams**: Use fenced `mermaid` blocks for flowcharts, architecture, sequence, and structure diagrams. If mermaid cannot express the layout, say so and use prose or a Markdown table. Never use ASCII art.
- **Tables**: Use Markdown tables (`| col | col |`), not aligned spaces or tabs.
- **Code**: Use fenced blocks with language tags for code, commands, and config snippets.
- **Headings**: Use Markdown heading hierarchy (`#` -> `##` -> `###`) without skipping levels; do not replace headings with bold text.

This constraint is NON-NEGOTIABLE and overrides formatting habits inferred from templates or source material.

## Design Principles

### Progressive Disclosure
Skills use a three-level loading system to manage context efficiently:
1. **Metadata (name + description)** -- Always in context (~100 words). Determines when Claude triggers the skill.
2. **SKILL.md body** -- Loaded when skill triggers. Target <5k words. Keep only essential procedural instructions and workflow guidance.
3. **Bundled resources** -- Loaded on demand by Claude. Unlimited size.

**Avoid duplication**: information should live in either SKILL.md, `references/`, or knowledge entries -- not in multiple places.

### Writing Style
Write the entire skill using **imperative/infinitive form** (verb-first instructions), not second person. Use objective, instructional language ("To accomplish X, do Y" rather than "You should do X"). This maintains consistency and clarity for AI consumption.

### Description Quality
The `name` and `description` in YAML frontmatter determine when Claude will use the skill. Guidelines:
- Be specific about what the skill does and when to use it.
- Use third-person ("This skill should be used when...").
- Include: what it does + when to trigger + how it differs from similar skills.

| Good | Bad |
|------|-----|
| "Create custom MVTT skills through interactive guided workflow. Use when user wants to create a new skill, extend the framework with custom functionality, or build project-specific automation." | "Skill creator" |
| "Analyze requirements documents and extract domain concepts. Use when user provides requirements text or asks to understand project scope." | "Analyze stuff" |

## Execution Flow

### Step 1: Load Inputs
- **Recommended**:
  - One existing skill's SKILL.md under `.claude/skills/<existing>/SKILL.md` as a structural reference (to extract shared section patterns like Activation Protocol, State Update, Next Steps).
  - `.ai-agents/registry.yaml` -- to check for name collisions and understand skill categories.

### Step 2: Understand Usage with Concrete Examples
Skip only when usage patterns are already crystal clear.

Ask up to 3 of the following (do not ask all at once):
- "Can you give 1-2 concrete examples of how this skill would be used?"
- "What would a user say or do that should trigger this skill?"
- "Are there edge cases or variations in how this skill gets invoked?"
- "What does success look like? What does the AI produce / write?"

Conclude this step with a short paragraph stating the skill's purpose in one sentence and listing 1-3 representative invocations.

### Step 3: Gather Requirements
Collect core metadata. Each field has an explicit constraint -- do not accept vague answers.

| Field | Constraint | Notes |
|-------|------------|-------|
| Name | Lowercase, kebab-case, no spaces, must match `^[a-z][a-z0-9-]*$`. Prefix `mvt-` for framework skills; project-specific prefixes (e.g., `app-`, `proj-`) are also acceptable | Reject if invalid or if it conflicts with an existing entry in `registry.yaml` |
| Agent role | One of: `conductor`, `analyst`, `architect`, `developer`, `reviewer`, `tester` | Maps the skill to an existing role family |
| Purpose | One sentence | Will become the SKILL.md `## Purpose` section |
| Category | One of: `workflow`, `shortcut`, `project`, `utility` | Drives how `/mvt-help` groups it |
| Description | Third-person, includes what + when + how it differs | Will become the frontmatter `description` |
| Variants (optional) | List of flag/sub-mode entries | Becomes the Variants table |

If the user is unsure on any field, propose a default and ask for confirmation rather than leaving it blank.

### Step 4: Plan Reusable Contents
- **What**: decide which resources (beyond the SKILL.md body) the new skill needs.
- **How**: for each example from Step 2, ask: "If we executed this from scratch, what reusable resource would have helped?" Map each answer to one of the categories below.

  | Resource | Directory | Use when | Example |
  |----------|-----------|----------|---------|
  | Scripts | `scripts/` | Same code rewritten repeatedly OR deterministic reliability needed | `scripts/validate_schema.py` |
  | References | `references/` | Documentation Claude should read while working (schemas, API docs, policies) | `references/api_spec.md` |
  | Assets | `assets/` | Files used in the output, not in context (templates, icons, fonts) | `assets/report_template.md` |
  | Knowledge | (declared in registry) | Loaded via Activation Protocol; share across skills or manage via `/mvt-manage-context` | `knowledge/principle/coding-standards/` |
  | Output template | `_templates/` | Persisted document that needs a stable structure | `_templates/{name}-output.md` |

- **Reuse vs new**: before declaring a new shared resource, check existing skills' SKILL.md files and knowledge entries -- prefer reusing patterns that already exist.
- **Output of this step**: a checklist `(name | purpose | path)` shown to user.

### Step 5: Design the Skill
- **What**: produce a one-page outline before generating any file.
- **How**: load an existing skill's SKILL.md (e.g., `.claude/skills/mvt-fix/SKILL.md`) as a structural reference, then fill in:

  | Aspect | Decision |
  |--------|----------|
  | Input parameters | What does the skill need from the user / workspace? |
  | Execution mode | Interactive / automated / hybrid |
  | Pre-flight checks | List, with severity (BLOCK / WARN); these populate the Pre-flight part of the Activation Protocol copied into the generated SKILL.md |
  | Decision rules (in role-header) | 3-7 imperative rules covering the major branches |
  | Boundaries | What is in-scope vs delegated to other skills |
  | Execution Flow steps | Bulleted titles only (full content comes in Step 6) |
  | Output | What gets written to disk (artifact path + template) OR pure conversation output |

### Step 6: Generate Skill Files
1. Create skill directory: `.claude/skills/{name}/`.
2. Generate a complete `SKILL.md` file (see Generated SKILL.md Structure below). This file must be fully self-contained — there is no assembler or build step to resolve shared section references. All content must be inlined directly into the SKILL.md.
3. For standard sections (Activation Protocol, Language Constraint, Output Format Constraint, State Update, Next Steps), read the existing shared section files under `.ai-agents/sections/` or the corresponding installed skill section and substitute only the skill-specific values (role, decision rules, boundaries, pre-flight checks, next-skill suggestions). Do NOT reproduce these sections from memory; use the checked-in source text as the canonical template.
4. For skill-specific sections (frontmatter, Purpose, Execution Flow, Edge Cases & Errors), generate fresh content following the skeleton below.
   - `## Execution Flow`
   - `### Step 1: Load Inputs` -- list required and recommended files, plus fallback rules.
   - Skill-specific main steps (1-5 of them), each with **What / How / Branches** sub-structure when there is real branching.
   - `### Step N: User Confirmation` -- only when destructive or non-obvious; describe trigger conditions.
   - `### Step N+1: Write Artifacts` -- only when the skill persists files; specify path, template, required content.
   - Final session update step.
   - `## Edge Cases & Errors` table with at least 3 rows.
5. If an output template was decided in Step 4, create `.ai-agents/skills/_templates/{name}-output.md` with **headings only** (this is a document structure, not a conversation reply template). If a custom version directory exists at `_templates/custom/`, note that users can override there.
6. If scripts / references / assets are needed, create them under the skill directory.
7. SKILL.md word budget: aim for the body to be under ~5k words. Push reference material to `references/`.

### Step 7: Register in Registry (MANDATORY)
Create a pre-write backup of `.ai-agents/registry.yaml`, then add the skill entry to `.ai-agents/registry.yaml` > `skills` section using structured YAML serialization (not hand-written string concatenation):

```yaml
  {name}:
    description: "{third-person description with trigger keywords}"
    custom: true
    template: "_templates/{name}-output.md"   # include ONLY if an output template was created in Step 6; omit this key otherwise
```

- If an output template was created in Step 6, set `template:` to its path so `/mvt-template` can discover it; otherwise omit the key entirely.
- The `custom: true` field is **required** for user-created skills; without it, framework updates will overwrite the entry.
- Refuse to overwrite an existing skill key.
- Escape `description` through the YAML serializer; never interpolate raw user text into YAML.
- Validate the YAML still parses after the write; if not, restore the backup and surface the parse error.
- Post-write, assert the skills entry count increased by exactly 1 and no existing sibling skill entry changed.

### Step 8: Validation
Walk this checklist; any failed item must be fixed before declaring success.

| Check | Pass criterion |
|-------|----------------|
| Frontmatter present | `name` and `description` exist in SKILL.md YAML frontmatter |
| Description quality | Third-person, includes what + when, distinguishes from neighbors |
| Writing style | Imperative/infinitive throughout; no "you" / "your" |
| Naming uniqueness | No collision with another entry in `registry.yaml` |
| `custom: true` | Set in registry entry |
| Standard sections present | SKILL.md contains Role, Activation Protocol, Execution Flow, Edge Cases & Errors, State Update, Suggested Next Steps |
| Knowledge files exist | Every file referenced in `knowledge:` resolves on disk |
| Template path correct | If `template:` set, file exists at that path; the template is headings-only |
| Word budget | SKILL.md body under ~5k words (use any available word-count method, e.g., editor statistics) |
| Standard skeleton | Execution Flow contains Load Inputs, main steps with branches, Edge Cases & Errors |

Show the user how to invoke: `/{name}`.

### Step 9: Iteration Guidance
Tell the user the iteration loop:
1. Use `/{name}` on real tasks.
2. Notice struggles or inefficiencies.
3. Decide whether to update SKILL.md, add a `references/` file, add a knowledge entry, or split into a new skill.
4. Re-run `/mvt-create-skill` to refine, or edit the source files directly and rebuild.

### Step 10: State Update
Apply the State Update rules defined in the **State Update** section below.

## Edge Cases & Errors

| Case | Handling |
|------|----------|
| Skill name collides with an existing registry entry | STOP at Step 3; ask user to rename; do not generate any file |
| User wants the skill to mutate `session.yaml` fields beyond `history` | Surface that ownership rules forbid this (e.g., `changes` is owned by `/mvt-plan-dev`/`/mvt-update-plan`); recommend redesign |
| Output template is requested but the skill is conversation-only (no persisted file) | Refuse to create a template; explain that templates are for document structure, not conversation replies |
| User asks to skip the registry registration step | Refuse; an unregistered skill is invisible to `/mvt-help`, `/mvt-status`, and `/mvt-resume`. Registration is non-negotiable |
| Skill duplicates an existing skill's responsibility | Surface the overlap (cite the existing skill's description); propose merging or sub-classing as a variant rather than creating a duplicate |
| User provides a non-third-person description ("Use this skill when you need...") | Rewrite to third-person before saving; show the rewrite for confirmation |
| Generated SKILL.md is missing a standard section (e.g., State Update, Next Steps) | Abort generation; inform user which section is missing; read an existing SKILL.md for the correct structure |
| `registry.yaml` parse fails after write | Restore from the pre-write backup; surface the error; do not leave the registry corrupt |

## Generated SKILL.md Structure

The generated SKILL.md consists of two parts: **skill-specific sections** (generated fresh) and **standard sections** (copied from this document with skill-specific values replaced).

### Skill-specific sections (generate fresh)

```markdown
---
name: '{name}'
description: '{third-person description with trigger keywords}'
---

# {Title}

## Purpose

{concise purpose statement}

## Role

You are the **{Agent Role}** -- {role description}.

### Decision Rules
{generated rules, one per line, verb-first}

### Boundaries
- Do NOT {scope} (use `/{skill}` instead)
{repeat for each boundary}

## Execution Flow

### Step 1: Load Inputs
{required and recommended inputs, plus fallback rules}

{skill-specific steps 2-N}

### Step N: User Confirmation
{only when destructive or non-obvious; describe trigger conditions}

### Step N+1: Write Artifacts
{only when the skill persists files; specify path, template, required content}
{if shortcut/conversation-only: "No artifact -- results are conversation-only."}

{final session update step if not shortcut, or shortcut operation rules}

## Edge Cases & Errors

| Case | Handling |
|------|----------|
{at least 3 rows}
```

### Standard sections (copy from this document)

Copy the following sections verbatim from this document (the assembled SKILL.md you are currently reading), replacing only the skill-specific values indicated:

| Section | Source in this document | What to replace |
|---------|----------------------|-----------------|
| Activation Protocol | `## Activation Protocol` (the whole Load + Resolve block) | Copy the entire block as-is. The block already covers context loading, project scope, knowledge, config preferences, and pre-flight. Adjust only two skill-specific parts: under Load, the Extended Context entries (add the files/directives this skill needs, or drop the Extended Context bullet if none); under Resolve > Pre-flight, the checks table (skill-specific checks, or a single INFO row if none required) |
| Language Constraint | `## Language Constraint` | Copy as-is (separate section, not part of Activation Protocol) |
| Output Format Constraint | `## Output Format Constraint` | Copy as-is (separate section); include only if the skill writes persisted markdown |
| State Update | `## State Update` | Replace `/{name}` with the new skill's command; include `active_change` conditional block only if the skill creates changes; include `Shortcut Operation Rules` if the user opted for shortcut semantics during Step 5 design |
| Suggested Next Steps | `## Suggested Next Steps` | Replace `current_skill` with the new skill name; replace conditional suggestions with skill-appropriate ones |

**Important**: Do NOT paraphrase or rewrite the standard sections. Load them from the checked-in shared section sources or a selected installed exemplar and only substitute the skill-specific values. This ensures consistency across all MVTT skills.

## Output Format

No external template -- output is the generated skill files plus a creation summary:

```markdown
## Custom Skill Created

- **Skill**: `/{name}`
- **Agent**: {agent role}
- **Location**: `.claude/skills/{name}/SKILL.md`
- **Registry**: `registry.yaml` (custom: true)
- **Template**: {created/none}
- **Resources**: {scripts/references/assets listing, or none}
- **Knowledge**: {per-skill entries / shared only}
- **Context Budget**: SKILL.md body ~{N} words

Use `/{name}` to invoke the new skill.

### Iteration
1. Use `/{name}` on real tasks
2. Notice struggles or inefficiencies
3. Update SKILL.md or bundled resources as needed
4. Use `/mvt-create-skill` to refine, or edit skill files directly
```

## State Update

After the skill's main task, run the session update script **exactly once**:

```bash
node .ai-agents/scripts/session-update.cjs --skill mvt-create-skill --summary "<concise one-line summary>"
```

Write `--summary` as one concise line in the configured `interaction_language`.

### Critical flag semantics

- Use only the flags rendered in the command above; do not invent extra session-update flags.

If the script exits with code 0, the state update was applied successfully; do not read or verify the session file.

### Failure handling

If the script fails (non-zero exit), do NOT abort the skill's main task. Continue execution and add a brief note at the end of your response that the session could not be updated.

## Suggested Next Steps

Recommend 2-3 relevant next skills based on the skill just completed (`mvt-create-skill`) and the current project state.
**Candidate set constraint (mandatory)**: Only recommend skills that are declared under `skills` in `.ai-agents/registry.yaml`.

### Conditional Recommendations

Match the current state to one of the conditions below. If none match, use `default`.

- **`skill created successfully`** → `/mvt-help` -- Verify the new skill appears in the catalog
- **`skill needs knowledge entries`** → `/mvt-manage-context` -- Add knowledge files for the new skill
- **`skill needs testing with real tasks`** → `/mvt-status` -- Check project state before testing the skill

### Format

- `/{skill_name}` -- {when to use this skill, tailored to the current context}

Do not suggest the skill that was just completed. Prioritize skills that logically follow from the work done.
