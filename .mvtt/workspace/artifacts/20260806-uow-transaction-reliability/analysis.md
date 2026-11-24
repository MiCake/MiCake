---
id: '20260806-uow-transaction-reliability'
source: 'uow-repository-efcore-architecture-assessment.md'
status: 'analyzed'
---

# Requirements Analysis: UoW Transaction Reliability

## Feature Overview

This change must resolve every defect and design deficiency identified in `uow-repository-efcore-architecture-assessment.md` across MiCake's UoW abstractions, EF Core repository integration, save lifecycle, ASP.NET Core automatic UoW boundary, public contracts, and test coverage. The desired outcome is a predictable transaction contract in which persistence ownership, rollback behavior, nested and `requiresNew` scope semantics, lifecycle state, and explicitly non-atomic multi-resource behavior are observable and verifiable.

## Actors

- **Application developer**: Uses repositories, DbContext, UoW APIs, attributes, bulk operations, and extension points.
- **MiCake framework runtime**: Coordinates ambient UoWs, resources, persistence, transactions, lifecycle callbacks, and disposal.
- **EF Core persistence provider**: Tracks entities and executes saves, bulk commands, transactions, rollback, and savepoints.
- **Repository lifecycle handler**: Implements audit, soft-deletion, domain-event, or custom pre/post-save behavior.
- **Application host**: Owns dependency-injection scopes, DbContext lifetimes, and application startup/shutdown.
- **Framework maintainer**: Evolves public APIs and verifies transaction behavior across supported relational providers.

## Requirements

### Persistence Ownership and Transaction Boundaries

1. While an active writable UoW exists, the UoW must be the authoritative persistence boundary for framework-mediated aggregate changes.
2. Repository APIs must not make data durable independently of the active UoW. Existing immediate-save contracts may be changed or removed because breaking public API changes are allowed for this change.
3. A TransactionManaged UoW must activate its transaction before the first database write, including repository SaveChanges, identity-generating inserts, direct DbContext SaveChanges, bulk update/delete, and raw SQL paths supported by the framework.
4. For resources that have not already committed and remain protected by the same transaction, a successful rollback must leave no data from that UoW committed. A partial commit under the declared best-effort multi-resource contract is governed by Requirements 35-37 instead.
5. The default automatic UoW behavior may change from OptimizeForSingleWrite when necessary to provide predictable consistency. The resulting default and any explicitly non-transactional mode must be documented.
6. Operations intentionally bypassing aggregate lifecycle semantics must use an explicit API and disclose their immediate SQL, audit, soft-delete, domain-event, and transaction behavior.
7. `DeleteByIdAsync` must have the same aggregate lifecycle, soft-deletion, audit, and UoW-boundary semantics as normal tracked deletion, unless it is replaced by an explicitly named physical-delete API.
8. Savepoints must be created only after all participating resources required for the operation have active transactions, and all writes intended to be protected by a savepoint must occur after its creation.

### `requiresNew` and Ambient UoW Semantics

9. `requiresNew` must be fully supported rather than rejected.
10. An inner `requiresNew` UoW must isolate its service-lifetime state, DbContext, ChangeTracker, resource set, and database transaction from the outer UoW.
11. Inner commit must not persist outer pending changes, and inner rollback must not alter outer tracked state.
12. Disposing or completing the inner UoW must restore `IUnitOfWorkManager.Current` to the still-live outer UoW.
13. Repository operations executed after the inner UoW completes must again participate in the outer UoW.
14. Shared nested UoWs that do not request `requiresNew` must continue delegating resource ownership and rollback intent to their root UoW.

### Save Lifecycle and Interceptor State

15. Save lifecycle state must be isolated by DbContext and save operation; singleton-global mutable operation state is prohibited.
16. Every lifecycle scope and operation-state object must be released on success, pre-save failure, database failure, optimistic concurrency failure, cancellation, and post-save failure.
17. Concurrent SaveChanges operations must never resolve handlers from each other's scope or overwrite each other's entity collections.
18. Post-save handlers must receive the Added, Modified, or Deleted repository state captured before persistence, even after EF Core changes or detaches the corresponding entry.
19. Interceptor state must remain correct when interceptors or DbContext options are pooled, shared, or explicitly reused.
20. Rollback provider failures must propagate to the UoW caller; logging alone is not sufficient.
21. Transaction and scope resources must support asynchronous disposal where the underlying provider exposes it.
22. Interceptor configuration and lifetime state must remain isolated when multiple application or test hosts run in the same process; one host must not overwrite or clear another host's configuration.

### Read-Only and Context Identity Contracts

23. Read-only intent must be explicitly configurable and enforceable. Action-name inference may remain only as documented compatibility behavior.
24. A read-only UoW must reject or otherwise prevent every framework-mediated write path, including immediate SaveChanges and direct SQL paths covered by the framework.
25. Context identity resolution must have one authoritative result: the same logical UoW and DbContext type must resolve to the same context instance without repository-dependent divergence.
26. One logical UoW must resolve one stable DbContext identity per context type.
27. Ordinary repository writes without an active UoW must be rejected. Independent persistence must be available only through an explicitly named standalone API with a deterministic context, transaction, lifecycle, commit, rollback, and disposal contract.
28. Unsupported DbContext lifetimes must fail validation with a clear diagnostic rather than produce inconsistent tracking behavior.

