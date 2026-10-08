---
id: 'implement-output'
version: '1.0'
skill: 'mvt-implement'
---

# Implementation: Context-Agnostic Isolated UoW Execution

## Task: t1-isolated-execution-api — Deliver ExecuteIsolatedAsync API and shared isolated core

## Implementation Summary

Delivered the context-agnostic isolated execution entry `ExecuteIsolatedAsync` (two overloads) on `IUnitOfWorkManager` per design ADR-012: with an ambient UoW it dispatches to the existing requiresNew core (suspend/restore), without one it runs the standalone root path through a shared start entry newly added to `IsolatedUowExecution` (`ExecuteStandaloneAsync`), so both isolated boundaries run a single implementation of scope creation, frame ownership, commit/rollback/dispose, and boundary-exception composition. Both strict contracts are preserved byte-for-byte as wiring self-checks (ADR-013); the missing-ambient `InvalidOperationException` now names all three remediation paths and the standalone rejection message mentions `ExecuteIsolatedAsync` (ADR-014); XML docs of the three execution entries cross-reference each other via `seealso`. The in-repo `IUnitOfWorkManager` test fake gained the two new members in the same change so the solution stays compiling. This fulfils plan task `t1-isolated-execution-api` (analysis R1-R13 relevant slice).

## Files Touched

| Path | Action | Intent |
|------|--------|--------|
| src/framework/MiCake/DDD/Uow/Internal/IsolatedUowExecution.cs | modify | Add shared standalone start entry `ExecuteStandaloneAsync` (isolated scope -> scoped manager `BeginAsync` -> existing `ExecuteAsync` core) |
| src/framework/MiCake/DDD/Uow/Internal/StandaloneUnitOfWorkExecutor.cs | modify | Keep strict ambient rejection (message now names `ExecuteIsolatedAsync`), delegate start path to the shared entry |
| src/framework/MiCake/DDD/Uow/IUnitOfWorkManager.cs | modify | Add `ExecuteIsolatedAsync` + `ExecuteIsolatedAsync<TResult>` with semantics docs; `seealso` cross-links across entries |
| src/framework/MiCake/DDD/Uow/IStandaloneUnitOfWorkExecutor.cs | modify | `seealso` cross-links and wiring self-check positioning in the type summary |
| src/framework/MiCake/DDD/Uow/Internal/UnitOfWorkManager.cs | modify | Sync-segment dispatch (`ExecuteIsolatedCoreAsync`); missing-ambient message names the three remediation paths |
| src/tests/MiCake.IntegrationTests/Filters/FilterOrderIntegrationTests.cs | modify | `TestMockUnitOfWorkManager` implements the two new members |

## Design Compliance

| Check | Result | Reason |
|-------|--------|--------|
| Files touched == Change Tracking | passed | 6/6 files exactly match the t1 slice of design Change Tracking; no extras |
| Module/layer placement | passed | All framework edits inside `MiCake` package `DDD/Uow` (+ `Internal`); test edit in `src/tests`; no layer boundary crossed |
| Public interfaces match Key Interfaces | passed | `ExecuteIsolatedAsync` and `ExecuteIsolatedAsync<TResult>` signatures match the design's Key Interfaces verbatim (verified by grep and clean compile) |
| Forbidden cross-layer imports | passed | No new imports beyond `Microsoft.Extensions.DependencyInjection` inside `DDD/Uow/Internal` (established pattern) |
| Error handling only at boundaries | passed | No new try/catch; failure composition reused from `IsolatedUowExecution` unchanged |
| No new external dependencies | passed | No manifest changes; pre-existing NU1903 package advisories unrelated |

## Deviations from Design

None.

## Self-Check Results

- Type-checker (dotnet build `MiCake.All.sln`): pass, 0 errors, 0 new warnings (4 pre-existing NU1903 NuGet advisories for `SQLitePCLRaw.lib.e_sqlite3` in test projects, unrelated to this change). XML `cref` references resolve without warnings.
- Targeted behavior-preservation tests: `dotnet test src/tests/MiCake.Tests --filter "FullyQualifiedName~Uow"` -> 119/119 passed (includes `ExecuteRequiresNewAsync_WithoutOuterUow_ShouldThrow` and the standalone rejection tests, unchanged).
- Test-fake conformance: `dotnet test src/tests/MiCake.IntegrationTests --filter "FullyQualifiedName~FilterOrder"` -> 4/4 passed.
- Full-suite run and the new ambient x outcome matrix are deferred to task `t3-execution-test-matrix` / `t4-final-verification-review` (not claimed as tested here).

