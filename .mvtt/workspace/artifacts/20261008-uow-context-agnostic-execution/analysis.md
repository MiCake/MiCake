---
id: '20261008-uow-context-agnostic-execution'
source: 'conversation (two user issue reports + stakeholder-confirmed design decisions)'
status: 'analyzed'
---

# Requirements Analysis: Context-Agnostic Isolated UoW Execution

## Feature Overview

Two independent user issue reports against MiCake `11.0.0-preview.2608081400` (ASP.NET Core + Hangfire, short-transaction stepwise commits) converge on one requirement theme: MiCake's isolated unit-of-work execution primitives must be usable identically inside and outside the HTTP request pipeline. Today `ExecuteRequiresNewAsync` requires an ambient UoW while `IStandaloneUnitOfWorkExecutor.ExecuteAsync` rejects one, so an entry-agnostic write service must branch or open a throwaway anchor frame; a missing-ambient failure gives no remediation path (a confirmed Low-severity discoverability defect that caused a production incident); and non-HTTP hosts have no documented boundary pattern comparable to `UnitOfWorkFilter`. This change delivers the P0+P1 scope agreed with the stakeholder: a context-agnostic isolated execution entry with comprehension guardrails. P2 (host-adapter integration packages such as `MiCake.Hangfire`) is explicitly out of scope. Scope of this analysis covers requirement extraction only; the architecture is already designed in `design.md` (ADR-012..015, stakeholder-confirmed 2026-10-08) and this baseline must stay consistent with it.

Source: conversation only — issue report 1 (background-host boundary gap and API-semantics feedback), issue report 2 (context-agnostic isolated execution proposal, options A-D), the `/mvt-bug-detect` diagnosis, and the `/mvt-consult` comparison and comprehensibility analyses. Conversation supplements: stakeholder selections recorded under "Ambiguities & Questions".

## Actors

- **Application developer (entry-agnostic service author)**: writes once-used-in-two-hosts service methods (request handlers and background jobs) that need isolated committed write blocks; the primary beneficiary of this change.
- **HTTP request handler**: ASP.NET Core host path where `UnitOfWorkFilter` establishes an automatic ambient UoW per request (`EnableAutoUnitOfWork` default true).
- **Background job host**: Hangfire / Quartz / console / custom worker path where no ambient UoW boundary exists.
- **MiCake framework runtime**: owns ambient frames (`AmbientUnitOfWorkAccessor`, AsyncLocal), isolated DI scopes, and commit/rollback/disposal execution (`IsolatedUowExecution`).
- **Framework maintainer**: evolves public API shape, migration guidance, and documentation for the v11 preview-to-GA window.

## Requirements

### Context-Agnostic Isolated Execution (P1)

1. `IUnitOfWorkManager` must expose a context-agnostic isolated execution entry, `ExecuteIsolatedAsync`, with two overloads (void and `TResult`), running a callback in a fully isolated DI scope with its own root unit of work regardless of ambient state. Assumption: placement on `IUnitOfWorkManager` and the name `ExecuteIsolatedAsync` are stakeholder-confirmed (ADR-012).
2. With an ambient UoW present, the outer frame must be suspended and restored on every exit path (success, failure, cancellation, commit failure, rollback failure), with semantics identical to `ExecuteRequiresNewAsync`: isolated service-lifetime state, DbContext, ChangeTracker, resource set, and database transaction; the inner commit must not persist outer pending changes; the inner rollback must not alter outer tracked state.
3. With no ambient UoW, execution must behave identically to `IStandaloneUnitOfWorkExecutor.ExecuteAsync`: a root unit of work in the isolated scope with the executor owning commit, rollback, and asynchronous disposal; ambient restoration is a documented no-op.
4. The callback must resolve repositories and DbContexts from the supplied isolated provider; resolving `IUnitOfWorkManager` from that provider and reading `Current` must return the inner unit of work (flush/savepoint access), matching existing requiresNew behavior.
5. `UnitOfWorkOptions` (`IsReadOnly`, `IsolationLevel`, `InitializationMode`) must apply to the inner unit of work exactly as in existing entries.
6. Success commits; failure rolls back while preserving the original exception unless rollback also fails, in which case a `UnitOfWorkBoundaryException` composes primary and rollback causes; partial commit (`PartialUnitOfWorkCommitException`) remains terminal with no rollback attempt. Assumption: reuse of the shared `IsolatedUowExecution` behavior is mandatory, not a rewrite.
7. Ambient frame push must remain in the synchronous segment of the called methods (AsyncLocal execution-context constraint that `UnitOfWorkFilter` also relies on).

