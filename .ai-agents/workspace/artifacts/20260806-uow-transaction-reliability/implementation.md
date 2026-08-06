---
id: '20260806-uow-transaction-reliability'
task: 't1-uow-runtime-contracts'
source: 'design.md'
status: 'implemented'
---

# Implementation: UoW Transaction Reliability — t1: Rebuild provider-neutral UoW runtime

## Implementation Summary

Rebuilt the provider-neutral Unit of Work runtime in the `MiCake` package per ADR-001, ADR-003, ADR-006, ADR-007, and ADR-011. The unit of work is now the sole persistence owner: every writable root UoW uses explicit resource transactions, read-only UoWs reject flush and write activation, `PersistenceStrategy`/`Timeout`/`IDbContextWrapper`/`requiresNew` boolean overloads are removed, and a host-local ambient frame stack with isolated `requiresNew` and standalone execution scopes replaces the old AsyncLocal wrapper model. Shared nested completion performs no physical commit and raises no transaction events; nested rollback marks the root rollback-only. Commit is best-effort across resources with a structured per-resource outcome (`PartialUnitOfWorkCommitException`), and rollback failures surface combined primary/rollback causes (`UnitOfWorkBoundaryException`). Savepoints enforce coverage (resources registered after creation are rejected before any state change) and support-capability validation. Ambient frames are written only in synchronous segments so they land in the caller's execution context (async method bodies run on an execution-context copy in .NET 10), and the accessor self-heals completed/disposed frames on read.

## Files Touched

| Path | Action | Intent |
|---|---|---|
| `src/framework/MiCake/DDD/Uow/IUnitOfWorkResource.cs` | create | New provider resource lifecycle contract + `UnitOfWorkResourceContext` |
| `src/framework/MiCake/DDD/Uow/UnitOfWorkCommitOutcome.cs` | create | `UnitOfWorkResourceId`, commit-state enum, resource/commit outcome records |
| `src/framework/MiCake/DDD/Uow/IStandaloneUnitOfWorkExecutor.cs` | create | Explicit standalone execution contract |
| `src/framework/MiCake/DDD/Uow/Exceptions/UnitOfWorkBoundaryException.cs` | create | Combined primary + rollback/cleanup failure exception |
| `src/framework/MiCake/DDD/Uow/Exceptions/PartialUnitOfWorkCommitException.cs` | create | Structured partial-commit outcome exception |
| `src/framework/MiCake/DDD/Uow/Exceptions/SaveChangesReentryException.cs` | create | Deterministic re-entry failure (consumed by t4) |
| `src/framework/MiCake/DDD/Uow/UnitOfWorkOptions.cs` | modify | Sealed; removed `PersistenceStrategy` and `Timeout`; kept static factories |
| `src/framework/MiCake/DDD/Uow/IUnitOfWork.cs` | modify | Added `IsReadOnly`, `FlushAsync`, `IAsyncDisposable`; event semantics documented |
| `src/framework/MiCake/DDD/Uow/IUnitOfWorkManager.cs` | modify | New `BeginAsync(options?)`; `ExecuteRequiresNewAsync` x2; `IAsyncDisposable` |
| `src/framework/MiCake/DDD/Uow/Internal/AmbientUnitOfWorkAccessor.cs` | create | Host-local immutable AsyncLocal frame stack with self-healing read |
| `src/framework/MiCake/DDD/Uow/Internal/IUnitOfWorkInternal.cs` | modify | Retained `RegisterResource` + `ActivatePendingResourcesAsync` only |
| `src/framework/MiCake/DDD/Uow/Internal/UnitOfWork.cs` | rewrite | Root/shared state machine: flush, partial commit, rollback propagation, savepoint coverage, async disposal, sync-segment completion |
| `src/framework/MiCake/DDD/Uow/Internal/UnitOfWorkManager.cs` | rewrite | Ambient frames, nested creation, requiresNew isolated-scope execution with restoration |
| `src/framework/MiCake/DDD/Uow/Internal/StandaloneUnitOfWorkExecutor.cs` | create | Provider-neutral isolated-scope executor |
| `src/framework/MiCake/Modules/MiCakeEssentialModule.cs` | modify | Register singleton accessor, scoped manager + standalone executor |
| `src/framework/MiCake/DDD/Uow/IDbContextWrapper.cs` | delete | Replaced by `IUnitOfWorkResource` |
| `src/framework/MiCake/DDD/Uow/PersistenceStrategy.cs` | delete | Writable UoWs always use explicit transactions |
| `src/tests/MiCake.Tests/Uow/TestUowResource.cs` | create | Hand-written fake resource with call recording and failure injection |
| `src/tests/MiCake.Tests/Uow/UnitOfWorkTests.cs` | rewrite | Registration, flush, commit, partial commit, rollback, savepoints, events, disposal |
| `src/tests/MiCake.Tests/Uow/UnitOfWorkManagerTests.cs` | rewrite | BeginAsync, ambient Current, lifecycle hooks, requiresNew execution |
| `src/tests/MiCake.Tests/Uow/UnitOfWorkNestedTransactionTests.cs` | rewrite | Shared nested commit/rollback semantics, delegation, propagation |
| `src/tests/MiCake.Tests/Uow/UnitOfWorkImmediateModeTests.cs` | rewrite | Activation contract incl. read-only rejection |
| `src/tests/MiCake.Tests/Uow/UnitOfWorkTwoPhaseRegistrationTests.cs` | rewrite | `Prepare(context)` contract, lazy activation, savepoint activation |
| `src/tests/MiCake.Tests/Uow/StandaloneUnitOfWorkExecutorTests.cs` | create | Standalone reject/commit/rollback/boundary/disposal coverage |
| `src/tests/MiCake.Tests/Uow/PersistenceStrategyTests.cs` | delete | Strategy removed |

## Design Compliance

| Check | Result | Reason |
|---|---|---|
| Files touched == Change Tracking ± deviation | passed | Every touched file is in design File Structure Create/Modify/Delete; two new test files (`TestUowResource.cs`, `StandaloneUnitOfWorkExecutorTests.cs`) sit inside the hinted test folder |
| Module/layer placement | passed | All files live in `MiCake/DDD/Uow*` / `MiCake/Modules`; no EF Core or ASP.NET references |
| Public interfaces match Key Interfaces | passed | `IUnitOfWork`, `IUnitOfWorkManager`, `IStandaloneUnitOfWorkExecutor`, `UnitOfWorkOptions` (sealed), resource/outcome contracts match normative signatures; static factories `Default`/`Immediate`/`ReadOnly` retained |
| Forbidden cross-layer imports absent | passed | Only `MiCake.Core` abstractions + `Microsoft.Extensions.*` used |
| Error handling at boundaries only | passed | Commit/rollback/requiresNew/standalone boundaries raise `PartialUnitOfWorkCommitException` / `UnitOfWorkBoundaryException`; internal calls propagate provider exceptions unchanged |
| No new external dependencies | passed | No manifest change |

