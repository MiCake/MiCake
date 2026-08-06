---
id: '20260806-uow-transaction-reliability'
task: 't7-relational-acceptance'
source: 'plan.yaml'
status: 'tested'
---

# Test Design: UoW Transaction Reliability — t7 Relational Acceptance

## Scope

Target: prove the relational transaction guarantees of the MiCake Unit of Work on a
temporary file-backed SQLite store, replacing the skipped EF Core InMemory transaction
tests. All framework-mediated writes must be transaction-bound and roll back atomically;
no guarantee may rely solely on EF Core InMemory or remain skipped.

Primary target files (from `plan.yaml` t7 artifacts):

| File | Action |
|------|--------|
| `src/tests/MiCake.IntegrationTests/Uow/SqliteUnitOfWorkFixture.cs` | Fixture: file-backed SQLite provider(s), pooled provider, frame-stable context resolution |
| `src/tests/MiCake.IntegrationTests/Uow/UnitOfWorkCompositionMatrixTests.cs` | Root/shared-nested/requiresNew/read-only/savepoint composition |
| `src/tests/MiCake.IntegrationTests/Uow/UnitOfWorkWritePathTests.cs` | First-write binding, flush, bulk/raw SQL, lifecycle, host isolation, pooled reuse |
| `src/tests/MiCake.IntegrationTests/Uow/UnitOfWorkFailureOutcomeTests.cs` | Partial commit and rollback-failure boundary outcomes (ADR-007) |
| `src/tests/MiCake.IntegrationTests/Uow/UnitOfWorkLazyImmediateModeIntegrationTests.cs` | Rewritten from InMemory to SQLite; 5 previously skipped tests now run |
| `src/tests/MiCake.IntegrationTests/Uow/UnitOfWorkWithRepositoryTests.cs` | Skipped transaction test removed (superseded); no-write-pipeline semantics documented |
| `src/tests/MiCake.IntegrationTests/Uow/AuditIntegrationTests.cs` | Migrated: writes wrapped in an ambient writable UoW |
| `src/tests/MiCake.IntegrationTests/Uow/GenericAuditIntegrationTests.cs` | Migrated: writes wrapped in an ambient writable UoW |
| `src/tests/MiCake.IntegrationTests/Uow/OwnedEntityAuditIntegrationTests.cs` | Migrated: writes wrapped in an ambient writable UoW |
| `src/tests/MiCake.IntegrationTests/Repository/CommonFilterPagingQueryIntegrationTests.cs` | Migrated: full UoW pipeline wiring, seeding inside a UoW |

## Test Framework & Layout

xUnit (`Xunit` global using) in `MiCake.IntegrationTests`, mirroring the existing
`src/tests/MiCake.IntegrationTests/Uow` layout. Framework and layout inferred from
`project-context.yaml` (`test_framework: xunit`) and existing files. The
`MiCake.IntegrationTests` assembly has InternalsVisibleTo access to the framework
packages, so runtime internals (`AmbientUnitOfWorkAccessor`, `UnitOfWorkManager`,
`MiCakeInterceptorFactory`) are registered directly without reflection.

Fixture pattern: one temporary SQLite database file per provider instance
(`Path.GetTempPath()` + GUID), cleaned up in `Dispose`. `Pooling=False` for regular
providers; EF context pooling with SQLite connection pooling enabled for the pooled
provider (see `SqliteUnitOfWorkFixture.BuildPooledProvider`).

## Test Scenarios

