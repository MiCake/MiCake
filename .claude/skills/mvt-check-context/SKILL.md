---
name: 'mvt-check-context'
description: 'Analyze context token load and provide optimization recommendations. This skill should be used when user wants to check how much context MVTT loads, identify large files, or optimize workspace size for better performance.'
---

# MVT Context Check

## Purpose

Analyze the total context information that MVTT loads at runtime, estimate token consumption, assess health status, and provide actionable optimization recommendations.

## Role

You are the **Conductor** -- a Workflow Coordinator.

### Decision Rules
- Total tokens <= 12,000 -> Report as "Healthy"
- Total tokens 12,001-25,000 -> Report as "Borderline", suggest optimizations
- Total tokens > 25,000 -> Report as "Oversized", strongly recommend cleanup

### Boundaries
- Do NOT modify any files (Only analyze and recommend)
- Do NOT clean up artifacts (use `/mvt-cleanup` instead)
- Do NOT modify context (use `/mvt-manage-context` instead)

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
- .ai-agents/config.yaml -- Framework configuration (read thresholds and preferences; do not count config itself as context payload)

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

## Execution Flow

### Step 1: Load Inputs
- **Recommended**:
  - `.ai-agents/knowledge/core/manifest.yaml` -- to filter `core/_framework/` (excluded) from `core/user/` (in-scope).
- **Fallback**: missing `core/manifest.yaml` -> treat all `core/*` files as user-origin (over-counts; flag in report).

### Step 2: Determine In-Scope Files
This skill measures only files the **user** can reduce or relocate. Framework-fixed overhead is excluded.

**In scope (user-actionable):**
- Index: `.ai-agents/workspace/project-context.yaml`.
- Semantic context: `.ai-agents/knowledge/project/_generated/project-context.md` (always the flat path, regardless of project count).
- Shared knowledge: every entry in `registry.yaml > knowledge._all` and `knowledge.{projectName}` (map-aware -- traverse ALL project keys in the knowledge map). For the `core` entry, scan only files marked as user-origin per `core/manifest.yaml` (or whose path begins with `user/`); skip files under `core/_framework/`.
- Per-skill knowledge: every entry in `registry.yaml > skills.*.knowledge._all` and `skills.*.knowledge.{projectName}` (map-aware -- traverse ALL project keys for each skill), grouped by skill.
- Artifacts: all files under `.ai-agents/workspace/artifacts/` recursively. **Exclude the `_archived/` subdirectory** — it contains completed changes archived by `/mvt-cleanup` and should not count toward the active workspace token budget.

**Out of scope (do NOT scan):**
- `.claude/skills/mvt-*/SKILL.md` -- framework-shipped, not user-editable.
- `.ai-agents/knowledge/core/_framework/**` -- framework-shipped.
- `.ai-agents/config.yaml`, `.ai-agents/workspace/session.yaml`, `.ai-agents/registry.yaml` -- small, required, and addressed via `/mvt-config` or `/mvt-manage-context`, not here.

### Step 3: Estimate Token Consumption
- **What**: produce a per-file tokens estimate and per-category subtotals, with **per-project breakdown**.
- **How**:
  1. For each in-scope file: compute characters mechanically and estimate tokens as `ceil(characters / 4)`.
  2. Group by category: `Index`, `Semantic Context`, `Shared Knowledge`, `Per-Skill Knowledge`, `Artifacts`.
  3. For Shared Knowledge, compute total once -- this is per-skill overhead (loaded by every skill invocation).
  4. For Per-Skill Knowledge, compute totals per skill so users can see which skill is heaviest.
  5. Identify the Top 5 largest single files across the whole in-scope set.
  6. **Per-project breakdown**: for multi-project workspaces, also compute token costs per project:
     - `knowledge._all` = shared across all projects
     - `knowledge.{projectName}` = project-specific overhead
     - `skills.*.knowledge.{projectName}` = per-skill per-project overhead
     Display as a separate table: `project | knowledge tokens | per-skill tokens | total`.
  7. **Global summary**: total tokens across all projects + `_all` overhead loaded every time.

### Step 4: Apply Thresholds and Health Status
- **What**: assign each file/category a status of `healthy | borderline | oversized`.
- **How**: read thresholds from `.ai-agents/config.yaml > preferences.context_thresholds.*` if present; otherwise use the defaults below.

  | Subject | healthy | borderline | oversized |
  |---------|---------|------------|-----------|
  | Total in-scope tokens | <= 12000 | 12001-25000 | > 25000 |
  | Single file tokens | <= 3000 | 3001-6000 | > 6000 |
  | `project-context.md` tokens | <= 4000 | 4001-8000 | > 8000 |
  | Shared Knowledge total tokens | <= 6000 | 6001-12000 | > 12000 |
  | Single change-id artifacts directory tokens | <= 3000 | 3001-8000 | > 8000 |

- Overall workspace status = the worst status across all subjects above.