## Deviations from Design

1. **`SaveChangesReentryException` created in t1** — listed in design Create; created now so t4 stays EF-side only. It is not thrown until t4.
2. **Ambient frame write timing** — design states the accessor "maintains immutable AsyncLocal frames"; implementation additionally requires frame writes to happen in synchronous segments (`BeginAsync` and the sync wrappers of `CommitAsync`/`RollbackAsync`) and self-heals completed frames on read, because .NET 10 runs async method bodies on an execution-context copy whose AsyncLocal writes do not propagate back to the caller. Behavior is unchanged; this is an implementation constraint, not a contract change.
3. **Read-only/empty commit and rollback raise no events** — aligned with the design's "events tie to physical commit/rollback" semantics (documented in `IUnitOfWork` XML).
4. **`UnitOfWorkOptions` static factories kept** — design removes `Strategy`/`Timeout` only; `Immediate` now means immediate initialization without a strategy.

## Self-Check Results

- Type-checker: `dotnet build src/framework/MiCake/MiCake.csproj` — succeeded (0 warnings, 0 errors).
- Tests: `dotnet test src/tests/MiCake.Tests/MiCake.Tests.csproj` — 245/245 passed (95 UoW tests + 150 other tests, 0 failures).
- Expected compile breakage outside scope: `MiCake.EntityFrameworkCore`, `MiCake.AspNetCore`, `MiCake.IntegrationTests`, and `MiCake.EntityFrameworkCore.Tests` / `MiCake.AspNetCore.Tests` reference removed/renamed APIs and will be migrated by t2/t6; this matches the plan's task ordering.

## Open TODOs

- t2 (`mvt-implement`): adapt `EFCoreDbContextWrapper` to the new `IUnitOfWorkResource` contract (collision-safe `Id`, `Prepare(context)`, `EnsureTransaction[Async]`, `FlushAsync`, `ValueTask` lifecycle, async disposal, `SupportsSavepoints`), frame-stable context identity, registration/lifetime validation.
- t3: EF write coordinator/interceptor reads the ambient frame; must resolve the owning UoW via the frame chain for commit-time flush paths.
- t4: throw `SaveChangesReentryException` from the save-operation coordinator.
- t6: migrate `UnitOfWorkFilter`/sample to new `BeginAsync(options?)`, `FlushAsync`, and `ExecuteRequiresNewAsync` signatures.
- `mvt-review`: independently verify this artifact's compliance claims.

## Change Tracking

- Plan: `.ai-agents/workspace/artifacts/20260806-uow-transaction-reliability/plan.yaml`
- Task: `t1-uow-runtime-contracts` — implemented; status update deferred to `/mvt-update-plan`.
- Acceptance status: all 7 acceptance criteria of t1 are covered by the rewritten unit tests (95 passing), plus the `MiCake` project build.

### Deliverables

#### Public Interface

Consumed by downstream tasks; signatures match design Key Interfaces.

- `IUnitOfWork : IDisposable, IAsyncDisposable` — `Id`, `IsDisposed`, `IsCompleted`, `IsReadOnly`, `HasActiveTransactions`, `IsolationLevel`, `Parent`, `IsNested` (DIM); `FlushAsync(ct) -> Task<int>`; `CommitAsync` / `RollbackAsync` / `MarkAsCompletedAsync`; savepoint trio; `OnCommitting` / `OnCommitted` / `OnRollingBack` / `OnRolledBack`.
- `IUnitOfWorkManager : IDisposable, IAsyncDisposable` — `Current`; `BeginAsync(UnitOfWorkOptions?, ct) -> Task<IUnitOfWork>`; `ExecuteRequiresNewAsync(Func<IServiceProvider, CancellationToken, Task>, options?, ct)` + generic overload (throws `InvalidOperationException` without an outer UoW).
- `IStandaloneUnitOfWorkExecutor` — `ExecuteAsync(Func<IServiceProvider, CancellationToken, Task>, options?, ct)` + generic overload; rejects an existing ambient UoW.
- `UnitOfWorkOptions` (sealed) — `IsReadOnly`, `IsolationLevel`, `InitializationMode` (`Lazy` default); statics `Default` / `Immediate` / `ReadOnly`. `PersistenceStrategy` and `Timeout` removed.
- `IUnitOfWorkResource : IDisposable, IAsyncDisposable` — `Id: UnitOfWorkResourceId`, `ResourceType`, `HasActiveTransaction`, `SupportsSavepoints`; `Prepare(UnitOfWorkResourceContext)`; `EnsureTransaction()` + `EnsureTransactionAsync`; `FlushAsync -> ValueTask<int>`; `CommitAsync` / `RollbackAsync`; savepoint trio; `UnitOfWorkResourceContext(UnitOfWorkId, IsolationLevel, IsReadOnly)`.
- Outcome types — `UnitOfWorkResourceId(Guid)`, `UnitOfWorkResourceCommitState` (Pending/Committed/Failed/RolledBack/RollbackFailed), `UnitOfWorkResourceOutcome`, `UnitOfWorkCommitOutcome`; exceptions `UnitOfWorkBoundaryException` (PrimaryException/RollbackExceptions/CleanupExceptions) and `PartialUnitOfWorkCommitException` (Outcome/CommitFailures/RollbackFailures); `SaveChangesReentryException` (thrown from t4).
- `IUnitOfWorkInternal` (public, `MiCake.DDD.Uow.Internal`) — `RegisterResource(IUnitOfWorkResource)` and `ActivatePendingResourcesAsync(ct)`; used by the EF resource registry.

#### Data Shapes

- `UnitOfWorkResourceContext(UnitOfWorkId: Guid, IsolationLevel: IsolationLevel?, IsReadOnly: bool)` — passed to `Prepare`; the resource must retain it.
- `UnitOfWorkFrame` / `UnitOfWorkFrameToken` (internal) — immutable ambient frame `(Token, UnitOfWork, ServiceProvider, Previous)`; the frame's `ServiceProvider` is the source for lifecycle-handler resolution (ADR-004) and for resolving inner repositories/DbContexts in `requiresNew`/standalone callbacks.
- Commit outcome records contain only IDs, type names, and states — no SQL, entity values, or connection strings.

#### Usage Constraints

