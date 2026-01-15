# MiCake Framework Knowledge Base

Reference documentation for AI agents working on MiCake framework.

## Contents

| File | Description |
|------|-------------|
| architecture.md | Four-layer architecture, layer responsibilities |
| module-system.md | Module lifecycle, dependencies, patterns |
| ddd-patterns.md | Entity, Aggregate, ValueObject, Repository patterns |
| coding-standards.md | Naming, DI, async, dispose, validation patterns |
| testing-guidelines.md | AAA pattern, naming, mocking, coverage |

## Usage

Agents should load relevant knowledge files before performing tasks:

| Agent | Primary Knowledge |
|-------|-------------------|
| architect | architecture.md, module-system.md |
| developer | coding-standards.md, ddd-patterns.md |
| tester | testing-guidelines.md |
| documenter | All files |
| reviewer | All files |

## Source of Truth

This knowledge base is derived from:
- `principles/development_principle.md` - Development principles
- `.github/copilot-instructions.md` - AI coding instructions
- Existing framework code patterns

When conflicts exist, development_principle.md takes precedence.

## Updates

When framework patterns change:
1. Update relevant knowledge files
2. Ensure consistency across files
3. Test agent behavior with changes