### Repository and Resource Contracts

29. Detached `UpdateAsync` semantics must be explicit. Full-aggregate replacement must not be presented as a partial update, and tracked modification must be the preferred safe workflow.
30. Resource identity must use collision-safe instance identity rather than a DbContext hash-code string.
31. The unused `IDbContextWrapper` abstraction must be removed or assigned a documented, exercised responsibility.
32. Immediate transaction initialization must reliably initialize every registered DbContext type, preserve compile-time-verifiable registrations where possible, and produce a clear diagnostic when a registered context cannot be initialized.
33. Paging APIs must apply deterministic ordering before `Skip` and `Take`.
34. Public API removals and signature changes are permitted; migration guidance must identify replacements for removed repository and UoW APIs.

### Multi-Resource Consistency

35. Multi-resource commit is explicitly best-effort and must not claim distributed ACID guarantees.
36. The framework must expose and log partial-commit failure clearly, including which resources committed, failed, or remain eligible for rollback.
37. Documentation must distinguish the framework's best-effort guarantee from any cross-resource consistency pattern selected by an application, without presenting application-level coordination as an implicit framework guarantee.

### Verification and Documentation

38. Transaction, savepoint, and `requiresNew` acceptance tests must run against at least one real relational provider; EF Core InMemory tests are insufficient for these guarantees.
39. Tests must cover early SaveChanges rollback, immediate identity generation, bulk SQL behavior, direct DbContext writes, read-only enforcement, rollback failure propagation, and deterministic context identity.
40. Concurrency tests must cover simultaneous SaveChanges operations, pooled/shared interceptor configurations, and cleanup on every failure and cancellation path.
41. Existing skipped transaction and savepoint tests must be replaced, enabled against an appropriate provider, or explicitly superseded by equivalent executable tests.
42. Documentation must distinguish domain events handled inside the transaction from integration events published after durable commit. The delivery mechanism is deferred to architecture design.
43. Documentation must state the exact semantics of default, read-only, TransactionManaged, standalone, nested, `requiresNew`, bulk, and multi-resource operations.
44. The ineffective `UnitOfWorkOptions.Timeout` public option must be removed, and migration guidance must state that it previously had no runtime effect.
45. SaveChanges re-entry from lifecycle or domain-event handlers is supported only as controlled re-entry: it must remain within the same transaction boundary, avoid duplicate event dispatch and cleanup, terminate deterministically, and expose a clear failure when safe progress is impossible.
46. Acceptance tests must cover the supported composition matrix for root, shared nested, `requiresNew`, read-only, and savepoint boundaries, including restoration of the previous ambient UoW after each inner boundary completes.
47. Detached update tests must cover optimistic concurrency-token conflicts and prove that stale aggregate replacement cannot silently overwrite a detected concurrent change.
48. Breaking API changes must update affected framework documentation, tests, and `samples/BaseMiCakeApplication`; the current sample use of repository `SaveChangesAsync` must migrate to the replacement contract.
49. Transaction activation, rollback failure, partial commit, read-only write rejection, lifecycle failure, and controlled re-entry must emit structured diagnostics with enough context to correlate the UoW, resource, DbContext, and operation without exposing sensitive data.
50. The design phase must establish a repeatable performance baseline for common read and write paths. Verification must demonstrate that transaction activation, lifecycle scope isolation, and state capture do not introduce an unexplained material regression against that baseline.

## Requirement Priority and Traceability

| Source finding | Priority | Covered requirements |
|---|---|---|
| 4.1 Competing persistence owners | P0 | R1, R2, R5, R27, R34, R43, R48 |
| 4.2 Lazy transaction activation | P0 | R3, R4, R8, R38, R39, R41, R49, R50 |
| 4.3 `DeleteByIdAsync` lifecycle bypass | P0 | R6, R7, R39 |
| 4.4 `requiresNew` isolation and ambient restoration | P0 | R9-R14, R38, R39, R43, R46 |
| 4.5 Singleton lifecycle operation state | P0 | R15-R17, R19, R40, R49, R50 |
| 5.1 Read-only inference and enforcement | P1 | R23, R24, R39, R43, R46, R49 |
| 5.2 Repository-level context cache | P1 | R25, R26, R39, R50 |
| 5.3 No-UoW context identity | P1 | R27, R28, R39, R43 |
| 5.4 Best-effort multi-resource commit | P2 | R4, R35-R37, R43, R49 |
| 5.5 Detached full-aggregate update | P2 | R29, R47 |
| 5.6 Shared interceptor entry state | P2 | R19, R40 |
| 5.7 Post-save state loss | P1 | R18, R40 |
| 5.8 Suppressed rollback failure | P1 | R20, R39, R49 |
| 6.1 Hash-based resource identity | P2 | R30 |
| 6.2 Unused `IDbContextWrapper` | P2 | R31, R34 |
| 6.3 Reflection-based immediate initialization | P2 | R32 |
| 6.4 Non-deterministic paging | P2 | R33 |
| 6.5 Synchronous-only disposal | P2 | R21 |
| 6.6 Process-global interceptor factory | P2 | R22, R40 |
| 7 Domain-event semantics | P1 | R42, R45 |
| Additional verified gap: ineffective UoW timeout | P1 | R44 |