### Deliverables

#### Public Interface

```csharp
// IUnitOfWorkManager (src/framework/MiCake/DDD/Uow/IUnitOfWorkManager.cs)
Task ExecuteIsolatedAsync(
    Func<IServiceProvider, CancellationToken, Task> operation,
    UnitOfWorkOptions? options = null,
    CancellationToken cancellationToken = default);

Task<TResult> ExecuteIsolatedAsync<TResult>(
    Func<IServiceProvider, CancellationToken, Task<TResult>> operation,
    UnitOfWorkOptions? options = null,
    CancellationToken cancellationToken = default);
```

Semantics contract downstream tasks must document and test: the callback runs in a fully isolated DI scope with its own root unit of work; with an ambient UoW the outer frame is suspended and restored on every exit path (success, failure, cancellation, commit failure, rollback failure); without one, restoration is a no-op; success commits, failure rolls back preserving the original exception unless rollback also fails (`UnitOfWorkBoundaryException` composition); `PartialUnitOfWorkCommitException` stays terminal; resolving `IUnitOfWorkManager` from the callback provider and reading `Current` returns the inner unit of work.

#### Data Shapes

No new types. `UnitOfWorkOptions` (`IsReadOnly`, `IsolationLevel`, `InitializationMode`) applies to the inner unit of work identically to `ExecuteRequiresNewAsync`. Failure surfaces: `InvalidOperationException` (strict-pair precondition violations), `UnitOfWorkBoundaryException` (boundary composition), `PartialUnitOfWorkCommitException` (terminal partial commit).

#### Usage Constraints

- Frame push/pop must stay in the synchronous segment of frame-owning methods (AsyncLocal constraint); host-boundary docs (t2) must state this.
- The strict pair is unchanged and positioned as wiring self-checks: `ExecuteRequiresNewAsync` still requires an ambient UoW, `IStandaloneUnitOfWorkExecutor` still rejects one; tests asserting this must not be modified.
- Missing-ambient message names the three remediation paths (`BeginAsync` boundary frame / `ExecuteIsolatedAsync` / `IStandaloneUnitOfWorkExecutor`); standalone rejection message mentions `ExecuteIsolatedAsync`. Docs and tests may reference these strings.
- Boundary-exception `operationName` is `"requiresNew"` on the ambient path, `"isolated"` on the no-ambient path of `ExecuteIsolatedAsync`, and `"standalone"` for the strict executor.

## Open TODOs

- Unit and integration test matrix for `ExecuteIsolatedAsync` (ambient x outcome; SQLite rows) — task `t3-execution-test-matrix`, `/mvt-test`.
- README decision table, non-HTTP host guide, migration note — task `t2-comprehension-guardrail-docs`, `/mvt-implement`.
- Full solution verification and R1-R19 traceability review — task `t4-final-verification-review`, `/mvt-review`.

## Change Tracking

- Plan task `t1-isolated-execution-api` (change `20261008-uow-context-agnostic-execution`): implementation complete, pending status transition via `/mvt-update-plan`.

## Task: t2-comprehension-guardrail-docs — Ship decision table and non-HTTP host guide

## Implementation Summary

Shipped the comprehension guardrails (ADR-015, analysis R14-R16) as pure documentation: the package README gained a four-quadrant "Choosing an Execution Mode" table and a `ExecuteIsolatedAsync` section while both strict entries are repositioned as wiring self-check variants; a new `docs/Background-Jobs-and-Non-HTTP-Hosts.md` guide documents two Hangfire reference patterns (async job base class preferred, `IServerFilter` mirroring `UnitOfWorkFilter`), the entry-agnostic write style, the AsyncLocal synchronous-segment constraint, and opt-out/cancellation/retry semantics; `docs/Breaking-Changes-and-Migration-v2.md` gained a v11 preview addendum covering the two added `IUnitOfWorkManager` members (source-breaking for interface implementers, non-breaking for consumers), a migration checklist item, and a new-API reference row. This fulfils plan task `t2-comprehension-guardrail-docs`.