- **Ambient writes must occur in synchronous segments** — `BeginAsync` pushes the frame synchronously; async completion APIs (`CommitAsync`/`RollbackAsync` on nested/read-only/empty UoWs) complete synchronously; the accessor self-heals completed/disposed frames on read. EF integration (t2/t3) must read the ambient frame, not write it, and must tolerate self-healing.
- Nested UoWs delegate resource registration, activation, flush, and savepoints to the root; `Prepare` receives the ROOT's `UnitOfWorkId`. A resource may be prepared only once per UoW (deduped by `UnitOfWorkResourceId`); rebinding to another live UoW is the resource's responsibility (t2).
- Read-only UoWs: registration allowed, `FlushAsync`/`ActivatePendingResourcesAsync`/savepoint creation throw `InvalidOperationException`.
- Commit/rollback are terminal and at most once per resource; rollback attempts cover only Pending/Failed resources (never Committed/RolledBack/RollbackFailed). The UoW is NOT marked completed after a partial commit — the boundary must roll back explicitly.
- `ExecuteRequiresNewAsync` callbacks must resolve repositories/DbContexts from the supplied provider; capturing outer scoped persistence services is detected by ownership validation in t2.
- Standalone execution requires no ambient UoW and owns scope → UoW → disposal in reverse order.


## Task: t2-ef-resource-identity — Implement EF resource and context identity

## Implementation Summary

Adapted the EF Core integration to the provider-neutral resource contract from t1. The ``EFCoreDbContextWrapper`` now implements ``IUnitOfWorkResource`` with collision-safe identity, per-UoW prepare/rebind rules, idempotent lazy transaction activation, terminal commit/rollback with failure propagation, async disposal, and explicit savepoint non-support. ``EFCoreContextFactory`` provides frame-stable context identity keyed by the root UoW id (shared nested UoWs share the wrapper; requiresNew/standalone scopes own theirs) with ownership validation that rejects reuse by another live unit of work and self-heals entries whose root completed. A non-generic ``IEFCoreContextFactory`` enables typed, reflection-free immediate initialization (``GetServices<IEFCoreContextFactory>``). DbContext registrations are validated for supported lifetimes (scoped/pooled pass; missing/singleton/transient fail with context-specific diagnostics), and startup validation rejects retrying execution strategies (ADR-011).

## Files Touched

| Path | Action | Intent |
|---|---|---|
| `src/framework/MiCake.EntityFrameworkCore/Uow/EFCoreDbContextWrapper.cs` | rewrite | `IUnitOfWorkResource` contract: collision-safe `Id`, `Prepare`/rebind rules, idempotent `EnsureTransaction[Async]`, `FlushAsync`, terminal `CommitAsync`/`RollbackAsync` with failure propagation, savepoint `NotSupportedException`, sync/async disposal |
| `src/framework/MiCake.EntityFrameworkCore/Uow/EFCoreContextFactory.cs` | rewrite | Non-generic `IEFCoreContextFactory`; frame-stable wrapper cache keyed by root UoW id; ownership validation against live roots; stale-root self-healing; bypass mode retained |
| `src/framework/MiCake.EntityFrameworkCore/Uow/AddCoreUowServicesExtension.cs` | modify | Registers generic + non-generic factories and core services (TryAdd deduplication); `ValidateDbContextRegistration` lifetime validation |
| `src/framework/MiCake.EntityFrameworkCore/Uow/ImmediateTransactionInitializer.cs` | modify | Reflection-free typed activation via `GetServices<IEFCoreContextFactory>()`; `IDbContextTypeRegistry` unchanged |
| `src/framework/MiCake.EntityFrameworkCore/Modules/MiCakeEFCoreModule.cs` | modify | Registers DbContext type; `ValidateExecutionStrategies` rejects `RetriesOnFailure=true` with context-specific diagnostic (ADR-011) |
| `src/tests/MiCake.EntityFrameworkCore.Tests/Uow/EFCoreDbContextWrapperTests.cs` | rewrite | New-contract tests: identity, prepare/rebind, activation, flush, commit/rollback, savepoint rejection, disposal |
| `src/tests/MiCake.EntityFrameworkCore.Tests/Uow/EFCoreContextFactoryTests.cs` | modify | Added frame-stable identity, shared-nested sharing, live-root ownership rejection, stale-root recreation |
| `src/tests/MiCake.EntityFrameworkCore.Tests/Uow/BypassUnitOfWorkCheckTests.cs` | modify | DI registers `AmbientUnitOfWorkAccessor`; UoW-bound test moved to file-backed SQLite (InMemory lacks transactions) |
| `src/tests/MiCake.EntityFrameworkCore.Tests/Uow/ImmediateTransactionInitializerTests.cs` | rewrite | Reflection-free factory enumeration; no-factory warning; wrapper resolution per factory |
| `src/tests/MiCake.EntityFrameworkCore.Tests/Uow/AddCoreUowServicesExtensionTests.cs` | modify | Lifetime validation tests: missing/transient/singleton rejected, pooled passes |
| `src/tests/MiCake.EntityFrameworkCore.Tests/Uow/MiCakeEFCoreModuleValidationTests.cs` | create | Execution-strategy validation: retrying strategy rejected, non-retrying passes |

## Design Compliance

| Check | Result | Reason |
|---|---|---|
| Files touched == Change Tracking ± deviation | passed | All five production files are in design t2 Modify list; tests inside `src/tests/MiCake.EntityFrameworkCore.Tests/Uow`; one new test file (`MiCakeEFCoreModuleValidationTests.cs`) added for startup validation coverage |
| Module/layer placement | passed | All files in `MiCake.EntityFrameworkCore/Uow` and `Modules`; references only `MiCake` + EF Core |
| Public interfaces match Key Interfaces | passed | `EFCoreDbContextWrapper` implements `IUnitOfWorkResource` per t1 contract; factory/framework types retained; new non-generic `IEFCoreContextFactory` documented as deviation |
| Forbidden cross-layer imports absent | passed | No references to ASP.NET or repository internals from the new code |
| Error handling at boundaries only | passed | Provider failures propagate from wrapper commit/rollback/dispose; validation failures at startup/registration boundary; no interior catch-and-swallow |
| No new external dependencies | passed | No manifest change |

## Deviations from Design

