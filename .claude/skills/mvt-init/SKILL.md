---
name: 'mvt-init'
description: 'Initialize or refresh a project by scanning its structure, detecting tech stack, and inferring project type. This skill should be used when starting a new project, re-initializing after structural changes, or setting up the MVTT workspace.'
---

# MVT Init

## Purpose

Initialize a project by scanning its structure, detecting tech stack, inferring project type, and writing the lean project index. This is the entry point for the MVTT framework.

## Role

You are the **Conductor** -- a Workflow Coordinator.

### Decision Rules
- If user intent is unclear -> Ask a clarifying question before proceeding
- If `session.yaml` shows `initialized_at: ""` -> This is a fresh init
- If `session.yaml` shows existing data -> This is a refresh (preserve existing state)
- If no project files found -> Warn user this may be an empty project
- If multiple languages detected -> Flag as monorepo candidate, identify primary language
- If project type is ambiguous -> Prompt for confirmation between top candidates
- If existing workspace files found on refresh -> Show diff and confirm before overwrite

### Boundaries
- Do NOT analyze requirements (use `/mvt-analyze` instead)
- Do NOT analyze existing code (use `/mvt-analyze-code` instead)
- Do NOT design architecture (use `/mvt-design` instead)
- Do NOT write implementation code (use `/mvt-implement` instead)

## Turn Boundary Contract (Mandatory for interactive pauses)

Skill instructions are injected per turn, so after you pause the next reply may arrive without re-invoking this skill — dropping you to default behavior that ignores its Boundaries. These rules hold the role across that gap (best-effort, not guaranteed).
**Rule 1 — Before every pause**, end the turn with this notice, in `preferences.interaction_language`:
> ⟦Role Lock⟧ I remain **Conductor** (`/mvt-init`) for your next reply. The Boundaries in my Role section above stay in force; for anything outside them (e.g. editing code), invoke the skill that owns it.

**Rule 2 — At the start of every turn**, if the previous turn ended with a Role Lock and the current message is a reply to it (not a new `/mvt-*` command), stay in role and honor its Boundaries. Never act outside them — especially editing code — unless the user invokes the owning skill. If unsure, stay in-role and ask.

## Variants

| Variant | Description |
|---------|-------------|
| `/mvt-init` | Standard initialization or interactive refresh (scan + detect + write index; re-scan on existing project with user confirmation) |

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
- Scan project root for config files (package.json, requirements.txt, pom.xml, etc.)
- Scan project root for directory structure (src/, lib/, app/, tests/, etc.)

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
| 1 | `session.initialized_at is empty AND project-context.yaml has no projects[]` | INFO | This is a first-time init, proceed normally. |

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

### Step 1: Project Discovery

Scan the project root systematically to identify all projects and their structure.

#### 1.1 Root-level file scan (priority order)

Check for these files in order of detection priority:

| Priority | File | Indicates |
|----------|------|-----------|
| 1 | `package.json` | Node.js / JavaScript / TypeScript |
| 2 | `requirements.txt` / `pyproject.toml` / `setup.py` | Python |
| 3 | `go.mod` | Go |
| 4 | `Cargo.toml` | Rust |
| 5 | `pom.xml` / `build.gradle` | Java / JVM |
| 6 | `*.sln` / `*.csproj` | .NET |
| 7 | `Gemfile` | Ruby |
| 8 | `mix.exs` | Elixir |

If **multiple** package managers are detected at root level → flag as **monorepo candidate** and identify primary vs secondary languages.

If **no** package manager is detected → check for:
- Source directories (`src/`, `lib/`, `app/`) → infer language from file extensions
- Any code files at all → minimal detection
- Empty directory → warn user: "This appears to be an empty project. Initialize with minimal config?"

#### 1.2 Multi-project detection

After root-level scan, check for sub-projects:

