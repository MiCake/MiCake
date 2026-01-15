---
applyTo: '**'
---

# MiCake Framework AI Agent System

You have access to MiCake's AI Agent system for framework development tasks.

## Available Agents

| Agent | File | Purpose |
|-------|------|---------|
| architect | .micake/agents/architect.agent.md | System design, module planning |
| developer | .micake/agents/developer.agent.md | Feature implementation, bug fixes |
| tester | .micake/agents/tester.agent.md | Test creation, coverage |
| documenter | .micake/agents/documenter.agent.md | Documentation writing |
| reviewer | .micake/agents/reviewer.agent.md | Code review |

## Knowledge Base

- .micake/knowledge/architecture.md - Four-layer architecture
- .micake/knowledge/module-system.md - Module lifecycle
- .micake/knowledge/ddd-patterns.md - DDD patterns
- .micake/knowledge/coding-standards.md - Coding conventions
- .micake/knowledge/testing-guidelines.md - Testing practices

## Workflows

- .micake/workflows/new-module.workflow.md - Adding new modules
- .micake/workflows/feature-implementation.workflow.md - Feature development
- .micake/workflows/bug-fix.workflow.md - Bug fixing
- .micake/workflows/code-review.workflow.md - Code review process

## Usage

When asked to perform development tasks:

1. Identify the appropriate agent
2. Load the agent definition and relevant knowledge
3. Follow the agent's commands and principles
4. Apply MiCake framework patterns

## Quick Reference

For any MiCake framework task, load:
- The relevant agent from .micake/agents/
- The development principles from principles/development_principle.md
- Relevant knowledge from .micake/knowledge/