1. **Non-generic `IEFCoreContextFactory`** — design lists `EFCoreContextFactory` under Modify; the non-generic factory interface is the typed, reflection-free vehicle for immediate initialization required by t2 acceptance (ADR-009). Public addition, no behavioral change.
2. **`SupportsSavepoints = false`** — EF Core exposes no public savepoint API on `IDbContextTransaction`; the UoW rejects savepoint creation before state change. A provider-specific savepoint path (e.g. `DbTransaction.Save`) can be layered on later if t7 requires it.
3. **`EFCoreDbContextWrapper` constructor drops the `MiCakeEFCoreOptions` parameter** — the strategy option was removed in t1 and the options object served no remaining purpose; breaking change within the approved t1/t2 contract breakage.
4. **`AddUowCoreServices` now validates the DbContext registration and throws on missing/non-scoped lifetimes** — new startup diagnostics per t2 acceptance (unsupported lifetimes fail with context-specific guidance); pooled registrations pass because their DI lifetime is scoped.

## Self-Check Results

- Type-checker: `dotnet build src/framework/MiCake.EntityFrameworkCore/MiCake.EntityFrameworkCore.csproj` — succeeded (0 warnings, 0 errors).
- Tests: `dotnet test src/tests/MiCake.EntityFrameworkCore.Tests/MiCake.EntityFrameworkCore.Tests.csproj` — 191/191 passed (0 failures).
- Regression: `MiCake.Tests` (258/258) unaffected by this task; EF Core changes are additive to the t1 runtime.

## Open TODOs

- t3: `EFCoreWriteCoordinator` / `MiCakeDbCommandInterceptor` must resolve the ambient frame, guard writes, and bind `DbCommand.Transaction`; the wrapper exposes `DbContext` for the interceptor paths.
- t4: lifecycle state machine consumes the wrapper's `FlushAsync`; re-entry coordinator throws `SaveChangesReentryException`.
- t5: `EFRepositoryBase` context cache can rely on `IEFCoreContextFactory` frame-stable identity; repository-owned save APIs removed.
- t7: relational savepoint tests may require a provider-specific savepoint path if EF Core gains support.
- `mvt-review`: independently verify this artifact's compliance claims.

## Change Tracking

- Plan: `.ai-agents/workspace/artifacts/20260806-uow-transaction-reliability/plan.yaml`
- Task: `t2-ef-resource-identity` — implemented; status update deferred to `/mvt-update-plan`.
- Acceptance status: all 6 acceptance criteria covered by the Uow tests (97) plus validation tests; EF Core project builds cleanly.

### Deliverables

#### Public Interface

Consumed by t3 (write coordinator/interceptor), t5 (repositories), and t7 (relational acceptance).

- `IEFCoreContextFactory` (non-generic) — `EFCoreDbContextWrapper GetDbContextWrapper()`; registered scoped per DbContext type for typed immediate initialization.
- `IEFCoreContextFactory<TDbContext> : IEFCoreContextFactory` — adds `TDbContext GetDbContext()`; frame-stable: the same root UoW and DbContext type resolve the same context/wrapper; nested UoWs share the root wrapper; a wrapper bound to another live root is rejected with guidance to use `ExecuteRequiresNewAsync` or a separate scope.
- `EFCoreDbContextWrapper : IUnitOfWorkResource` — `Id: UnitOfWorkResourceId` (Guid, collision-safe), `ResourceType` (DbContext full name), `HasActiveTransaction`, `SupportsSavepoints = false` (savepoint methods throw `NotSupportedException`); `Prepare(UnitOfWorkResourceContext)` idempotent per UoW and rejecting rebind; `EnsureTransaction()`/`EnsureTransactionAsync` idempotent; `FlushAsync -> ValueTask<int>` (`SaveChangesAsync`); `CommitAsync`/`RollbackAsync` terminal at-most-once with failure propagation; `Dispose`/`DisposeAsync` (async path disposes the owned DbContext). Constructor `(DbContext, ILogger<EFCoreDbContextWrapper>, bool shouldDisposeDbContext = false)`.
- `AddUowCoreServices(IServiceCollection, Type)` — registers generic + non-generic factories, repository dependencies, immediate hook/initializer, type registry; validates the DbContext registration (scoped/pooled only) with context-specific diagnostics.
- `MiCakeEFCoreModule.ValidateExecutionStrategies(IServiceProvider, IDbContextTypeRegistry)` (internal static) — rejects `RetriesOnFailure=true` execution strategies at startup (ADR-011).
- `IDbContextTypeRegistry` — unchanged public shape (`RegisterDbContextType(Type)` / `GetRegisteredTypes()`); immediate initialization no longer consumes it.

#### Data Shapes

- `UnitOfWorkResourceContext(UnitOfWorkId, IsolationLevel?, IsReadOnly)` — retained by the wrapper; `Prepare` uses `UnitOfWorkId` for rebind rejection.
- Wrapper state: `_currentTransaction` (`IDbContextTransaction?`), prepared UoW id/isolation/read-only, terminal flags (commit/rollback at-most-once).
- Frame-stable cache entry: `(root UoW, EFCoreDbContextWrapper)` keyed by root UoW id inside the scoped factory; entries are invalidated when the bound root completes/disposes.

#### Usage Constraints

- **Resource outcome contract correction (from t1 fix)**: `UnitOfWorkResourceOutcome` carries `CommitState` + `RollbackState?` — a commit-failed resource is never conflated with a never-committed one; t3/t7 must read `CommitState` for failed-identity and `RollbackState` for compensation results.
- The wrapper must be prepared before activation/flush/commit; the UoW runtime guarantees prepare (registration) precedes activation and flush.
- Commit and rollback are terminal per wrapper instance; the UoW runtime never calls them twice (rollback eligibility excludes committed/rolled-back resources).
- Rollback/disposal failures propagate to the UoW boundary so `UnitOfWorkBoundaryException` can carry primary/rollback/cleanup causes.
- Providers that cannot activate synchronously are unsupported for sync write paths (t3 enforces the diagnostic); the wrapper's synchronous `EnsureTransaction` uses the EF synchronous transaction API.
- `SupportsSavepoints = false`: any savepoint creation attempt fails before state change; t7 savepoint coverage must account for this capability report.

## Task: t3-write-transaction-guards — Guard and bind every EF write

## Implementation Summary

Implemented ADR-002: every supported EF write is guarded and bound to the explicit unit of work transaction before SQL execution. A scoped `EFCoreWriteCoordinator` rejects missing and read-only units of work, resolves the frame-stable resource wrapper, activates its transaction, and binds the current provider transaction to non-query commands. `SavingChanges[Async]` on `MiCakeEFCoreInterceptor` activates the transaction before EF builds write commands; the new `MiCakeDbCommandInterceptor` guards and binds ExecuteSqlRaw, ExecuteDelete/ExecuteUpdate, and unknown non-query commands, and passes database-initiation DDL (Migrations) through unbound when no unit of work is active. Interceptors are stateless and resolve the coordinator from the provider of the scope that owns the DbContext, preserving frame-stable context identity.

## Files Touched