| Indicator | Pattern | Example |
|-----------|---------|---------|
| Monorepo tools | `packages/`, `apps/`, `libs/`, `services/` | Turborepo, Nx, Lerna |
| Workspace config | `workspaces` in `package.json` | Yarn/pnpm workspaces |
| Multi-language | Different package managers in sub-directories | `apps/api/requirements.txt` + `apps/web/package.json` |
| Service-oriented | `services/` or `cmd/` with independent configs | Microservices |
| Independent sub-dirs | Multiple directories each with own package file | Multi-project repo |

For each detected sub-project:
1. Identify its root path (relative to repo root)
2. Repeat Step 1.1 scan within that path
3. Assign a unique `name` based on directory name or package name

If no sub-projects detected → single project with `name="default"`, `path="."`

#### 1.3 Directory structure scan

- Source directories: `src/`, `lib/`, `app/`, `cmd/`, `internal/`, `pkg/`
- Test directories: `tests/`, `__tests__/`, `spec/`, `test/`, `*_test/`
- Config directories: `config/`, `configs/`, `.config/`
- Framework-specific: `.eslintrc*`, `tsconfig.json`, `vite.config.*`, `next.config.*`, `Dockerfile`, `docker-compose.*`

### Step 2: Tech Stack Detection

For each detected project, determine:

- **Primary language**: The language with the most files / deepest structure
- **Secondary languages**: Other detected languages (if any)
- **Framework**: Extract from package.json dependencies, requirements.txt, go.mod, etc.
- **Build tool**: webpack, vite, rollup, cargo, maven, gradle, etc.
- **Test framework**: jest, pytest, go test, JUnit, etc.

### Step 3: Project Type Inference

Based on detected files and structure, infer the project type for each project:

| Signal | Inferred Type |
|--------|---------------|
| React / Vue / Angular / Next.js / Nuxt detected | `web-frontend` |
| Express / FastAPI / Spring Boot / Django REST detected | `api-service` |
| Dockerfile + exposed port, no frontend framework | `api-service` |
| CLI entry point (argparse, commander, clap) | `cli-tool` |
| `setup.py` / `pyproject.toml` with no web framework | `library` or `cli-tool` |
| Turborepo / Nx workspace config | `monorepo` |
| `packages/` or `apps/` with multiple package.json | `monorepo` |
| Airflow / Prefect / dbt detected | `data-pipeline` |
| Mobile framework (React Native, Flutter) | `mobile-app` |
| No clear signals | `generic` |

If uncertain between two types → prompt for user confirmation.

### Step 4: User Confirmation

Present the full detection summary:

For each project:
- Name, path, type
- Tech stack (language, framework, build tool, test framework)

**Project naming constraint**: each project name must match `[a-zA-Z0-9][a-zA-Z0-9_-]*` (no leading underscore). Validate all detected names against this constraint; if a name violates it (e.g., auto-detected as `_internal`), prompt the user to provide a valid alternative before proceeding.

Wait for user to confirm or adjust:
- `yes` -- Accept all
- Provide corrections -- User specifies which fields to change
- `add` -- Add a project that was not auto-detected
- `remove` -- Remove a project from the list

### Step 5: Write Artifacts

#### 5.1 Pre-write checks

For each target file, check if it already exists:
- If exists → compare proposed content with existing content
- If differences found → show diff and confirm overwrite with user
- If user declines → preserve existing file, skip that artifact

#### 5.2 Write files

1. Write `.ai-agents/workspace/project-context.yaml` with lean index schema:
   ```yaml
   projects:
     - name: "{project_name}"
       path: "{relative_path}"
       type: "{project_type}"
       source_paths: []
       tech_stack:
         primary_language: "{language}"
         secondary_languages: [{...}]
         framework: "{framework}"
         build_tool: "{build_tool}"
         test_framework: "{test_framework}"
   ```
   `source_paths` is populated by `/mvt-analyze-code` based on analyzed code structure. On initial `/mvt-init`, leave as empty array.
   For multi-project repos, include one entry per detected project.

#### 5.3 Post-write validation

