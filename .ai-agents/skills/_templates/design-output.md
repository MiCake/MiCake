---
id: 'design-output'
version: '1.0'
skill: 'mvt-design'
---

# Architecture Design: {Feature Name}

<!--
  This template defines the structure of design.md.
  Each section below includes a guidance comment explaining what to write.
  Replace {Feature Name} with the actual feature/change name.
  Remove these HTML comments in the final artifact.
-->

## Overview
<!--
  The problem statement: what is being designed and why. Summarise the
  requirement context (from analysis.md or conversation) and the design
  goal in one short paragraph.
-->

## Architecture Decision Records
<!--
  Every ADR from the design process. Each ADR: context, decision, status
  (proposed/accepted/superseded), and consequences. Never zero ADRs — if
  none were needed, produce minimal one-line ADRs. In --plan mode, ADRs
  collapse to one-line `decision: <text>`.
-->

## Module Design
<!--
  Table of modules from the design: | Module | Path | Responsibility |
  Dependencies |. Each module's boundary, what it owns, and what it
  depends on. This drives the File Structure and implementation ordering.
-->

## Key Interfaces
<!--
  Explicit signatures and endpoints: function signatures, class contracts,
  HTTP endpoints (method + path), event contracts. These are the public
  contracts that /mvt-implement must match exactly.
-->

## Data Flow
<!--
  Sequences describing how data moves through the system, including error
  paths. Use prose or mermaid sequence diagrams. Cover the main flow and
  each declared error path.
-->

## File Structure
<!--
  Mapping of modules to file/directory paths in this repo. Show the
  intended file layout (create/modify/delete) so /mvt-implement and
  /mvt-plan-dev can locate targets without re-deriving paths.
-->

## Implementation Guidelines
<!--
  Ordering hints for /mvt-implement and /mvt-plan-dev: which modules to
  build first, shared dependencies to establish early, and any sequencing
  constraints. Not a task list — just guidance.
-->

## Change Tracking
<!--
  List of files expected to be created/modified/deleted by this design.
  This is the footprint estimate; /mvt-implement records the actual
  footprint. If > ~5 files or > 1 new module, /mvt-plan-dev is recommended.
-->