## Domain Concepts

| Term | Meaning |
|---|---|
| Persistence owner | The component authorized to make tracked changes durable while a UoW is active. |
| Logical UoW | The application-visible operation boundary represented by `IUnitOfWork`. |
| Ambient UoW | The UoW exposed through `IUnitOfWorkManager.Current` for the current asynchronous flow. |
| Shared nested UoW | A child operation that delegates resources and final commit or rollback to its root UoW. |
| `requiresNew` UoW | An independent operation with its own ambient frame, DI scope, DbContext, ChangeTracker, resources, and transaction. |
| UoW resource | A persistence participant implementing the generic prepare, activate, save, commit, rollback, savepoint, and disposal protocol. |
| Lifecycle operation state | Per-save data connecting pre-save entities and states with post-save processing and cleanup. |
| Aggregate lifecycle delete | A tracked deletion that participates in soft deletion, audit, domain events, and UoW commit. |
| Physical delete | An explicit immediate database deletion that bypasses aggregate lifecycle processing. |
| Read-only UoW | A boundary in which writes are prohibited rather than merely omitted at final commit. |
| Best-effort multi-resource commit | Sequential coordination that can report partial success but does not provide distributed atomicity. |
| Standalone persistence | An explicitly named independent commit boundary executed without an ambient UoW and with deterministic context, transaction, lifecycle, and disposal semantics. |
| Controlled SaveChanges re-entry | A lifecycle-handler save that remains in the current transaction and has deterministic event, cleanup, and termination behavior. |

## Business Rules

- An active writable UoW owns persistence; repositories must not silently create a competing commit boundary.
- No write advertised as TransactionManaged may become durable before the corresponding transaction is active.
- A successful rollback removes protected writes that have not already committed; best-effort partial commits remain observable but cannot be described as rolled back.
- Read-only is an enforced behavioral contract, not only a transaction optimization.
- `requiresNew` isolation includes both database isolation and restoration of the previous ambient UoW.
- A shared nested UoW cannot commit independently of its root and must propagate rollback intent to that root.
- Normal aggregate deletion must invoke the same lifecycle rules regardless of whether the caller supplies an entity or an identifier.
- Physical and bulk operations must be explicit when they bypass aggregate lifecycle behavior.
- Pre-save state passed to post-save handlers is immutable operation data, not a live EF Core entry state queried after persistence.
- Every lifecycle scope must have one owner and one guaranteed cleanup path.
- Rollback failure creates an uncertain outcome and must be observable by the caller.
- Multi-resource commit remains best-effort; partial commit must be reported and never described as atomic.
- Ordinary repository writes require an active UoW; independent commits require the explicit standalone boundary.
- Controlled SaveChanges re-entry must not duplicate domain-event dispatch or produce unbounded recursion.
- Breaking API changes are permitted, but removed behavior requires migration documentation.
- Real transaction guarantees require executable relational-provider tests.

## Assumptions and Constraints

- The assessment report is the authoritative scope baseline for this change.
- All Critical, Warning, and Minor findings in the report are in scope, including maintainability and documentation items.
- The implementation may change the default persistence strategy and public interfaces.
- Full `requiresNew` support is required; fail-fast rejection is not an acceptable final behavior.
- Distributed transactions are out of scope; multi-resource behavior is deliberately best-effort.
- Architecture and technology choices for satisfying these requirements are deferred to `/mvt-design`.
- The existing dependency direction from MiCake DDD/UoW abstractions toward provider-neutral contracts must be preserved.
- Correctness checks remain mandatory by default; MiCake's lightweight and non-intrusive goals do not permit silent transaction or lifecycle contract violations.

## Ambiguities & Questions

None detected after clarification. The user selected the following governing decisions:

- Breaking public API changes are allowed.
- EF Core `requiresNew` must be fully supported.
- Multi-resource consistency remains explicitly best-effort.
- The default automatic UoW strategy may change to restore predictable consistency.
- Ordinary writes without a UoW are rejected; independent commits use an explicit standalone API.
- The ineffective `UnitOfWorkOptions.Timeout` option is removed rather than implemented.
- SaveChanges from lifecycle and domain-event handlers uses a controlled re-entry contract rather than fail-fast prohibition.

## Change Tracking

- Change ID: `20260806-uow-transaction-reliability`
- Status: Requirements analyzed; ready for architecture design.