| ID | Scenario | Type | Rule-traced-to |
|----|----------|------|----------------|
| S1 | First SaveChanges binds to the UoW transaction and rolls back | happy | ADR-002, t7 A1 |
| S2 | ExecuteUpdate / ExecuteDelete / ExecuteSqlRaw roll back with the UoW | happy | ADR-002, t7 A1 |
| S3 | Write without a UoW is rejected before SQL execution | negative | ADR-002, t7 A4 |
| S4 | FlushAsync materializes the generated key without committing; rollback discards it | happy | ADR-001, t7 A1 |
| S5 | Detached stale aggregate replacement raises `DbUpdateConcurrencyException` | negative | ADR-001 (concurrency token), t7 A4 |
| S6 | Pre-save lifecycle failure marks the UoW rollback-only and persists nothing | negative | ADR-004, t7 A4 |
| S7 | Post-save re-entry converges in follow-up cycles and commits atomically | edge | ADR-005, t7 A4 |
| S8 | Two hosts in one process never share frames or state | edge | ADR-003 (host-local), t7 A4 |
| S9 | Tracked update rolled back by the UoW keeps the original state | happy | ADR-011, t7 A4 (supersedes skipped `Transaction_Rollback_ShouldDiscardChanges`) |
| S10 | Pooled DbContext commits and rolls back atomically across pool reuses | happy | ADR-004 (pool reset), t7 A4 |
| S11 | Root commit persists across scopes; root rollback leaves no trace | happy | ADR-011, t7 A2 |
| S12 | Shared nested inner commit does not physically commit; write invisible before root commit | edge | ADR-003, t7 A2 |
| S13 | Shared nested inner rollback marks the root rollback-only; root commit rejected | negative | ADR-003, t7 A2 |
| S14 | requiresNew inner commit persists independently; outer pending state untouched; ambient restored | happy | ADR-003, t7 A2 |
| S15 | requiresNew inner failure rolls back inner, leaves outer pending, restores ambient | negative | ADR-003, t7 A2 |
| S16 | requiresNew without an outer UoW throws | negative | ADR-003 |
| S17 | Read-only UoW rejects FlushAsync and rejects writes; nothing persists | negative | ADR-006, t7 A4 |
| S18 | Savepoint with all resources active before creation rolls back work across all resources | happy | t7 A3 |
| S19 | Savepoint rejects a resource registered after creation | negative | t7 A3 |
| S20 | Savepoint without an active transaction throws | negative | t7 A3 |
| S21 | Immediate mode activates transactions on Begin; Lazy defers until first write | happy | ADR-011, t7 A4 |
| S22 | Partial commit: first resource durable, failing resource Failed+RolledBack, structured outcome, UoW terminal | negative | ADR-007, t7 A4 |
| S23 | Rollback failure raises `UnitOfWorkBoundaryException` carrying every rollback failure | negative | ADR-007, t7 A4 |
| S24 | Lazy mode commit / rollback / nested / savepoint work on SQLite (supersedes skipped tests) | happy | t7 A1-A4 |
| S25 | Immediate mode commit / rollback / nested / savepoint work on SQLite (supersedes skipped tests) | happy | t7 A1-A4 |
| S26 | Audit timestamps are set inside the UoW contract (write guard compliance) | happy | ADR-002 guard |
| S27 | Owned-entity change updates the owner UpdatedAt | happy | audit + t4 snapshot contract |

## Test Cases

