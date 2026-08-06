# MiCake Framework Issues Report

> Prepared for submitting GitHub issues to [MiCake/MiCake](https://github.com/MiCake/MiCake).
> Analysis is based on the `master` branch source at `src/framework` and live
> reproduction against MiCake **10.0.0** in a production-style ASP.NET Core 10
> application using PostgreSQL and EF Core 10.

---

## Table of Contents

1. [Environment](#environment)
2. [Issue 1 (confirmed, reproduced): Race condition in `LazyEFSaveChangesLifetime` singleton state](#issue-1-confirmed-reproduced-race-condition-in-lazyefsavechangeslifetime-singleton-state)
3. [Issue 2 (design flaw, latent): Repository `DbContext` property creates a fresh context on every access when no UoW is active](#issue-2-design-flaw-latent-repository-dbcontext-property-creates-a-fresh-context-on-every-access-when-no-uow-is-active)
4. [Issue 3 (confirmed by code inspection): Pre-save scope leak in `LazyEFSaveChangesLifetime.ExecuteWithScopeAsync`](#issue-3-confirmed-by-code-inspection-pre-save-scope-leak-in-lazyefsavechangeslifetime-executewithscopeasync)
5. [Appendix A: `new` method hiding pitfall (API design concern, not a bug)](#appendix-a-new-method-hiding-pitfall-api-design-concern-not-a-bug)
6. [Appendix B: Suggested fixes](#appendix-b-suggested-fixes)

---

## Environment

| Item | Value |
|------|-------|
| MiCake version | `10.0.0` (also reproduced on `10.0.0-preview.11`) |
| Framework | .NET 10 (ASP.NET Core 10.0.7) |
| EF Core | Microsoft.EntityFrameworkCore 10.x, Npgsql provider |
| Database | PostgreSQL |
| MiCake options | `EFCoreConfig.BypassUnitOfWorkCheck = true` (auto-UoW per request enabled) |
| Hosting | ASP.NET Core web application, scoped `DbContext` registration (`AddNpgsql<AppDbContext>`) |

---

## Issue 1 (confirmed, reproduced): Race condition in `LazyEFSaveChangesLifetime` singleton state

### Severity

High — causes intermittent `ObjectDisposedException` (HTTP 500) on concurrent
write requests in production; also makes the xUnit test suite non-deterministic.

### Background

MiCake's EF Core interceptor (`MiCakeEFCoreInterceptor`) invokes pre/post-save
handlers through `IEFSaveChangesLifetime`. The implementation,
`LazyEFSaveChangesLifetime`, is registered as a **singleton** (per the project
comment: "A singleton wrapper for IEFSaveChangesLifetime that safely resolves
scoped services"), but it keeps **per-request state in an instance field**:

File: `src/framework/MiCake.EntityFrameworkCore/Internal/LazyEFSaveChangesLifetime.cs`

```csharp
internal class LazyEFSaveChangesLifetime : IEFSaveChangesLifetime
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private IServiceScope? _currentScope;      // <-- instance field, shared across ALL requests

    public async Task BeforeSaveChangesAsync(...)
    {
        await ExecuteWithScopeAsync(..., isPreSave: true, ...);
    }

    public async Task AfterSaveChangesAsync(...)
    {
        await ExecuteWithScopeAsync(..., isPreSave: false, ...);
    }

    private async Task ExecuteWithScopeAsync(...)
    {
        ...
        IServiceScope scope;
        if (isPreSave)
        {
            _currentScope = _serviceScopeFactory.CreateScope();   // <-- WRITE shared state
            scope = _currentScope;
        }
        else
        {
            scope = _currentScope ?? _serviceScopeFactory.CreateScope();  // <-- READ shared state
        }

        try
        {
            await processor(entries, scope.ServiceProvider, cancellationToken);
        }
        finally
        {
            if (!isPreSave)
            {
                scope.Dispose();
                _currentScope = null;      // <-- WRITE shared state
            }
        }
    }
}
```

### Root cause

The singleton instance's `_currentScope` field is written in `BeforeSaveChanges`
and read/cleared in `AfterSaveChanges`. Under concurrency, two requests can
interleave as follows:

1. Request A `BeforeSaveChanges` → `_currentScope = scopeA`
2. Request B `BeforeSaveChanges` → `_currentScope = scopeB` (overwrites A)
3. Request A `AfterSaveChanges` → reads `scopeB` (or `null`) instead of `scopeA`
4. Request A's `AfterSaveChanges` disposes `scopeB` and sets `_currentScope = null`
5. Request B `AfterSaveChanges` → `_currentScope` is `null` → creates a new scope, **or** reads an already-disposed scope → `ObjectDisposedException`

Because `_currentScope` is never synchronized (no lock, not `AsyncLocal`, not a
scoped service), every concurrent write request pair is exposed to this race.
A `Task` interleaving at any `await` point in the handler pipeline makes the
failure probabilistic.

### Reproduction (verified in our project)

Our application registers repositories via MiCake and runs concurrent write
requests. The simplest deterministic reproduction is running the integration
test suite **without serializing test collections** (xUnit parallelizes test
classes by default):

```bash
# in a solution using MiCake EFCore with real repositories + EF interceptor
dotnet test tests/PandaQ.Web.IntegrationTests --no-build
```

Observed failure (intermittent; typically appears within 2–3 runs):

```
System.ObjectDisposedException : Cannot access a disposed object.
Object name: 'IServiceProvider'.
   at Microsoft.Extensions.DependencyInjection.ServiceLookup.ThrowHelper.ThrowObjectDisposedException()
   at Microsoft.Extensions.DependencyInjection.ServiceProvider.CreateScope()
   at MiCake.EntityFrameworkCore.Internal.LazyEFSaveChangesLifetime.ExecuteWithScopeAsync(IEnumerable`1 entityEntries, Func`4 processor, Boolean isPreSave, CancellationToken cancellationToken)
   at MiCake.EntityFrameworkCore.Internal.LazyEFSaveChangesLifetime.BeforeSaveChangesAsync(IEnumerable`1 entityEntries, CancellationToken cancellationToken)
   at MiCake.EntityFrameworkCore.Internal.MiCakeEFCoreInterceptor.SavingChangesAsync(DbContextEventData eventData, InterceptionResult`1 result, CancellationToken cancellationToken)
   at Microsoft.EntityFrameworkCore.DbContext.SaveChangesAsync(Boolean acceptAllChangesOnSuccess, CancellationToken cancellationToken)
```

In our app this surfaces as `HTTP 500` on endpoints that write (POST/PUT/DELETE)
when two requests overlap. The framework also logs
`"Rolled back Unit of Work ... for action CreateScript due to exception"`.

> Note: the failure is **not** limited to tests — it is a production race.
> Serializing test collections only hides it in CI.

### Expected vs. actual

| | Expected | Actual |
|---|----------|--------|
| Concurrent requests | Each request's pre/post-save handlers run in their own scope | Scopes are shared/mixed between requests; disposed-scope access throws |
| Save pipeline | Interceptor completes reliably | Intermittent `ObjectDisposedException` → 500 |

### Impact

- Any MiCake application with concurrent write traffic can throw 500s.
- Interceptor handlers (`IRepositoryPreSaveChanges`/`IRepositoryPostSaveChanges`)
  receive the wrong service provider, so audit hooks may write to a disposed
  scope or leak state across requests.

---

## Issue 2 (design flaw, latent): Repository `DbContext` property creates a fresh context on every access when no UoW is active

### Severity

Medium (latent) — not triggered in our setup today, but a correctness hazard
that depends on DI lifetime configuration.

### Background

`EFRepositoryBase.DbContext` (and `DbSet`, `Entities`, `EntitiesNoTracking`)
resolve the context through a per-UoW cache. When **no Unit of Work is active**,
the cache is bypassed and a brand-new context is created on **every property
access**:

File: `src/framework/MiCake.EntityFrameworkCore/Repository/EFRepositoryBase.cs`

```csharp
private CacheContext GetOrCreateCacheContext()
{
    var currentUow = Dependencies.UnitOfWorkManager.Current;

    // If no active UoW, create a temporary cache without storing it
    if (currentUow == null)
    {
        var tempCache = CreateCacheContext(Guid.Empty);
        return tempCache;                 // <-- new DbContext + new DbSet every access
    }
    ...
}
```

`CreateCacheContext` calls `Dependencies.ContextFactory.GetDbContext()`, which
resolves `TDbContext` from DI:

File: `src/framework/MiCake.EntityFrameworkCore/Uow/EFCoreContextFactory.cs`

```csharp
public EFCoreDbContextWrapper GetDbContextWrapper()
{
    var currentUow = _unitOfWorkManager.Current;
    if (currentUow == null)
    {
        if (!_efCoreOptions.BypassUnitOfWorkCheck) { throw ...; }
        return CreateStandaloneWrapper();     // <-- _serviceProvider.GetRequiredService<TDbContext>()
    }
    return CreateUoWBoundWrapper(currentUow);
}
```

### Why it is dangerous

`CreateStandaloneWrapper` uses `_serviceProvider.GetRequiredService<TDbContext>()`.
If `TDbContext` is registered as **scoped** (the common case, and ours), the DI
container returns the *same* instance within one scope, so multiple property
accesses happen to share one context — this is why the flaw is hidden in
practice (we verified with a diagnostic test: `injected == c1 == c2` are all
`ReferenceEquals`).

However, the code **does not rely on scoped DI caching** — it explicitly creates
a new wrapper per access. The moment any of the following is true, the
repository silently operates on **different DbContext instances** between
`Add` and `SaveChanges`, causing lost writes:

- `TDbContext` registered as `Transient` (e.g. `AddDbContext` without default
  scoped lifetime, or `AddDbContextPool` where pooled instances can differ),
- A repository instance created from a root/other scope and used outside a UoW,
- `BypassUnitOfWorkCheck = true` combined with any context factory that does
  not cache per scope.

The intent of the comment ("per-repository instance cache of DbContext/DbSet
per Unit of Work ID") shows the cache should also cover the no-UoW case (e.g. a
single fallback entry), rather than allocating per access.

### Reproduction

Not reproduced in our app (scoped DI masks it). Minimal repro sketch:

```csharp
// Register DbContext as Transient
services.AddDbContext<AppDbContext>(o => o.UseSqlServer(...), ServiceLifetime.Transient);

// Access the same repository outside a UoW (or with BypassUnitOfWorkCheck = true)
var repo = scope.ServiceProvider.GetRequiredService<IMyRepo>();
var dbc1 = repo.DbContextPropertyAccess();  // instance #1
var dbc2 = repo.DbContextPropertyAccess();  // instance #2 (different!)
// dbc1 != dbc2
```

### Expected vs. actual

| | Expected | Actual |
|---|----------|--------|
| Two accesses of `DbContext` on the same repository (no UoW) | Same instance within the same scope | New instance per access (only masked by scoped DI caching) |

---

## Issue 3 (confirmed by code inspection): Pre-save scope leak in `LazyEFSaveChangesLifetime.ExecuteWithScopeAsync`

### Severity

Low–Medium — resource leak and stale-state hazard on the pre-save exception path.

### Background

In `ExecuteWithScopeAsync`, the scope is disposed **only on the post-save path**:

```csharp
try
{
    await processor(entries, scope.ServiceProvider, cancellationToken);
}
finally
{
    if (!isPreSave)
    {
        scope.Dispose();       // <-- pre-save path never disposes, never clears _currentScope
        _currentScope = null;
    }
}
```

If a pre-save handler (`IRepositoryPreSaveChanges`) throws, the branch executes
with `isPreSave == true`, so:
- the created scope is **never disposed** (leak),
- `_currentScope` **keeps the dead scope** for the lifetime of the singleton,
  so the next post-save reuses a stale scope and may re-enter disposed-object
  territory (compounding Issue 1).

### Reproduction

```csharp
public class ThrowingPreSaveHandler : IRepositoryPreSaveChanges
{
    public Task PreSaveChangesAsync(RepositoryEntityStates state, object entity, CancellationToken ct)
        => throw new InvalidOperationException("boom");
}

// Register handler, then call SaveChangesAsync twice:
// 1st call: exception propagates; _currentScope is left set and undisposed.
// 2nd call (any request): AfterSaveChanges reads the stale scope.
```

### Expected vs. actual

| | Expected | Actual |
|---|----------|--------|
| Pre-save handler throws | Scope disposed, `_currentScope` cleared | Scope leaked, stale scope retained |

---

## Appendix A: `new` method hiding pitfall (API design concern, not a bug)

When a repository implementation hides a base interface method with `new`
(e.g. `public new Task<LanguageModel?> FindAsync(...)` in a subclass of
`EFRepository<...>`), calls through the repository **interface** dispatch to
the hidden method (C# interface mapping picks the most-derived signature match).
There is no compiler warning, and the base behavior silently differs depending
on whether the caller holds the concrete type or the interface.

Suggested improvement: the framework docs could explicitly warn against `new`
hiding on `IRepository` members, or `IRepository` could expose an
`FindAsync(id, Func<IQueryable<T>, IQueryable<T>>? includes)` overload so
subclasses do not need hiding at all.

---

## Appendix B: Suggested fixes

1. **`LazyEFSaveChangesLifetime`** — make the lifetime **scoped** (it is only
   consumed by the interceptor, which can resolve it per scope), or replace
   `_currentScope` with `AsyncLocal<IServiceScope>` so state is per
   async-flow, not per singleton.
2. **Pre-save scope leak** — dispose/clear `_currentScope` in a `finally` on
   both branches, or track pre/post scope pairs explicitly.
3. **`EFRepositoryBase` no-UoW cache** — add a single fallback cache entry for
   the `currentUow == null` case (e.g. keyed by scope id) so repeated property
   access returns the same context within a scope.
4. Consider adding a concurrency test to the MiCake test suite:
   `Task.WhenAll` over several `SaveChangesAsync` calls with the real
   interceptor, asserting no `ObjectDisposedException`.

---

*Prepared by the PandaQ team — reproduction logs and stack traces captured
against MiCake 10.0.0, 2026-08-06. Happy to provide the full repro repository
or a minimal sample on request.*