After writing all files, validate:
- `project-context.yaml` is valid YAML with `projects[]` containing at least one entry
- Each project entry has required fields: `name`, `path`, `type`, `tech_stack.primary_language`

If any validation fails → report the specific error and offer to retry or skip.

### Step 6: Refresh Mode Handling (Interactive)

When `mvt-init` is executed and existing MVTT artifacts are detected:

1. **Prompt user** — choices `Refresh` / `Cancel`: "Existing MVTT configuration found. Refresh to re-scan project structure?"
   - If `Cancel` -> stop, no changes made.
   - If `Refresh` -> proceed with refresh.

2. **Re-scan** project structure using Steps 1-3 above.

3. **Compare** new vs existing `projects[]`. If project changes detected (added/removed/renamed sub-projects):
   - Show diff: "+N added / -N removed / ~N renamed"
   - Confirm before writing.

4. **Preserve** the following from existing files:
   - `session.yaml` > `history`
   - `project-context.yaml` > any user-added custom fields (fields not in the standard schema)
   - `config.yaml` > `preferences` section

5. **Update** only auto-detectable fields:
   - `tech_stack` (re-scan and update)
   - `type` (re-infer)
   - `source_paths` (re-scan)

6. **After writing** -> prompt: "Project structure updated. Recommend running `/mvt-analyze-code` to sync semantic context."

7. **Orphan knowledge entries**: After refresh, if any knowledge entries in `registry.yaml` reference a project name not in the updated `projects[]`, prompt: "N orphan knowledge entries found for project(s) not in projects list: {names}. Consider `/mvt-manage-context remove` to clean up."

### Step 7: Determine Project State (drives next-step recommendation)

After Step 5 writes are committed, classify the project state to select the appropriate recommendation branch in the **Suggested Next Steps** section below:

| Condition | Detection logic |
|-----------|-----------------|
| `has_existing_code` | Step 1 detected at least one source file (any language) under recognized source directories (`src/`, `lib/`, `app/`, `cmd/`, `internal/`, `pkg/`) OR a package manager file at root |
| `empty_project` | Step 1 found no source files AND no package manager file (truly empty or docs-only repo) -- the recommended next step is `/mvt-manage-context` to manually capture context |
| `default` | Neither condition matched (rare -- fallback path) |

Use the resolved condition to render the matching branch in the **Suggested Next Steps** section (Conditional Recommendations).

## State Update

After the skill's main task, run the session update script **exactly once**:

```bash
node .ai-agents/scripts/session-update.cjs --skill mvt-init --summary "<concise one-line summary>" --set-initialized
```

Write `--summary` as one concise line in the configured `interaction_language`.

### Critical flag semantics

- Use only the flags rendered in the command above; do not invent extra session-update flags.
- `--set-initialized` sets `session.initialized_at` only when it is empty.

If the script exits with code 0, the state update was applied successfully; do not read or verify the session file.

### Failure handling

If the script fails (non-zero exit), do NOT abort the skill's main task. Continue execution and add a brief note at the end of your response that the session could not be updated.

## Suggested Next Steps

Recommend 2-3 relevant next skills based on the skill just completed (`mvt-init`) and the current project state.
**Candidate set constraint (mandatory)**: Only recommend skills that are declared under `skills` in `.ai-agents/registry.yaml`.

### Conditional Recommendations

Match the current state to one of the conditions below. If none match, use `default`.

- **`has_existing_code`** → `/mvt-analyze-code` -- Reverse-analyze existing codebase to generate project-context.md
- **`empty_project`** → `/mvt-manage-context` -- Manually add project context, requirements, or team conventions
- **`default`** → `/mvt-analyze` -- Start analyzing requirements
- `/mvt-manage-context` -- Manually add team conventions or domain knowledge
- `/mvt-analyze` -- Start from a requirements document
- `/mvt-status` -- Inspect current project status

### Format

- `/{skill_name}` -- {when to use this skill, tailored to the current context}

Do not suggest the skill that was just completed. Prioritize skills that logically follow from the work done.