| ID | Scenario | Granularity | Preconditions | Inputs | Actions | Expected | Rule-traced-to |
|----|----------|-------------|---------------|--------|---------|----------|----------------|
| T1 | S1 | integration | SQLite created | aggregate Add | SaveChangesAsync then RollbackAsync | row count 0 | ADR-002 |
| T2 | S2 (update) | integration | seeded alpha/beta | ExecuteUpdateAsync rename | RollbackAsync | alpha restored | ADR-002 |
| T3 | S2 (delete) | integration | seeded alpha/beta | ExecuteDeleteAsync alpha | RollbackAsync | 2 rows remain | ADR-002 |
| T4 | S2 (raw SQL) | integration | seeded alpha/beta | ExecuteSqlRawAsync delete | RollbackAsync | 2 rows remain | ADR-002 |
| T5 | S3 | integration | SQLite created | Add + SaveChangesAsync without UoW | assert InvalidOperationException | rejected, 0 rows | ADR-002 |
| T6 | S4 | integration | SQLite created | Add + FlushAsync | assert Id > 0 then RollbackAsync | 0 rows | ADR-001 |
| T7 | S5 | integration | seeded original | load detached, bulk-update Name+Version in another UoW, UpdateAsync stale, FlushAsync | assert DbUpdateConcurrencyException | conflict, DB name = concurrent | ADR-001 |
| T8 | S6 | integration | ThrowingPreSaveHandler registered | Add + SaveChangesAsync | assert InvalidOperationException, CommitAsync rejected, RollbackAsync | 0 rows | ADR-004 |
| T9 | S7 | integration | PostSaveAddsOnceHandler registered | Add root + SaveChangesAsync | CommitAsync | 2 rows atomically | ADR-005 |
| T10 | S8 | integration | two fixtures/providers | host A commit, host B rollback | assert A=1, B=0 | no interference | ADR-003 |
| T11 | S9 | integration | seeded "Rollback Test" | tracked ChangeName + SaveChangesAsync + RollbackAsync | name unchanged | ADR-011 |
| T12 | S10 | integration | pooled provider created | 3 scopes: commit, rollback, commit | assert names = pooled-commit + pooled-second-commit | pool reset isolation | ADR-004 |
| T13 | S11 | integration | SQLite created | root commit / root rollback | 1 row / 0 rows | ADR-011 |
| T14 | S12 | integration | SQLite created | nested add + nested commit, count mid-flight, root commit | 0 before root, 1 after | ADR-003 |
| T15 | S13 | integration | SQLite created | nested rollback, root commit | InvalidOperationException "rollback-only", 0 rows | ADR-003 |
| T16 | S14 | integration | SQLite created | outer add, ExecuteRequiresNewAsync inner add | inner visible, outer pending, ambient = outer, then outer commit | 2 rows | ADR-003 |
| T17 | S15 | integration | SQLite created | outer add, inner write + SaveChangesAsync + throw | exception propagates, 0 rows, ambient = outer, outer commit = 1 row | ADR-003 |
| T18 | S16 | integration | no outer UoW | ExecuteRequiresNewAsync | InvalidOperationException | ADR-003 |
| T19 | S17 | integration | ReadOnly UoW | FlushAsync, then Add + SaveChangesAsync | both rejected, 0 rows | ADR-006 |
| T20 | S18 | integration | primary + secondary registered | activate both, flush, savepoint, write both after, rollback to savepoint, commit | only pre-savepoint rows on both stores | t7 A3 |
| T21 | S19 | integration | primary + secondary registered | savepoint after primary flush, register secondary, write secondary, RollbackToSavepointAsync | InvalidOperationException "does not cover resource" | t7 A3 |
| T22 | S20 | integration | SQLite created, no writes | CreateSavepointAsync | InvalidOperationException "no active transaction" | t7 A3 |
| T23 | S21 | integration | SQLite created | BeginAsync(Immediate) / BeginAsync() | HasActiveTransactions true / false then true after first write | ADR-011 |
| T24 | S22 | integration + fake resource | failing second resource registered | relational Add + CommitAsync | PartialUnitOfWorkCommitException with structured outcome; durable row remains; RollbackAsync rejected | ADR-007 |
| T25 | S23 | integration + fake resource | rollback-failing resource registered | relational Add + RollbackAsync | UnitOfWorkBoundaryException; relational store rolled back | ADR-007 |
| T26-S30 | S24 | integration | SQLite host | lazy commit / rollback / nested / savepoint | expected per-mode outcomes | t7 A1-A4 |
| T31-S35 | S25 | integration | SQLite host | immediate commit / rollback / nested / savepoint | expected per-mode outcomes | t7 A1-A4 |
| T36 | S26 | integration | audit modules + UoW | wrapped audit writes | timestamps set | ADR-002 |
| T37 | S27 | integration | owned entities + UoW | owned replacement + SaveChangesAsync | owner UpdatedAt set (FAILS: see issue I-1) | audit + t4 |

## Test Code