| Path | Action | Intent |
|---|---|---|
| `src/framework/MiCake.EntityFrameworkCore/Internal/EFCoreWriteCoordinator.cs` | create | `EFWriteOperationKind`, `IEFCoreWriteCoordinator`, scoped guard/activation/binding implementation |
| `src/framework/MiCake.EntityFrameworkCore/Internal/MiCakeDbCommandInterceptor.cs` | create | Stateless `DbCommandInterceptor` guarding and binding every non-query write |
| `src/framework/MiCake.EntityFrameworkCore/Internal/MiCakeEFCoreInterceptor.cs` | modify | `SavingChanges[Async]` activates the transaction before EF writes |
| `src/framework/MiCake.EntityFrameworkCore/Internal/IMiCakeInterceptorFactory.cs` | modify | Add `CreateInterceptor(IServiceProvider?)` and `CreateCommandInterceptor(IServiceProvider?)` |
| `src/framework/MiCake.EntityFrameworkCore/Internal/MiCakeInterceptorFactory.cs` | modify | Implement provider-aware interceptor creation; static helper forwards the provider |
| `src/framework/MiCake.EntityFrameworkCore/Extensions/DbContextExtensions.cs` | modify | All `UseMiCakeInterceptors` overloads install the command interceptor; DI overload forwards the scope provider |
| `src/framework/MiCake.EntityFrameworkCore/Uow/AddCoreUowServicesExtension.cs` | modify | Register `IEFCoreWriteCoordinator` as scoped |
| `src/tests/MiCake.EntityFrameworkCore.Tests/Internal/EFCoreWriteCoordinatorTests.cs` | create | Guard (missing/read-only), activation, commit/rollback, command binding, connection mismatch, unregistered context |
| `src/tests/MiCake.EntityFrameworkCore.Tests/Internal/MiCakeWriteGuardInterceptorsTests.cs` | create | End-to-end: SaveChanges/ExecuteSqlRaw/ExecuteDelete rejected without UoW and transactional inside one |

`MiCakeDbContext.cs` is unchanged: `OnConfiguring` already installs interceptors via `UseMiCakeInterceptors()`; the static-helper path limitation is recorded in Deviations.

## Design Compliance

| Check | Result | Reason |
|---|---|---|
| Files touched == Change Tracking ± deviation | passed | t3 Modify/Create lists matched; `AddCoreUowServicesExtension.cs` gained one registration line (see Deviations) |
| Module/layer placement | passed | All production files in `Internal` / `Extensions` / `Uow` of MiCake.EntityFrameworkCore |
| Public interfaces match Key Interfaces | passed | `IEFCoreWriteCoordinator` + `EFWriteOperationKind` match the design signatures |
| Forbidden cross-layer imports absent | passed | No ASP.NET references; coordinator uses UoW contracts + EF Core only |
| Error handling at boundaries only | passed | Failures raised before SQL execution with specific diagnostics; no interior catch-and-swallow |
| No new external dependencies | passed | No manifest change |

## Deviations from Design

1. **Interceptors resolve the coordinator from the provider of the scope that owns the DbContext** — EF's `GetInfrastructure()` provider does not expose application-registered scoped services (verified empirically), so `UseMiCakeInterceptors(IServiceProvider)` forwards the AddDbContext options-delegate provider into the interceptors; the static-helper overload creates interceptors without a provider, which disables the write guard for that path (legacy compatibility; the DI overload is the supported path and is the pattern used by the sample and README).
2. **`CommandSource` classification facts** — EF Core 10 reports ExecuteDelete/ExecuteUpdate commands as `BulkUpdate` (same constant value as `ExecuteUpdate`); `ExecuteSqlRaw` commands report `CommandSource.ExecuteSqlRaw`; EnsureCreated/Migrate DDL reports `CommandSource.Migrations`. The classifier maps these accordingly; `Migrations` and unknown non-query commands without an ambient UoW pass through unbound as database initialization (deviation from the conservative treat-unknown-as-write rule, required so `EnsureCreated`/`Migrate` remain usable; with an active UoW they are bound).
3. **`IMiCakeInterceptorFactory` gained provider-aware overloads** — additive public extension; the factory itself is scheduled for rework in t4.
4. **`AddCoreUowServicesExtension.AddUowCoreServices` registers the scoped write coordinator** — one line added to the t2 registration file; without it the interceptors cannot resolve the pipeline.

## Self-Check Results

- Type-checker: `dotnet build src/framework/MiCake.EntityFrameworkCore/MiCake.EntityFrameworkCore.csproj` — succeeded (0 warnings, 0 errors).
- Tests: `dotnet test src/tests/MiCake.EntityFrameworkCore.Tests/MiCake.EntityFrameworkCore.Tests.csproj` — 217/217 passed (203 existing + 14 new guard/binding tests).
- Regression: `MiCake.Tests` UoW tests 108/108 passed; MiCake build unaffected.

## Open TODOs

- t4: save lifecycle state machine consumes the guard/activation order; replace the singleton `_changedEntries` field with the per-context operation state; rework `MiCakeInterceptorFactory` and remove the process-global static helper configuration.
- t5: repositories can rely on the guard for every write path; `MiCakeDbContext` static-helper path should be migrated to the DI overload in t6 with the sample.
- t7: relational acceptance must re-verify first-command binding for bulk/raw SQL against file-backed SQLite (unit-level binding is covered here).
- `mvt-review`: independently verify this artifact's compliance claims.

## Change Tracking

- Plan: `.ai-agents/workspace/artifacts/20260806-uow-transaction-reliability/plan.yaml`
- Task: `t3-write-transaction-guards` — implemented; status update deferred to `/mvt-update-plan`.
- Acceptance status: 5 of 6 acceptance criteria covered by the new tests (first SaveChanges inside the UoW transaction, first bulk/raw SQL bound, missing/read-only/mismatch failures before SQL, sync/async paths, focused tests + clean build). Multi-host interceptor isolation is satisfied by stateless interceptors plus per-scope coordinator resolution; the process-global static factory rework is a t4 deliverable.

### Deliverables

#### Public Interface

Consumed by t5 (repositories) and t7 (relational acceptance).

- `IEFCoreWriteCoordinator` (internal, scoped) — `BeforeWrite(BeforeWriteAsync)` and `BeforeCommand(BeforeCommandAsync)` with `EFWriteOperationKind`; resolves the frame-stable wrapper, activates its transaction, and binds provider transactions to commands.
- `EFWriteOperationKind` (internal) — `SaveChanges`, `ExecuteUpdate`, `ExecuteDelete`, `RawSql`, `PhysicalDelete`, `UnknownNonQuery`.
- `MiCakeDbCommandInterceptor` (internal, stateless) — installs with `UseMiCakeInterceptors()`; guards every non-query write before SQL execution.
- `IMiCakeInterceptorFactory` — `CreateInterceptor(IServiceProvider?)` / `CreateCommandInterceptor(IServiceProvider?)`; the provider is the scope that owns the DbContext.