## Files Touched

| Path | Action | Intent |
|------|--------|--------|
| src/framework/MiCake/README.md | modify | Add decision table + `ExecuteIsolatedAsync` section; reposition strict pair as wiring self-checks; name remediation paths in the requiresNew note |
| docs/Background-Jobs-and-Non-HTTP-Hosts.md | create | Boundary guide: Hangfire patterns A/B, entry-agnostic helper, sync-segment constraint, opt-out/cancel/retry, verification checklist |
| docs/Breaking-Changes-and-Migration-v2.md | modify | v11 preview addendum (2 members + implementer guidance), checklist item, API reference row (zh-CN to match the file) |

## Design Compliance

| Check | Result | Reason |
|-------|--------|--------|
| Files touched == Change Tracking | passed | 3/3 files exactly match the t2 slice of design File Structure (README modify, guide create, Breaking-Changes modify) |
| Module/layer placement | passed | Documentation-only; no code or project file touched |
| Public interfaces match Key Interfaces | passed | All code snippets reproduce the delivered `ExecuteIsolatedAsync` signatures and semantics verbatim |
| Forbidden cross-layer imports | not-applicable | No code written |
| Error handling only at boundaries | not-applicable | No code written |
| No new external dependencies | passed | Hangfire appears in documentation snippets only; no package reference added |

## Deviations from Design

None. Language note resolved as planned: the new guide and README additions follow `document_output_language` (en-US); the Breaking-Changes addendum matches that file's existing zh-CN.

## Self-Check Results

- Type-checker: not required (documentation-only change; repository has no documentation tests; snippets do not participate in compilation).
- Plan acceptance for t2 verified line-by-line against the written content (four-quadrant table covers all four modes; both Hangfire patterns plus the sync-segment note present; migration addendum documents both added members with implementer guidance; strict pair positioned as wiring self-checks).

### Deliverables

#### Public Interface

No exported symbols change. Documentation surfaces downstream (t4) must verify:

- `src/framework/MiCake/README.md` — "Choosing an Execution Mode" four-quadrant table, "Context-Agnostic Isolated Execution (`ExecuteIsolatedAsync`)" section, strict pair positioned as wiring self-check variants.
- `docs/Background-Jobs-and-Non-HTTP-Hosts.md` — Hangfire Pattern A (async job base class) and Pattern B (`IServerFilter`), entry-agnostic helper, sync-segment constraint, opt-out/cancellation/retry, verification checklist.
- `docs/Breaking-Changes-and-Migration-v2.md` — v11 preview addendum (two added `IUnitOfWorkManager` members), migration checklist item, new-API reference row.

#### Data Shapes

None. Code snippets in the guide and README reproduce the `ExecuteIsolatedAsync` signatures and semantics delivered by `t1-isolated-execution-api` (see its Deliverables); t4 may diff them against `Key Interfaces` for R14-R16 evidence.

#### Usage Constraints

- Language conventions: guide and README additions are en-US (`document_output_language`); the Breaking-Changes addendum matches that file's zh-CN.
- The strict pair must remain worded as wiring self-check variants of `ExecuteIsolatedAsync`; the migration addendum must preserve the source-breaking (implementers) / non-breaking (consumers) split.
- The guide must retain the AsyncLocal synchronous-segment constraint and the opt-out/cancellation/retry sections — they are acceptance evidence for R15.

## Open TODOs

- Test matrix for `ExecuteIsolatedAsync` — task `t3-execution-test-matrix`, `/mvt-test`.
- Full solution verification and R1-R19 traceability review — task `t4-final-verification-review`, `/mvt-review`.
- Review suggestion S1 from the t1 review (extend `IsolatedUowExecution` class summary to mention the standalone start entry) — small doc polish, unassigned.

## Change Tracking

- Plan task `t2-comprehension-guardrail-docs` (change `20261008-uow-context-agnostic-execution`): implementation complete, pending status transition via `/mvt-update-plan`.