| File | Contents |
|------|----------|
| `SqliteUnitOfWorkFixture.cs` | `BuildProvider` (primary/secondary, `Pooling=False`), `BuildPooledProvider` (EF context pooling, SQLite pooling on), `GetPrimaryContext`/`GetSecondaryContext` via `IEFCoreContextFactory<T>`, `UowAcceptanceAggregate` (int DB-generated identity + concurrency token `Version`), `UowAcceptanceDbContext`/`UowSecondaryDbContext` |
| `UnitOfWorkCompositionMatrixTests.cs` | T13-T23: root/commit/rollback, shared nested, requiresNew commit/rollback/ambient restoration, read-only, savepoint coverage + late registration, Immediate/Lazy activation |
| `UnitOfWorkWritePathTests.cs` | T1-T12: first-write binding, bulk/raw SQL, no-UoW rejection, generated-key flush, detached concurrency, lifecycle failure, post-save re-entry, host isolation, tracked-update rollback, pooled reuse |
| `UnitOfWorkFailureOutcomeTests.cs` | T24-T25: partial commit and rollback failure via a hand-written `IUnitOfWorkResource` double with injectable failures (provider-level commit/rollback failures cannot be forced deterministically on a live SQLite connection) |
| `UnitOfWorkLazyImmediateModeIntegrationTests.cs` | T26-T35: rewritten from InMemory to a file-backed SQLite host with the full pipeline; all 5 previously skipped tests now execute |
| `UnitOfWorkWithRepositoryTests.cs` | Removed the skipped transaction test (superseded by T11); documented that the file exercises aggregate/domain-event mechanics without the write pipeline |
| `AuditIntegrationTests` / `GenericAuditIntegrationTests` / `OwnedEntityAuditIntegrationTests` | Writes wrapped in `RunInUowAsync` (ambient writable UoW) to comply with the write guard |
| `CommonFilterPagingQueryIntegrationTests.cs` | Full pipeline wiring (`AmbientUnitOfWorkAccessor`, `UnitOfWorkManager`, `UseMiCakeInterceptors(sp)`), seeding inside a UoW |

Fixture notes: `UowAcceptanceDbContext` and the other MiCake-derived test contexts
override `OnConfiguring` WITHOUT calling the base implementation while the fallback
write-guard interceptor defect (issue I-2) is unfixed; the provider-bound interceptors
are wired explicitly through `UseMiCakeInterceptors(sp)`.

## Granularity Decisions

All scenarios are **integration** tests: every case crosses the relational store
boundary (SQLite file, transactions, savepoints) and exercises module collaboration
(UoW runtime + EF Core write pipeline + repository + audit). No unit-level scenarios
were needed for t7 because the provider-neutral UoW runtime and save-operation
state machine are already covered by `MiCake.Tests` and `MiCake.EntityFrameworkCore.Tests`
from tasks t1-t6. T24/T25 mix a fake `IUnitOfWorkResource` with the real relational
resource to force failure paths that no live provider can trigger deterministically.

## Implementation Issues Found

### I-1 (Critical): Owned-entity changes do not update the owner UpdatedAt

- **Scenario**: S27 (T37)
- **Expected**: after replacing an owned value object (`ChangeAuthor`), the owner's
  `UpdatedAt` is set by the audit pre-save handler.
- **Observed**: `UpdatedAt` stays null. Diagnostic evidence: owner EF state =
  `Unchanged`, owned entries = `[Added, Deleted]`, and
  `SaveOperationEntityHelper.ResolvePreSaveState(ownerEntry)` returns `Unchanged`.
- **Root cause**: `SaveOperationEntityHelper.HasOwnedEntityChanges` compares the
  found owner with `ReferenceEquals(FindOwnerEntry(...), entry)`. `EntityEntry` is a
  **struct** in EF Core; `ReferenceEquals` boxes the arguments, so the comparison is
  always `false`. An unchanged owner whose owned entity changed is therefore never
  resolved as `Modified`, the audit handler never receives `Modified`, and `UpdatedAt`
  is never set.