#### Data Shapes

- `CommandSource` classification facts for EF Core 10: `ExecuteDelete`/`ExecuteUpdate` commands arrive as `BulkUpdate` (same value as `CommandSource.ExecuteUpdate`); `ExecuteSqlRaw` arrives as `CommandSource.ExecuteSqlRaw`; EnsureCreated/Migrate DDL arrives as `CommandSource.Migrations`.
- Guard diagnostics: missing UoW (write operation + context type + BeginAsync guidance), read-only rejection (UoW id), connection mismatch, conflicting transaction, unavailable provider transaction — all raised before SQL execution.

#### Usage Constraints

- The write guard only activates when interceptors are installed with a scope provider (`UseMiCakeInterceptors(IServiceProvider)` inside `AddDbContext`); the static-helper overload installs no guard (legacy compatibility, migrated in t6).
- Database initialization DDL (`Migrations`/unknown non-query without an ambient UoW) passes unbound; with an active UoW all non-query commands are bound to its transaction.
- t5: repository writes may rely on the guard for direct DbContext saves, bulk updates/deletes, and raw SQL inside an ambient writable UoW; read-only and missing-UoW writes fail before SQL execution.
- t7: re-verify first-command binding and rollback of bulk/raw SQL against file-backed SQLite; unit-level binding is already covered in `MiCakeWriteGuardInterceptorsTests`.

## Task: t4-lifecycle-reentry-state — Isolate save lifecycle and re-entry

## Implementation Summary

Implemented ADR-004 and ADR-005 as a per-DbContext root save-operation state machine. A new `SaveOperationStateAccessor` (internal service registered through a new EF Core options extension) owns the root operation frame: immutable pre-save entity snapshots, dispatched event identities, re-entry state, cycle count, and the owning scope provider. `MiCakeEFCoreInterceptor` was rewritten to drive the state machine: the first SaveChanges becomes the root operation (guard, snapshot, pre-save handlers), nested SaveChanges from lifecycle handlers are suppressed and recorded as re-entry requests, and the root coordinator re-scans changes into bounded follow-up save cycles in the same transaction. Handlers are always resolved from the provider of the scope that owns the unit of work; no temporary scopes are created and no static or singleton mutable state remains. The accessor implements the EF Core pool reset contract (`IResettableService`) and every success, failure, cancellation, and reset path converges on `EndOperation`. `MiCakeEFCoreOptions.MaxSaveCycles` (default 16) bounds the loop; no-progress and cycle-limit paths throw `SaveChangesReentryException`.

## Files Touched

| Path | Action | Intent |
|---|---|---|
| `src/framework/MiCake.EntityFrameworkCore/Internal/SaveOperationStateAccessor.cs` | create | Per-DbContext root operation frame, re-entry state, `IResettableService` pool-reset contract |
| `src/framework/MiCake.EntityFrameworkCore/Internal/MiCakeSaveOperationOptionsExtension.cs` | create | `IDbContextOptionsExtension` registering the accessor and carrying `MaxSaveCycles` |
| `src/framework/MiCake.EntityFrameworkCore/Internal/SaveOperationEntityHelper.cs` | create | Shared stateless changed-entity detection (owner/owned handling) without mutable caches |
| `src/framework/MiCake.EntityFrameworkCore/Internal/MiCakeEFCoreInterceptor.cs` | rewrite | Root/nested/re-entry state machine; pre/post save cycles; failure cleanup in interceptor |
| `src/framework/MiCake.EntityFrameworkCore/Internal/LazyEFSaveChangesLifetime.cs` | rewrite | Stateless scoped implementation; no temp scopes, no static caches (compat shim) |
| `src/framework/MiCake.EntityFrameworkCore/Internal/MiCakeInterceptorFactory.cs` | modify | Drop `IEFSaveChangesLifetime` dependency (interceptor no longer consumes it) |
| `src/framework/MiCake.EntityFrameworkCore/MiCakeEFCoreOptions.cs` | modify | Add `MaxSaveCycles` (public option, default 16) |
| `src/framework/MiCake.EntityFrameworkCore/Extensions/DbContextExtensions.cs` | modify | All `UseMiCakeInterceptors` overloads apply the options extension |
| `src/framework/MiCake.EntityFrameworkCore/Modules/MiCakeEFCoreModule.cs` | modify | Register `IEFSaveChangesLifetime` as scoped |
| `src/tests/MiCake.EntityFrameworkCore.Tests/Internal/SaveOperationConcurrencyTests.cs` | create | Isolation, pool reset, failure cleanup, pre-save state retention, re-entry, event dedup, cycle limit |
| `src/tests/MiCake.EntityFrameworkCore.Tests/Internal/MiCakeInterceptorFactoryTests.cs` | modify | Adapt to new factory constructor |
| `src/tests/MiCake.EntityFrameworkCore.Tests/Internal/LazyEFSaveChangesLifetimeTests.cs` | modify | Adapt to stateless scoped constructor |

## Design Compliance

| Check | Result | Reason |
|---|---|---|
| Files touched == Change Tracking ± deviation | passed | t4 Modify/Create lists matched; three additions recorded in Deviations |
| Module/layer placement | passed | All production files in `Internal` / `Extensions` / `Modules` of MiCake.EntityFrameworkCore |
| Public interfaces match Key Interfaces | passed | No public interface changes; `MiCakeEFCoreOptions.MaxSaveCycles` is a documented additive option |
| Forbidden cross-layer imports absent | passed | No ASP.NET references; MiCake DDD contracts only |
| Error handling at boundaries only | passed | Failure cleanup inside the interceptor; `SaveChangesReentryException` at the save boundary |
| No new external dependencies | passed | No manifest change |

## Deviations from Design

1. **EF Core does not raise `SaveChangesFailed` when the failure originates in the `SavingChanges` interceptor** (verified empirically) — the interceptor ends the operation frame in its own catch so a failed pre-save cannot leave a stale root frame.
2. **Domain-event dedup relies on the existing `DomainEventCleanupLifetime` post-save clear plus a defensive dispatched-identity record** — `IDomainEventAccessor` is internal to the MiCake assembly and invisible to the EF Core package, so per-event removal is not possible; the post-save clear already prevents cross-cycle re-dispatch (covered by the event test).
3. **Legacy static interceptor path (`UseMiCakeInterceptors()` without a provider) is now a no-op** — without a scope provider the interceptor cannot resolve handlers or the write pipeline; it stays a no-op (previously it only lacked the write guard). The DI overload is the supported path (migrated in t6).
4. **`IMiCakeInterceptorFactory` is unchanged from t3** — t4 rewrote the factory implementation only (constructor no longer requires `IEFSaveChangesLifetime`).
5. **`DbContextExtensions` and `MiCakeEFCoreModule` gained one line each** (options extension application; scoped lifetime registration) — outside the t4 Change Tracking list but required for the accessor registration.

