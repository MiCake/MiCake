# MiCake Framework AI Agent System

AI-powered development assistants for MiCake framework contributors.

## Purpose

This system provides specialized AI agents to help MiCake framework maintainers and contributors work more efficiently on framework development tasks.

## Agents

| Agent | Role | Use Case |
|-------|------|----------|
| architect | System Architect | New modules, subsystems, architecture decisions |
| developer | Framework Developer | Feature implementation, bug fixes, code quality |
| tester | Test Engineer | Unit tests, integration tests, coverage analysis |
| documenter | Documentation Writer | API docs, user manuals, code comments |
| reviewer | Code Reviewer | PR reviews, quality checks, best practices |

## Quick Start

1. Select an agent in your AI assistant
2. Use the `help` command to see available actions
3. Follow the workflow for your task

## Directory Structure

```
.micake/
├── agents/                    # Agent definitions
│   ├── architect.agent.md
│   ├── developer.agent.md
│   ├── tester.agent.md
│   ├── documenter.agent.md
│   └── reviewer.agent.md
│
├── knowledge/                 # Framework knowledge base
│   ├── architecture.md
│   ├── module-system.md
│   ├── ddd-patterns.md
│   ├── coding-standards.md
│   └── testing-guidelines.md
│
├── workflows/                 # Task workflows
│   ├── new-module.workflow.md
│   ├── feature-implementation.workflow.md
│   └── bug-fix.workflow.md
│
└── platforms/                 # Platform-specific configurations
    ├── github-copilot/
    └── vscode/
```

## Workflows

### Adding a New Module
1. architect: Design module structure
2. developer: Implement module code
3. tester: Create test suite
4. documenter: Write documentation
5. reviewer: Final quality check

### Feature Implementation
1. architect: Review design if needed
2. developer: Implement feature
3. tester: Add tests
4. reviewer: Code review

### Bug Fix
1. developer: Analyze and fix
2. tester: Verify fix and add regression tests
3. reviewer: Code review

## Configuration

No configuration required. Agents use framework knowledge base directly.

## References

- [MiCake Repository](https://github.com/MiCake/MiCake)
- [Development Principles](../principles/development_principle.md)
