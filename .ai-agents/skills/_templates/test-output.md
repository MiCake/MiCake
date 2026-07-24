---
id: 'test-output'
version: '1.0'
skill: 'mvt-test'
---

# Test Design: {Feature Name}

<!--
  This template defines the structure of test-design.md.
  Each section below includes a guidance comment explaining what to write.
  Replace {Feature Name} with the actual feature/change name.
  Remove these HTML comments in the final artifact.
-->

## Scope
<!--
  The target files under test and any fallbacks applied (e.g., resolved
  from implementation.md Files Touched, or git diff, or user argument).
  Establishes what this test design covers.
-->

## Test Framework & Layout
<!--
  The chosen test framework (inferred from project manifests or confirmed
  with user) and the file layout convention used (mirror under tests/,
  sibling *.test.ts, etc.). One short paragraph or a small table.
-->

## Test Scenarios
<!--
  The Scenario Table from the scenario-identification step. Columns:
  | ID | Scenario | Type (happy/edge/negative/security/performance) | Rule-traced-to |.
  Covers happy paths, boundaries, invalid inputs, business rules from
  project-context.md, and error paths declared in design.md data flow.
-->

## Test Cases
<!--
  The row-level test case table from the design step. Columns:
  | ID | Scenario | Granularity | Preconditions | Inputs | Actions | Expected | Rule-traced-to |.
  Each row maps to one concrete, runnable test. One scenario maps to one
  granularity (unit / integration / E2E).
-->

## Test Code
<!--
  The generated test files: file paths and a brief description of what
  each file covers. The actual test source goes into the project tree;
  this section is a record of what was written and where. Include any
  setup/fixture notes that explain non-obvious test infrastructure.
-->

## Granularity Decisions
<!--
  Summary of how scenarios were assigned to unit / integration / E2E,
  including any "needs setup" gaps (e.g., integration scenarios flagged
  because the project lacks an integration test setup). One short
  paragraph or a small table.
-->

## Coverage Analysis
<!--
  Only when the --coverage flag is set; otherwise omit this heading
  entirely. Map test cases back to target files/functions and business
  rules from project-context.md; list gaps (untested functions, untested
  rules, untested error paths) and recommend additional test cases for
  each gap. Do not auto-generate gap tests unless the user confirms.
-->

## Implementation Issues Found
<!--
  Bugs discovered during scenario design or test writing (failing test
  reveals a real bug, not a test bug). Each finding: scenario id, expected
  vs observed, severity (Critical / Warning), and a recommendation to run
  /mvt-fix. Do NOT modify production code from this skill. If none found,
  write "None".
-->

## Suggested Run Commands
<!--
  One or two commands the user can copy-paste to execute the generated
  tests (e.g., `npx vitest run path/to/test.test.ts`). Keep it minimal
  and project-appropriate.
-->