## Self-Check Results

- Type-checker: `dotnet build src/framework/MiCake.EntityFrameworkCore/MiCake.EntityFrameworkCore.csproj` — succeeded (0 warnings, 0 errors).
- Tests: `dotnet test src/tests/MiCake.EntityFrameworkCore.Tests/MiCake.EntityFrameworkCore.Tests.csproj` — 224/224 passed (217 existing + 7 new lifecycle/concurrency tests).
- Regression: `MiCake.Tests` 258/258 passed.
- Note: `MiCake.IntegrationTests` still fails to compile because `MiCake.AspNetCore` references removed t1 APIs (`PersistenceStrategy`, `requiresNew`); this is the planned t6 migration, not a t4 regression.

## Open TODOs

- t5: repositories rely on the guard/lifecycle pipeline; `MiCakeDbContext` static path should be migrated to the DI overload in t6 with the sample.
- t6: remove the process-global static `MiCakeInterceptorFactoryHelper` configuration and migrate the ASP.NET boundary (unblocks `MiCake.IntegrationTests`).
- t7: relational acceptance must re-verify re-entry, no-progress, and pool-reset against file-backed SQLite.
- `mvt-review`: independently verify this artifact's compliance claims.

### Deliverables

#### Public Interface

Consumed by t5 (repositories) and t7 (relational acceptance).

- `SaveOperationStateAccessor` (internal, per-DbContext scoped service registered via the options extension) — root operation frame lifecycle: `TryBeginRoot`, `RequestReentry`/`ConsumeReentryRequest`, `MarkRootCycleSave`/`ConsumeRootCycleSave`, `TryMarkEventDispatched`, `EndOperation`, `IResettableService.ResetState[Async]`.
- `EntityStateSnapshot` (internal) — `(EntityEntry Entry, RepositoryEntityStates State)`; post-save handlers receive the pre-save state even after EF mutates the entry.
- `MiCakeEFCoreOptions.MaxSaveCycles` (public, default 16) — bounds the controlled re-entry loop; both the no-progress path and the cycle-limit path throw `SaveChangesReentryException`.

#### Data Shapes

- Re-entry semantics: nested SaveChanges from a lifecycle handler is suppressed (no SQL) and recorded; the root re-scans changes into a follow-up save cycle in the same transaction. A re-entry request without new pending changes throws `SaveChangesReentryException` (no progress). Handlers that keep generating changes hit `MaxSaveCycles`.
- Event dedup: `DomainEventCleanupLifetime` clears entity events after each post-save; the accessor additionally records dispatched identities (defensive). New events raised in later cycles are eligible for dispatch.

#### Usage Constraints

- Handlers (`IRepositoryPreSaveChanges` / `IRepositoryPostSaveChanges`) are resolved from the provider of the scope that owns the unit of work; they must NOT create their own DI scopes.
- The lifecycle pipeline requires the DI interceptor path (`UseMiCakeInterceptors(IServiceProvider)`); the legacy static path installs a no-op interceptor (migrated in t6).
- EF Core does not raise `SaveChangesFailed` for interceptor-originated failures; the interceptor ends the operation frame itself.
- t5: repositories can rely on pre/post-save handlers running per root save operation with retained pre-save states; post-save handlers must tolerate re-entry cycles (they run once per cycle).
- t7: re-verify re-entry, no-progress, and pool-reset behavior against file-backed SQLite; pool reset is exercised through `IResettableService`.

## Task: t5-repository-contracts — Migrate repository and paging behavior

## Implementation Summary

Implemented ADR-001's repository ownership contract: repository mutations only modify tracked state and never save or commit independently of the ambient unit of work. `AddAndReturnAsync`, `SaveChangesAsync`, and `ClearChangeTrackingAsync` are removed from `IRepository` and `EFRepository`; identity-generating writes use `AddAsync` plus `IUnitOfWork.FlushAsync` (documented on `AddAsync`). `DeleteByIdAsync` now loads the aggregate into the stable UoW context and performs tracked deletion, so audit, soft deletion, domain events, and rollback semantics match `DeleteAsync`; a missing id is a no-op. `UpdateAsync` remains full detached-aggregate replacement with configured concurrency tokens preserved, so stale replacements surface `DbUpdateConcurrencyException` at flush/commit instead of silently overwriting. The repository-local context cache (`EFRepositoryBase`) is removed; context identity is delegated to the frame-stable `IEFCoreContextFactory` from t2. A new `IEFCorePhysicalOperationExecutor<TDbContext>` provides explicit lifecycle-bypassing physical deletion inside the UoW transaction (registered scoped in `AddUowCoreServices`). Paging now establishes a total order before Skip/Take: without caller sorting every primary-key property is applied ascending in EF model order; with caller sorting the caller direction is preserved and missing primary-key properties are appended as ascending final ThenBy clauses; keyless entity types are rejected with a diagnostic.

## Files Touched

| Path | Action | Intent |
|---|---|---|
| `src/framework/MiCake/DDD/Domain/IRepository.cs` | modify | Remove `AddAndReturnAsync`, `SaveChangesAsync`, `ClearChangeTrackingAsync`; document UoW-owned persistence, detached update semantics, and DeleteById lifecycle |
| `src/framework/MiCake.EntityFrameworkCore/Repository/EFRepository.cs` | modify | Remove removed-API implementations; `DeleteByIdAsync` loads then tracked-deletes; missing id no-op |
| `src/framework/MiCake.EntityFrameworkCore/Repository/EFRepositoryBase.cs` | modify | Remove per-repository context cache (dictionary, locks, UoW event subscriptions); delegate directly to the frame-stable context factory |
| `src/framework/MiCake.EntityFrameworkCore/Repository/EFRepositoryHasPaging.cs` | modify | Deterministic total order before Skip/Take; append missing primary-key properties; keyless rejection |
| `src/framework/MiCake.EntityFrameworkCore/Repository/IEFCorePhysicalOperationExecutor.cs` | create | Explicit lifecycle-bypassing physical delete contract |
| `src/framework/MiCake.EntityFrameworkCore/Repository/EFCorePhysicalOperationExecutor.cs` | create | Scoped implementation: ambient writable UoW guard, frame-stable context, `ExecuteDeleteAsync` in the UoW transaction |
| `src/framework/MiCake.EntityFrameworkCore/Uow/AddCoreUowServicesExtension.cs` | modify | Register `IEFCorePhysicalOperationExecutor<TDbContext>` as scoped (deviation: outside t5 Change Tracking list, one registration line) |
| `src/tests/MiCake.EntityFrameworkCore.Tests/Repository/EFRepositoryBaseTests.cs` | modify | Cache-semantics tests rewritten to factory-delegation semantics |
| `src/tests/MiCake.EntityFrameworkCore.Tests/Repository/RepositoryWithoutUoWTests.cs` | modify | Rename cache test to delegation; verify factory per-access |
| `src/tests/MiCake.EntityFrameworkCore.Tests/Repository/EFRepositoryHasPagingTests.cs` | modify | Primary-key ordering, caller-sort tie-break, keyless rejection tests |
| `src/tests/MiCake.EntityFrameworkCore.Tests/Repository/EFRepositoryContractTests.cs` | create | DeleteById lifecycle, physical delete bypass/rollback/guards, concurrency token conflict, missing-id no-op |

