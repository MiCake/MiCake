# Task Orchestration

Coordinates agents for common development tasks.

## Task Routing

| Task Description | Primary Agent | Workflow |
|-----------------|---------------|----------|
| "Add new module" | architect | new-module.workflow.md |
| "Design subsystem" | architect | - |
| "Implement feature" | developer | feature-implementation.workflow.md |
| "Fix bug" | developer | bug-fix.workflow.md |
| "Add tests" | tester | - |
| "Write documentation" | documenter | - |
| "Review code" | reviewer | code-review.workflow.md |
| "Review PR" | reviewer | code-review.workflow.md |

## Quick Reference

### Single Agent Tasks

```
Task                    Agent       Command
─────────────────────────────────────────────
Design module           architect   design-module
Review architecture     architect   review-architecture
Implement feature       developer   implement-feature
Fix bug                 developer   fix-bug
Create class            developer   create-class
Write tests             tester      create-unit-tests
Analyze coverage        tester      analyze-coverage
Write XML docs          documenter  write-xml-docs
Write README            documenter  write-readme
Review code             reviewer    review-code
```

### Multi-Agent Workflows

```
Workflow                Sequence
─────────────────────────────────────────────
New Module              architect → developer → tester → documenter → reviewer
Feature Implementation  [architect] → developer → tester → reviewer
Bug Fix                 developer → tester → reviewer
Code Review             reviewer → [architect] → reviewer → [documenter]
```

## Handoff Protocol

When transferring between agents:

1. Current agent completes their task
2. Current agent outputs artifact
3. Next agent receives artifact as input
4. Next agent validates input
5. Next agent proceeds with task

## Error Handling

If agent cannot complete task:

1. Agent reports blocker
2. Determine if prerequisite missing
3. Route to correct agent
4. Resume after prerequisite complete

## Task Status

Track workflow progress:

```
[ ] Not started
[~] In progress
[✓] Completed
[!] Blocked
[x] Failed
```

## Usage Examples

### "I need to add a caching module"

Route to: `architect` → `new-module.workflow.md`

### "There's a bug in repository disposal"

Route to: `developer` → `bug-fix.workflow.md`

### "Please review my PR"

Route to: `reviewer` → `code-review.workflow.md`

### "I need to add tests for OrderService"

Route to: `tester` → Direct command `create-unit-tests`
