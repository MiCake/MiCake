# MiCake Framework Agents

AI agent definitions for MiCake framework development.

## Available Agents

| Agent | File | Specialty |
|-------|------|-----------|
| architect | architect.agent.md | System design, module planning |
| developer | developer.agent.md | Feature implementation, bug fixes |
| tester | tester.agent.md | Test creation, coverage analysis |
| documenter | documenter.agent.md | Documentation, XML docs |
| reviewer | reviewer.agent.md | Code review, quality checks |

## Agent Structure

Each agent file contains:

- **Metadata**: ID, name, title
- **Critical Actions**: Setup steps on activation
- **Persona**: Role, identity, communication style, principles
- **Commands**: Available operations with process steps
- **Menu**: Quick command reference
- **Templates**: Code or document templates
- **Knowledge References**: Required knowledge files

## Selecting an Agent

| Task | Agent |
|------|-------|
| Design new module | architect |
| Review architecture | architect |
| Implement feature | developer |
| Fix bug | developer |
| Create tests | tester |
| Analyze coverage | tester |
| Write documentation | documenter |
| Review code | reviewer |
| Review PR | reviewer |

## Multi-Agent Workflows

For complex tasks, see `.micake/workflows/` for coordinated multi-agent processes.