### Strict Contract Preservation (P1 guardrail)

8. `ExecuteRequiresNewAsync` must keep requiring an active outer UoW and keep throwing `InvalidOperationException` otherwise, with unchanged behavior byte-for-byte (contract stability; wiring self-check positioning).
9. `IStandaloneUnitOfWorkExecutor.ExecuteAsync` must keep rejecting an existing ambient UoW with `InvalidOperationException`.
10. The existing strict-pair regression tests (e.g. `ExecuteRequiresNewAsync_WithoutOuterUow_ShouldThrow`) must pass unchanged as the behavior-preservation proof for the whole change.

### Discoverability (P0)

11. The missing-ambient failure message must name all three remediation paths: establish a boundary frame with `BeginAsync` at the host edge, use `ExecuteIsolatedAsync` for context-agnostic isolated execution, or use `IStandaloneUnitOfWorkExecutor.ExecuteAsync` when no ambient UoW exists.
12. The standalone executor's rejection message must additionally mention `ExecuteIsolatedAsync`, and the XML documentation of all three entries must cross-reference each other (`seealso`), completing both navigation directions.
13. No new exception type is introduced for the missing-ambient failure (governing decision; see Ambiguities & Questions).

### Comprehension Guardrails (P0)

14. The package README must contain a four-quadrant decision table (boundary frame / shared nested / requiresNew isolated / standalone isolated) keyed on user intent and ambient context, and must position the strict entries as wiring self-check variants of `ExecuteIsolatedAsync`.
15. A "Background jobs and non-HTTP hosts" guide must document: two Hangfire reference implementations (async job base class preferred; `IServerFilter` mirroring `UnitOfWorkFilter`), the entry-agnostic helper shape, the AsyncLocal synchronous-segment constraint, opt-out for notification-only jobs, and cancellation/retry semantics (rollback is typically empty because write blocks are individually idempotent and committed).
16. Migration guidance must cover the interface addition: source-breaking for third-party `IUnitOfWorkManager` implementers (add or delegate two members), non-breaking for consumers, with `ExecuteIsolatedAsync` documented as the recommended default entry for isolated write blocks.

### Verification

17. Unit tests must cover the matrix ambient (present / absent) x outcome (success, operation failure, cancellation, commit failure, rollback failure) for `ExecuteIsolatedAsync`, following the existing `UnitOfWorkManagerTests` naming and assertion patterns.
18. Relational integration tests (SQLite, per ADR-010 conventions) must prove: inner commit is invisible to outer pending state; the outer ambient UoW is restored after inner completion; the no-ambient path commits and leaves the ambient empty.
19. `dotnet build MiCake.All.sln` completes with no new warnings and `dotnet test MiCake.All.sln` stays fully green (baseline 1124 tests) as the regression net for unchanged strict semantics.

### Out of Scope

- P2 host-adapter integration packages (`MiCake.Hangfire`, Quartz listeners, `BackgroundService` base classes) — deferred until the documented reference pattern is validated in real use.
- Relaxing `ExecuteRequiresNewAsync` to self-sufficient execution when no ambient exists (issue option B) — rejected by design decision ADR-012 alternatives.
- A dedicated `MissingAmbientUnitOfWorkException` type — rejected by design decision ADR-014 alternatives.
- Any change to ownership checks, nested-sharing semantics, read-only enforcement, or multi-resource best-effort commit (inherited contract from change `20260806-uow-transaction-reliability`).

## Domain Concepts

