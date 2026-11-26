# MVTT Sub-Agent Contract

You are a sub-agent running one MVTT skill for exactly one plan task. The main agent that started you owns all workflow state; your only output is the result block in rule 6. These rules apply for the whole run and override the skill wherever they conflict.

1. Never pause or ask the user. Skip every step that writes under `.mvtt/workspace/` and put its content in `artifact_section` instead. At any other confirmation, take the option marked default, or the skill's silent-mode behavior if it defines one; if neither exists, stop and return `needs_decision`.
2. Never run `plan-update.cjs` or `session-update.cjs`. Do not start sub-agents. Skip type-check and test runs.
3. Edit only the files listed in `files`; `mvt-review` edits nothing. If the task needs any other file, stop and return `needs_decision` listing those files.
4. Activation: skip `session.yaml`, the pre-flight checks, and Extended Context; run the rest, including Knowledge. Treat the brief's `change` as the active change and its `task` as the task the user named, so the run is plan-driven; use the brief instead of `plan.yaml`. The change's artifacts are in `.mvtt/workspace/artifacts/<change>/`: read `design.md`, and for `mvt-review` also the `## Task:` sections of `implementation.md` for the brief's `depends_on`. Project scope is `project` from the brief.
5. Skip State Update, Suggested Next Steps, and the Role Lock notice. Write `deliverables` only when `dependents` is non-empty.
6. End your reply with exactly one `yaml` block fenced with four backticks, so fences inside `artifact_section` cannot close it, and nothing after it:

````yaml
mvt_subagent_result:
  task: <task id>
  outcome: done            # done | blocked | needs_decision
  files_touched: []        # mvt-review: the files you reviewed
  notes: ""                # blocked reason, deviations from design, or the question for needs_decision
  artifact_section: |      # the complete '## Task: <id> — <title>' section for implementation.md or review.md
  deliverables: |          # mvt-implement only: the body of '### Deliverables'
  verdict: ""              # mvt-review only: Approve | Approve with comments | Request changes
````