## Design Compliance

| Check | Result | Reason |
|---|---|---|
| Files touched == Change Tracking ± deviation | passed | All 7 t5 files touched; one extra registration line in `AddCoreUowServicesExtension.cs` (recorded in Deviations) |
| Module/layer placement | passed | `MiCake/DDD/Domain`, `MiCake.EntityFrameworkCore/Repository` and `/Uow` only; no ASP.NET references |
| Public interfaces match Key Interfaces | passed | `IRepository` matches the normative contract; `IEFCorePhysicalOperationExecutor` signature matches the design Key Interfaces |
| Forbidden cross-layer imports absent | passed | EF repository/executor reference MiCake DDD contracts and EF Core only |
| Error handling at boundaries only | passed | Physical-executor guards at the API boundary; repository methods contain no interior catch |
| No new external dependencies | passed | No manifest change |

## Deviations from Design

1. **`AddCoreUowServicesExtension.cs` registration line** — `IEFCorePhysicalOperationExecutor<TDbContext>` is registered scoped in the t2 registration file so DI can resolve it; outside the t5 Change Tracking list, confirmed with the user at scope confirmation.
2. **`FilterQueryAsync` filter-before-sort order adjusted** — caller sorts are now applied before the filter in the expression chain (semantically equivalent for the final query); the previous chain sorted then filtered. No behavior change.

## Self-Check Results

- Type-checker: `dotnet build src/framework/MiCake.EntityFrameworkCore/MiCake.EntityFrameworkCore.csproj` — succeeded (0 warnings, 0 errors).
- Tests: `dotnet test src/tests/MiCake.EntityFrameworkCore.Tests/MiCake.EntityFrameworkCore.Tests.csproj` — 239/239 passed (23 repository tests, 0 failures).
- Regression: `MiCake.Tests` 260/260 passed.
- Note: `MiCake.IntegrationTests` and `samples/BaseMiCakeApplication` still reference removed repository APIs (`SaveChangesAsync`, `AddAndReturnAsync`); migrating them is part of t6.

## Open TODOs

- t6: migrate `MiCake.IntegrationTests` and the sample off removed repository APIs; migrate `MiCakeDbContext`/README to the DI interceptor overload.
- t7: relational acceptance must re-verify DeleteById lifecycle, physical delete rollback, and paging determinism against file-backed SQLite (unit coverage is SQLite-based in `EFRepositoryContractTests`).
- `mvt-review`: independently verify this artifact's compliance claims.

## Change Tracking

- Plan: `.ai-agents/workspace/artifacts/20260806-uow-transaction-reliability/plan.yaml`
- Task: `t5-repository-contracts` — implemented; status update deferred to `/mvt-update-plan`.
- Acceptance status: all 6 acceptance criteria covered: repository mutations never save independently (APIs removed, guard covers direct saves), DeleteById lifecycle equivalence (handler-observable test), physical delete explicit/transaction-bound/rollback-safe (SQLite), detached stale replacement raises concurrency conflict (SQLite), paging appends missing primary keys and rejects keyless entities (InMemory), MiCake + EF Core builds cleanly.

### Deliverables

#### Public Interface

Consumed by t6 (sample/README migration) and t7 (relational acceptance).

- `IRepository<TAggregateRoot, TKey>` — now `AddAsync`, `UpdateAsync`, `DeleteAsync`, `DeleteByIdAsync` only. Removed: `AddAndReturnAsync` (use `AddAsync` + `IUnitOfWork.FlushAsync` for generated keys), `SaveChangesAsync`, `ClearChangeTrackingAsync` (persistence owned by the UoW).
- `IEFCorePhysicalOperationExecutor<TDbContext>` (public, scoped) — `Task<int> ExecuteDeleteAsync<TEntity>(Expression<Func<TEntity, bool>> predicate, CancellationToken = default)`; requires an ambient writable UoW; bypasses aggregate loading, audit mutation, soft deletion, and domain events; participates in the UoW transaction.
- `EFRepositoryBase` protected surface unchanged (`DbContext`, `DbSet`, `Entities`, `EntitiesNoTracking`, `GetDbContextAsync`, `GetDbSetAsync`) — now delegates directly to the frame-stable factory (no per-repository cache).

#### Data Shapes

- `DeleteByIdAsync` — loads via `FindAsync(id)`; tracked `DeleteAsync` when found; no-op when absent.
- `UpdateAsync` — full detached replacement via `DbSet.Update`; concurrency tokens preserved; `DbUpdateConcurrencyException` surfaces at UoW flush/commit.
- Paging total order — primary-key properties from EF model metadata (`FindPrimaryKey().Properties`), ascending; caller-sorted keys keep their direction and are not duplicated; keyless entity types throw `InvalidOperationException` with a diagnostic.

#### Usage Constraints

- Repository mutations never save; callers use `IUnitOfWork.FlushAsync` (generated keys) or `CommitAsync`.
- Direct `DbContext.SaveChangesAsync` remains guarded by the t3 write pipeline (requires ambient writable UoW).
- Physical delete requires `IUnitOfWorkManager.BeginAsync()`; read-only and no-UoW writes are rejected before SQL execution.
- t6: sample controllers must migrate `SaveChangesAsync`/`AddAndReturnAsync` to the UoW contract; migration guidance must name replacements per R34.
- t7: re-verify DeleteById lifecycle, physical delete rollback, and paging determinism on file-backed SQLite; unit coverage already runs on SQLite.
