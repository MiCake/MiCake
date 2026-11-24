# Fix Notes: t1 UoW Runtime — Review Findings C1-C5 / W1-W3

## Symptom

The independent `/mvt-review` of t1 (`uow-runtime-contracts`) found 5 Critical and 4 Warning issues. The follow-up `/mvt-test` run added 13 regression tests; 10 red tests reproduced every finding with exact failure modes. This record documents the fixes applied to make all 10 red tests green without regressing the existing 98 tests.

## Input Source

Review result from `/mvt-review` (held in conversation, no artifact persisted), pre-verified by the red-phase tests from `/mvt-test`. Per skill Step 1a, hypotheses/reproduction steps were skipped.

## Reproduction

Verified. All 10 red regression tests in `src/tests/MiCake.Tests/Uow/UnitOfWorkReviewRegressionTests.cs` reproduced the findings before the fix and pass after it.

## Root cause

1. `ActivatePendingResourcesAsync` short-circuited on a global `_transactionsStarted` flag, so resources registered after the first activation pass were flushed without ever activating a transaction (ADR-011 violation).
2. `ExecuteRequiresNewCoreAsync` constructed the inner `UnitOfWork` directly, bypassing the `BeginAsync` initialization pipeline (lifecycle hooks and Immediate activation) and the inner scope provider.
3. A single commit-state enum per resource was overwritten by a successful compensating rollback (`Failed` -> `RolledBack`), losing the identity of the commit-failed resource; a commit failure with zero committed resources was misreported as a partial commit.
4. After a partial commit, both execution boundaries called `RollbackAsync` again; with no eligible resources left, the UoW marked itself completed and raised `OnRolledBack`, reporting an overall rollback despite durable resources (ADR-007 violation).
5. `DisposeAsync` ignored rollback failures and swallowed resource-disposal failures, so `UnitOfWorkBoundaryException.CleanupExceptions` could never be populated.
6. Boundary cleanup rollback reused the already-cancelled operation token, so a token-honoring provider would cancel the rollback itself.
7. Savepoint creation recorded coverage only on full success; partially created savepoints became untracked and unusable.

## Patch summary

| File | Change |
|---|---|
| `src/framework/MiCake/DDD/Uow/UnitOfWorkCommitOutcome.cs` | `UnitOfWorkResourceOutcome` single `State` replaced by `CommitState` + `RollbackState?` so a commit-failed resource stays distinguishable from a never-committed one. Public contract change; no consumer exists yet (t2 is the first). |
| `src/framework/MiCake/DDD/Uow/Internal/UnitOfWork.cs` | Per-resource activation tracking (`_activatedResources`, global short-circuit removed); `HasActiveTransactions` reflects any real per-resource transaction; commit failure with zero committed resources rethrows the original commit exception via `ExceptionDispatchInfo`; `_hasPartialCommit` terminal state (rollback refused); `DisposeAsync` collects rollback + resource-disposal failures and throws `UnitOfWorkBoundaryException` with `RollbackExceptions`/`CleanupExceptions`; savepoint partial coverage with attempted-set distinction; separate commit/rollback state dictionaries. |
| `src/framework/MiCake/DDD/Uow/Internal/UnitOfWorkManager.cs` | `InitializeUnitOfWorkAsync` accepts the owning provider (root for `BeginAsync`, inner scope for `requiresNew`); boundary exception composition rewritten: partial commit propagates without a second rollback, cleanup rollback uses `CancellationToken.None`, dispose failures are collected and combined. |
| `src/framework/MiCake/DDD/Uow/Internal/StandaloneUnitOfWorkExecutor.cs` | Same exception-composition rewrite as the manager. |
| `src/tests/MiCake.Tests/Uow/UnitOfWorkTests.cs` | `CommitAsync_CommitAndRollbackFailure_ShouldExposeBoth` now asserts `UnitOfWorkBoundaryException` (zero-committed + rollback failure); partial-commit outcome asserts use `CommitState`/`RollbackState`. |
| `src/tests/MiCake.Tests/Uow/UnitOfWorkImmediateModeTests.cs` | Partial activation failure now asserts `HasActiveTransactions == true` (the old assertion encoded the W1 bug). |
| `src/tests/MiCake.Tests/Uow/UnitOfWorkReviewRegressionTests.cs` | T4 adapted to `CommitState`; all other red tests unchanged. |

## Regression risk

- `UnitOfWorkResourceOutcome` shape change: no consumer outside `MiCake` yet (verified by grep); low risk before t2.
- `HasActiveTransactions` semantic tightening: sole caller (`DisposeAsync` condition) is compatible.
- Boundary exception composition: guarded by 5 existing requiresNew/standalone tests plus the 10 regression tests.

## Verification

- UoW tests: 108/108 passed (98 existing + 10 regression + 3 new coverage tests).
- Full `MiCake.Tests`: 258/258 passed.
- `dotnet build src/framework/MiCake/MiCake.csproj`: 0 warnings, 0 errors.

## Follow-ups

- Synchronous `Dispose()` still swallows resource-disposal exceptions (log-only); consider aligning with `DisposeAsync` in a later slice.
- `design.md` Key Interfaces (outcome record shape) should be revised before t2 consumes the contract.
- `_hasPartialCommit` leaves frame restoration to the boundary `finally`; direct-callers must dispose the UoW (documented in the exception message).
