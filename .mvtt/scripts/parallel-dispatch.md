# MVTT Parallel Dispatch (main agent)

Follow this procedure when the Parallel Dispatch Gate of `/mvt-implement` or `/mvt-review` selects it. It replaces that skill's Execution Flow, State Update, and Suggested Next Steps for this run.

You own every write to `plan.yaml`, `session.yaml`, `implementation.md`, and `review.md`; sub-agents never write them. Run at most one batch per invocation. Below, `S` means `node .mvtt/scripts/plan-update.cjs --plan "<active_change.plan_path>" --projects "<project names from project-context.yaml>"`, and `<skill>` is the skill that opened the gate.

## 1. Offer the batch

Show the batch (id, title, files), the deferred tasks with their reasons, and the interrupted tasks. Interrupted tasks are never dispatched; tell the user to recover them with `/mvt-resume`.

Offer choices `Dispatch {n} in parallel` / `Run {batch[0].id} only` / `Cancel`:

- `Run {batch[0].id} only`: leave this procedure and run the skill's Execution Flow for that task, as if the user had named its id.
- `Cancel`: stop without writing anything.
- With `<skill>` = `mvt-implement` and `preferences.silent.implement: true`, dispatch without asking. Silent mode skips only this choice.

## 2. Claim

For each batch task, in order: `S --task <id> --status in_progress --dispatched set`. If a call fails, run `S --task <id> --dispatched clear` for each task already claimed in this step, report the error, and stop.

## 3. Delegate

Start one sub-agent per batch task in the same turn and wait for all of them before continuing. Give each this brief, filled from its batch entry and its task in `plan.yaml`:

```text
MVTT_SUBAGENT_MODE: v1
First read .mvtt/scripts/subagent-contract.md and follow it for this whole run.
Then run the skill <skill> for exactly one plan task (use your skill tool; if you have none, read <path of the SKILL.md you are running>).
change: <active_change.id>
task: <id> — <title>
project: [<batch project>]
files: [<batch files>]
depends_on: [<task depends_on>]
dependents: [<batch dependents>]
notes: <task notes>
acceptance:
- <each acceptance criterion>
```

## 4. Check results

- From each reply, take the last block fenced with four backticks (` ````yaml `) whose top-level key is `mvt_subagent_result`. A reply without one counts as "no result".
- If two results list the same file in `files_touched`, keep a warning that names those files.
- For `mvt-implement`, note every file in `files_touched` that is not in the task's brief `files`.
- For `mvt-implement`, run the project's type-checker once over all files in `files_touched`, and attribute each failure to the task that touched that file.

## 5. Propose and confirm

Propose one transition per task:

| Result | Proposal |
|--------|----------|
| implement result that touched files outside its brief `files` | `blocked`: those files |
| implement `done`, no type-check failure in its files | `done` |
| implement `done`, type-check failure in its files | `blocked`: the errors |
| `blocked` | `blocked`: its `notes` |
| `needs_decision` | `blocked`: the question |
| review `Approve` or `Approve with comments` | `done` |
| review `Request changes` | `blocked`: the verdict and main findings; recover with `/mvt-fix`, then `/mvt-update-plan <id> pending` |
| no result | no change; the task stays interrupted |

Show the proposals and any warnings, then offer `Apply all` / `One by one` / `Keep all in progress`. `One by one` asks `Apply` / `Keep in progress` for each task. This confirmation always runs, also in silent mode.

## 6. Apply, in plan order

For each task with an applied proposal:

1. Write its `artifact_section` into `implementation.md` (`mvt-implement`) or `review.md` (`mvt-review`), following the skill's Write Artifact rules for `## Task:` sections.
2. If `deliverables` is non-empty and the task has dependents, add it as `### Deliverables` inside that section, then run `S --task <id> --deliverables-pointer current --mark-deliverable-stale <dependents>`.
3. `done`: `S --task <id> --status done --artifacts "<files_touched, comma-separated>"`. For `mvt-review`, omit `--artifacts`.
4. `blocked`: `S --task <id> --status blocked --notes "<reason> | Previous notes: <original guidance>"`, where the original guidance is the task's notes after the last `Previous notes: ` (or all of them if there is none), so reasons never pile up. Keep the value on one line and escape double quotes.

For each task kept in progress: `S --task <id> --dispatched clear`, so it becomes a normal candidate again.

If a call fails, report its stderr, keep any section already written, and continue with the next task.

## 7. Finish

Run `node .mvtt/scripts/session-update.cjs --skill <skill> --summary "<one line covering the batch>"` exactly once. If it fails, add a note and do not retry.

Report each task's outcome, the deferred tasks ("run `/<skill>` again for the next batch"), and any interrupted tasks ("recover with `/mvt-resume`"). Suggest the `skill_hint` of tasks that became ready.