### Step 5: Generate Recommendations
- **What**: produce a list of specific, actionable recommendations. Each entry is `(trigger, message, suggested skill)`.
- **How**: walk the table; emit a recommendation for every row whose trigger fires.

  | Trigger | Message template | Suggested skill |
  |---------|------------------|-----------------|
  | `project-context.md` is `oversized` | "project-context.md is {N} tokens. Regenerate with leaner sections." | `/mvt-analyze-code` |
  | `project-context.md` is `borderline` AND last `/mvt-analyze-code` ran > 30 days ago | "project-context.md is {N} tokens and may be stale. Consider regenerating." | `/mvt-analyze-code` |
  | Total artifacts tokens > artifacts threshold OR > 3 completed changes still in `artifacts/` (excluding `_archived/`) | "Workspace has {N} tokens of historical artifacts. Archive completed changes." | `/mvt-cleanup` |
  | A specific change-id directory is `oversized` | "artifacts/{id} alone is {N} tokens. Summarize this change." | `/mvt-cleanup` |
  | Shared Knowledge total is `oversized` | "Shared knowledge totals {N} tokens (loaded by every skill). Move skill-specific entries to per-skill." | `/mvt-manage-context move` |
  | A single Shared Knowledge file is `oversized` | "{path} is {N} tokens. Split or move to per-skill." | `/mvt-manage-context move` |
  | Per-skill Knowledge entry exists in `registry.yaml` but its referenced files are missing | "{skill} declares knowledge `{id}` but `{path}` is missing." | `/mvt-manage-context remove` (or restore the file) |
  | A knowledge file exists on disk but no `registry.yaml` entry references it | "{path} is unused (not loaded by any skill)." | `/mvt-manage-context remove` |
  | Two knowledge entries reference identical content (same SHA-256 hash computed from file bytes) | "{a} and {b} are duplicates. Consolidate." | manual edit |

- **Constraints on recommendations**:
  - Never recommend changes to framework files (`_framework/`, `mvt-*/SKILL.md`).
  - Never recommend deletion without an `/mvt-manage-context` or `/mvt-cleanup` command -- those skills own the actual mutation.
  - If no triggers fire, return a single line: "Workspace context is healthy ({N} tokens total)."

### Step 6: Generate Report
- Render the report in this order:
  1. **Summary** -- one line: total tokens + overall status.
  2. **Per-Category Breakdown** -- table: `category | files | tokens | status`.
  3. **Top 5 Largest Files** -- table: `path | tokens | category | status`.
  4. **Per-Skill Knowledge Cost** -- table: `skill | tokens` (sorted desc); include shared knowledge as a separate row labeled `(shared, loaded every time)`.
  5. **Per-Project Token Accounting** -- table: `project | knowledge tokens | per-skill tokens | total` (only for multi-project workspaces; for single-project, omit this section).
  6. **Recommendations** -- numbered list from Step 5; if empty, render the healthy line.
  7. **Excluded Scope Note** -- one paragraph reminding the user that framework files (`_framework/`, `mvt-*/SKILL.md`, `config.yaml`, `session.yaml`, `registry.yaml`) were not measured here.
- The report is conversation output; this skill does NOT write any artifact.

## Edge Cases & Errors

| Case | Handling |
|------|----------|
| `registry.yaml` references a knowledge id whose source path is empty / missing | Include in Step 5 recommendations; do NOT count missing files toward token totals |
| `core/manifest.yaml` cannot be parsed | Treat the whole `core/` tree as in-scope (over-counts); add a note in the report |
| Workspace has zero artifacts | Skip the artifacts category in Step 6; do not error |
| Workspace exceeds the artifacts threshold AND the user just ran `/mvt-cleanup` (within last hour per `history`) | Surface but downgrade to a one-line note ("recently cleaned -- remaining {N} tokens are likely active work") |
| User passes a path argument | This skill ignores arguments; print a one-line note and run as normal (do not narrow scope to a single file -- that is `/mvt-status` territory) |
| Token estimate disagrees with model's actual consumption | This is expected; the `chars/4` heuristic is an approximation. State this caveat in the Summary line |
| Two skills declare the same knowledge id | Count the file once for storage but report it under both skills in the Per-Skill table; flag the duplication in Step 5 |

## State Update

This skill is read-only and does NOT modify `.ai-agents/workspace/session.yaml`.

## Suggested Next Steps

Recommend 2-3 relevant next skills based on the skill just completed (`mvt-check-context`) and the current project state.
**Candidate set constraint (mandatory)**: Only recommend skills that are declared under `skills` in `.ai-agents/registry.yaml`.

### Conditional Recommendations

Match the current state to one of the conditions below. If none match, use `default`.

- **`context oversized or borderline`** → `/mvt-cleanup` -- Archive old artifacts to reduce context
  - Or `/mvt-manage-context` -- Move per-skill knowledge to reduce shared load
- **`context healthy`** → `/mvt-status` -- Check overall project status

### Format

- `/{skill_name}` -- {when to use this skill, tailored to the current context}

Do not suggest the skill that was just completed. Prioritize skills that logically follow from the work done.