| Term | Meaning |
|---|---|
| Context-agnostic isolated execution | An isolated committed unit of work runnable from any host regardless of ambient state, through one call-site API (`ExecuteIsolatedAsync`). |
| Entry-agnostic service | A service method shared by request handlers and background job pipelines, unable to assume whether an ambient UoW exists. |
| Wiring self-check | A deliberately strict API whose precondition failure acts as an early configuration/misuse signal (`ExecuteRequiresNewAsync`, `IStandaloneUnitOfWorkExecutor`). |
| Ambient UoW | The UoW exposed via `IUnitOfWorkManager.Current` for the current asynchronous flow (AsyncLocal frame stack). |
| Suspend and restore | Pushing an inner frame over a live outer frame and deterministically popping it (token compare-and-pop) on every exit path. |
| Anchor frame | The pre-change workaround of opening a throwaway `BeginAsync(ReadOnly)` frame purely to satisfy `ExecuteRequiresNewAsync`'s precondition; eliminated by this change. |
| Isolated DI scope | A fresh service scope created for the inner unit of work; the callback must resolve all services from it. |
| Four-quadrant decision table | The documentation map choosing among boundary frame / shared nested / requiresNew isolated / standalone isolated by intent and ambient context. |
| Shared nested UoW | (inherited) A child operation delegating resources and final commit/rollback to its root UoW. |
| Standalone persistence | (inherited) An explicitly named independent commit boundary executed without an ambient UoW. |

## Business Rules

- Isolated write blocks must be expressible through one call-site API regardless of ambient context; user-side context branching and anchor frames are defects of ergonomics, not acceptable usage patterns.
- Existing primitives (`ExecuteRequiresNewAsync`, `IStandaloneUnitOfWorkExecutor`, `BeginAsync`) must not change observable behavior; this change is additive at the API level and zero-delta at the behavior level for existing entries.
- Every fail-loud precondition violation in the UoW surface must name its remediation paths in the failure message (discoverability rule, derived from the confirmed Low-severity defect and its production incident).
- No new execution API may ship without its decision-table documentation in the same change (terminology guardrail; stakeholder-confirmed comprehension requirement).
- Breaking public API changes are permitted in the preview window only with migration documentation naming replacements and impact scope (inherited posture from the prior change's assumptions).
- The UoW remains the sole persistence owner; ownership checks, read-only enforcement, and best-effort multi-resource semantics are unchanged (inherited contract).
- Ambient frame creation must occur in the synchronous segment of frame-owning methods; async boundary wrappers around frame creation are prohibited.
- Deferred P2 integration packages must not add dependencies to existing core packages; the lightweight, non-intrusive packaging principle holds.

## Ambiguities & Questions

None detected. Conversation supplements from the stakeholder resolved every contested point before this analysis; the governing decisions are recorded here so downstream phases can distinguish the established baseline from later additions:

- Scope is P0+P1 only (selected 2026-10-08 over "P1 only" and "P0+P1+P2"); documentation guardrails are a prerequisite of the API addition, not an optional follow-up.
- API shape is a context-agnostic `ExecuteIsolatedAsync` on `IUnitOfWorkManager` (ADR-012, accepted with acknowledged source-breaking impact on interface implementers).
- Both strict primitives keep their contracts unchanged and are repositioned as wiring self-checks (ADR-013).
- The missing-ambient failure stays `InvalidOperationException` with an actionable message; no dedicated exception type (ADR-014).
- Comprehension guardrails ship in the same change (ADR-015); the stakeholder's "will this make usage harder?" concern is answered by the decision table plus strict-pair repositioning.
- Non-blocking note already flagged in design: the migration doc `docs/Breaking-Changes-and-Migration-v2.md` is zh-CN while generated document output is en-US; the implementer should keep the appended migration section consistent with the file's existing language unless the maintainer directs otherwise.

## Change Tracking

- Change-id: `20261008-uow-context-agnostic-execution` — analysis complete; architecture design (`design.md`, ADR-012..015) already written in this artifact directory; planning (`/mvt-plan-dev`) is the next phase.
