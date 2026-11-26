---
id: 'decompose-output'
version: '1.0'
skill: 'mvt-decompose'
---

# Epic Decomposition: {Epic Title}

<!--
  This template defines the structure of epic.md (the narrative companion
  to epic.yaml). Each section below includes a guidance comment explaining
  what to write. Replace {Epic Title} with the actual epic name.
  Remove these HTML comments in the final artifact.
-->

## Vision
<!--
  One-sentence summary of the overall epic goal. This is the single
  cohesive outcome the decomposition aims to deliver.
-->

## Scope & Out of Scope
<!--
  What the epic delivers (in scope) vs. what it explicitly excludes
  (out of scope). Two short lists or a two-column table. Boundaries here
  prevent scope creep into the child stories.
-->

## Requirement Sources
<!--
  Derived provenance view from epic.yaml requirement_context.sources.
  Use a Markdown table with: Source ID | Kind | Reference | Fingerprint.
  Show "Not applicable" for conversation fingerprints. Do not copy source
  file contents into this narrative artifact.
-->

## Requirement Baseline
<!--
  Summarize the normalized context items that define the epic. Group concise
  statements by category and retain each context item ID for traceability.
  Identify globally inherited items explicitly. epic.yaml remains authoritative.
-->

## Cross-cutting Concerns
<!--
  Themes spanning multiple children: auth, logging, error handling, data
  migration, shared infrastructure, etc. Each concern: which children it
  affects. These are not standalone children — they are shared obligations.
-->

## Child Stories
<!--
  Markdown table mirroring epic.yaml children[]. Columns:
  | # | Child | Scope | Context Coverage | Status | Depends On |
  One row per child story. Status is `active` for the first child and
  `pending` for the rest. Context Coverage lists context_refs in stored order.
  Depends On lists change_ids (empty for roots).
-->

## Dependency Map
<!--
  Mermaid flowchart showing child dependencies (the DAG). Each node is a
  child change_id; edges point from dependency to dependent. This must
  match the depends_on relationships in epic.yaml and contain no cycles.
-->

## Open Questions
<!--
  Ambiguities or decisions deferred during decomposition. Each item: the
  question + which child it affects (if any). If none, write "None".
-->