- **Impact**: all OwnsOne/OwnsMany audit-timestamp updates are silently broken
  (3 failing tests: `SaveChanges_WhenOwnedEntityChanged_ShouldUpdateOwnerUpdatedAt`,
  `SaveChanges_WhenOneOfMultipleOwnedEntitiesChanged_ShouldUpdateOwnerUpdatedAt`,
  `SaveChanges_WithDateTimeOffset_WhenOwnedEntityChanged_ShouldUpdateOwnerUpdatedAt`).
- **Recommendation**: run `/mvt-fix`; replace the `ReferenceEquals` comparison with an
  equality comparison that respects the struct semantics of `EntityEntry`
  (`EntityEntry` implements `IEquatable<EntityEntry>`).

### I-2 (Critical): Pooled DbContext writes deadlock with `database is locked`

- **Scenario**: S10 (T12)
- **Expected**: a UoW over a pooled DbContext commits and rolls back across pool reuses.
- **Observed**: the first write commit hangs for the SQLite busy timeout (30 s) and
  fails with `SQLite Error 5: 'database is locked'`.
- **Root cause**: under `AddDbContextPool`, the `sp` captured by
  `UseMiCakeInterceptors(sp)` in the `AddDbContextPool((sp, opt) => ...)` delegate is
  EF's internal pool provider, not the request scope. The save-operation interceptor
  therefore hands the wrong provider to `EFCoreWriteCoordinator`, whose
  `ResolveWrapper` resolves a SECOND pooled context instance from that provider and
  registers it as a second UoW resource. Two EF resources then each activate a
  SQLite write transaction (BEGIN IMMEDIATE) against one file, and the second BEGIN
  deadlocks. Evidence: with a scope-resolved context holding a transaction, a
  root-provider-resolved second instance fails at its first `BeginTransactionAsync`;
  the identical experiment succeeds when the interceptor's provider matches the scope
  provider (non-pooled `AddDbContext`).
- **Impact**: every pooled DbContext write path deadlocks; `SaveOperationStateAccessor`
  pool-reset and frame-isolation guarantees cannot be exercised until fixed.
- **Recommendation**: run `/mvt-fix`; resolve the coordinator's `IEFCoreContextFactory`
  from the DbContext's own service provider (e.g. `context.GetInfrastructure()
  .GetService(...)` or `context.GetService<...>()`) instead of the interceptor's
  captured provider.

### I-3 (Warning): `MiCakeDbContext.OnConfiguring` installs a conflicting fallback interceptor

- **Scenario**: all writes through `MiCakeDbContext` subclasses with
  `UseMiCakeInterceptors(sp)`.
- **Observed**: every write fails with "Write operation ... cannot be guarded because
  the MiCake write pipeline is not registered" until the test contexts skip the base
  `OnConfiguring` call.
- **Root cause**: `MiCakeDbContext.OnConfiguring` unconditionally calls
  `UseMiCakeInterceptors()` (provider-less), which installs a second
  `MiCakeEFCoreInterceptor`/`MiCakeDbCommandInterceptor` pair that cannot resolve the
  coordinator. This conflicts with the DI-first wiring recommended by the
  implementation. The MiCake-derived test contexts work around it by overriding
  `OnConfiguring` without the base call; the sample `BaseAppDbContext` inherits the
  defect and every sample write would fail.
- **Recommendation**: run `/mvt-fix`; install the fallback interceptors only when the
  options are not already configured with provider-bound interceptors (e.g.
  `if (!optionsBuilder.IsConfigured)`-style guard, or skip when a
  `MiCakeSaveOperationOptionsExtension` with a provider is already present).

No other implementation issues were found. All remaining t7 scenarios pass.

## Suggested Run Commands

```bash
dotnet test src/tests/MiCake.IntegrationTests/MiCake.IntegrationTests.csproj
```

Focused run after `/mvt-fix`:

```bash
dotnet test src/tests/MiCake.IntegrationTests/MiCake.IntegrationTests.csproj --filter "FullyQualifiedName~UnitOfWork"
```
