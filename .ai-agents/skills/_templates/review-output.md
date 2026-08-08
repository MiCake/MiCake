---
id: 'review-output'
version: '1.0'
skill: 'mvt-review'
---

# Code Review Report

<!--
  This template defines the structure of review.md.
  Each section below includes a guidance comment explaining what to write.
  Remove these HTML comments in the final artifact.
-->

## Review Scope
<!--
  The file list reviewed, the review depth (full / per-module / aspect
  filter), and any fallbacks applied (e.g., "design.md missing -> Group A
  skipped"). This establishes what was and was not covered.
-->

## Summary
<!--
  Counts per severity (Critical / Warning / Suggestion) plus a one-paragraph
  overall verdict: Approve / Approve with comments / Request changes / Block.
  Verdict rule: Critical > 0 -> Request changes; Critical = 0 & Warnings > 5
  -> Approve with comments; Critical = 0 & Warnings <= 5 & Suggestions only
  -> Approve. Code-only review (design.md missing) caps at Approve with comments.
-->

## Critical Issues
<!--
  One entry per Critical finding. Each finding: file, line range, severity,
  observation, recommendation. Critical = bug, security flaw, broken
  contract, data loss risk, or layer violation that breaks isolation.
  Merge duplicate findings (same root cause, multiple files) into one entry.
-->

## Warnings
<!--
  One entry per Warning finding. Warning = likely problem or significant
  quality issue that is not a bug today but is high-risk or a maintainability
  hazard (e.g., function 200 lines, duplicated logic 3x, missing tests for a
  documented business rule). Same finding format as Critical Issues.
-->

## Suggestions
<!--
  One entry per Suggestion finding. Suggestion = improvement, polish, or
  taste preference (e.g., clearer variable name, split a small helper,
  minor docstring gap). Same finding format as Critical Issues.
-->

## Skipped Checks
<!--
  Review groups skipped because their inputs were missing (e.g., Group A
  skipped when design.md is absent, Group E skipped when no test files in
  scope). Each entry: the group name + the reason it was skipped.
  If no groups were skipped, write "None".
-->

## Recommended Next Skill
<!--
  One-line recommendation based on findings: `/mvt-fix` for Critical
  issues, `/mvt-test` if Group E (tests) gaps, `/mvt-refactor` if Group B
  (quality) is dominant, `/mvt-implement` if review is clean and work
  remains. Tailor to the actual finding distribution.
-->

## Highlights
<!--
  Positive observations worth noting: well-structured code, good test
  coverage, clean abstractions, or anything done particularly well.
  Balances the review and reinforces good practices. Optional — omit if
  nothing stands out.
-->
