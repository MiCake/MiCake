# MiCake.EntityFrameworkCore

Entity Framework Core integration for the MiCake DDD toolkit.

## Overview

`MiCake.EntityFrameworkCore` provides EF Core support for MiCake:

- **MiCakeDbContext** - Enhanced DbContext with DDD support
- **Repository Implementation** - Auto-generated repositories
- **Unit of Work** - Transaction management with EF Core
- **Store Conventions** - Automatic entity configuration
- **Domain Event Dispatch** - Events triggered on SaveChanges

## Installation

```bash
dotnet add package MiCake.EntityFrameworkCore
```

## Quick Start

```csharp
// Inherit from MiCakeDbContext
public class AppDbContext : MiCakeDbContext
{
    public DbSet<Order> Orders { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder); // Required!
        // Your configurations...
    }
}
```

## Automatic Write Pipeline (Interceptors)

The write pipeline (write guard, transaction binding, and save lifecycle) is installed
**automatically** for every DbContext registered in the container: the MiCake EF Core
module registers an `IDbContextOptionsConfiguration<TContext>` configurator
(`ConfigureDbContext`) that attaches the interceptors and the `UseMiCake()` options for
you. No interceptor-install API call is required:

```csharp
// Register the DbContext in the container — that is all.
services.AddDbContext<AppDbContext>(opt => opt.UseSqlite(connectionString));
```

- **Permissive policy**: a direct DbContext write *without* an ambient writable unit of
  work passes through with native EF Core semantics (e.g. implicit transaction).
  Guards, transaction binding, and lifecycle processing apply only when an ambient
  writable unit of work is active. MiCake never interferes with vanilla EF usage.
- **Inside a unit of work**, every write is guarded and transaction-bound: the first
  `SaveChanges`, `ExecuteUpdate`/`ExecuteDelete`, and raw SQL command executes inside the
  active unit-of-work transaction.
- Inheriting `MiCakeDbContext` is optional — plain `DbContext` types registered in the
  container get the same pipeline through the configurator. `UseMiCake()` remains the
  options-level entry for `OnConfiguring` scenarios.

## Key Features

| Feature | Description |
|---------|-------------|
| `MiCakeDbContext` | Base DbContext with DDD integration |
| `IRepository<T, TKey>` | Auto-implemented repository |
| `IEFSaveChangesLifetime` | SaveChanges lifecycle hooks |
| `IEFCorePhysicalOperationExecutor` | Explicit lifecycle-bypassing physical operations |

## Repository and Persistence Ownership

Repositories **never save or commit independently**. Mutations are tracked on the stable context bound to the ambient unit of work; the unit of work is the sole persistence owner:

```csharp
public class BookService
{
    private readonly IRepository<Book, Guid> _bookRepository;
    private readonly IUnitOfWorkManager _uowManager;

    public async Task ChangeAuthorAsync(Guid bookId, string author)
    {
        using var uow = await _uowManager.BeginAsync();

        var book = await _bookRepository.FindAsync(bookId);
        book.ChangeAuthor(author);

        // No repository save call: the unit of work commits the tracked changes
        await uow.CommitAsync();
    }
}
```

- `AddAsync` / `UpdateAsync` / `DeleteAsync` / `DeleteByIdAsync` only track changes.
- `DeleteByIdAsync` loads the aggregate into the stable unit-of-work context and uses tracked deletion, so audit, soft-delete, domain-event, and lifecycle behavior is preserved.
- Paging always produces a total order before `Skip`/`Take` by appending missing primary-key properties; keyless entities are rejected.

### Explicit Physical and Bulk Operations

Operations that bypass aggregate lifecycle behavior (loading, audit mutation, soft deletion, domain events) are explicitly named and stay **inside** the unit-of-work transaction:

```csharp
// Physical delete: bypasses aggregate lifecycle but is transaction-bound
var executor = provider.GetRequiredService<IEFCorePhysicalOperationExecutor<AppDbContext>>();
await executor.ExecuteDeleteAsync<Book>(b => b.PublishedAt < cutoff);

// EF bulk and raw SQL APIs are guarded and transaction-bound as well
await context.Books.Where(b => b.Price == 0).ExecuteUpdateAsync(s => s.SetProperty(b => b.Price, 1));
await context.Database.ExecuteSqlRawAsync("DELETE FROM \"Books\" WHERE ...");
```

## Transactional Domain Events vs. Integration Events

- **Domain events** raised by aggregates are collected and dispatched inside the unit-of-work transaction during `CommitAsync`. A domain event handler failure aborts the commit and rolls back the whole unit of work.
- **Integration events** (cross-service notifications) must be published only **after** the transaction durably commits. Publish them after `CommitAsync` succeeds, or from the `OnCommitted` hook, never inside the transaction.

## Soft Delete and Query Filters

When soft deletion is enabled, entities that implement `ISoftDeletable` get a **named** query filter, `MiCakeQueryFilterNames.SoftDelete` (`"MiCake.SoftDelete"`), that hides deleted rows. Soft deletion is off by default; enable it with the audit options:

```csharp
services.AddMiCake<AppModule>()
    .UseAudit(options => options.UseSoftDeletion = true)
    .UseEFCore<AppDbContext>()
    .Build();
```

Without it, MiCake adds no soft-delete filter, and you configure one yourself under the same name, `HasQueryFilter(MiCakeQueryFilterNames.SoftDelete, e => !e.IsDeleted)`. A filter with that name keeps working if you enable soft deletion later.

> **Warning:** the filter only hides rows. Converting a delete into a soft delete is part of the same option: without `UseSoftDeletion`, removing an entity (`Remove` followed by a save) deletes the row **permanently**. If you stay on the hand-written filter, set `IsDeleted = true` yourself instead of removing the entity.

```csharp
// Bypass only soft delete; other named filters (for example a tenant filter) still apply
var all = await context.Users.IgnoreQueryFilters([MiCakeQueryFilterNames.SoftDelete]).ToListAsync();

// Bypass every filter
var everything = await context.Users.IgnoreQueryFilters().ToListAsync();
```

### Breaking change in 11.0.0: custom filters must be named

EF Core does not allow one entity type to mix an anonymous and a named query filter, so any filter you add to a soft-deletable entity must have a name:

```csharp
// Before: anonymous filters
modelBuilder.Entity<User>().HasQueryFilter(u => !u.IsDeleted);
modelBuilder.Entity<User>().HasQueryFilter(u => u.TenantId == tenantId);

// After: named filters; drop the soft-delete line when UseSoftDeletion is enabled, MiCake adds it
modelBuilder.Entity<User>().HasQueryFilter(MiCakeQueryFilterNames.SoftDelete, u => !u.IsDeleted);
modelBuilder.Entity<User>().HasQueryFilter("App.Tenant", u => u.TenantId == tenantId);
```

What happens to an anonymous filter on a soft-deletable entity depends on where it is configured:

| Where the anonymous filter is configured | Result |
|------------------------------------------|--------|
| Before MiCake's conventions run (for example before `base.OnModelCreating`) | Model building throws `MiCakeConventionException`, naming the entity type |
| After MiCake's conventions (for example after `base.OnModelCreating`) | EF Core throws `InvalidOperationException` from your own `OnModelCreating` code |

Both placements of `base.OnModelCreating` are supported and behave the same for named filters; calling the base first is recommended. For default values and ignored properties, your own configuration wins in both placements: MiCake sets a default or ignores a property only when you have not configured it before the base call, and your later configuration replaces MiCake's.

- **Replacing the soft-delete rule**: a filter you configure with the name `MiCakeQueryFilterNames.SoftDelete` wins over MiCake's in either order. Use it only when you deliberately want different semantics; a permissive filter under that name effectively turns soft delete off for the entity.
- **Hierarchies**: EF Core accepts filters on the root entity type only. MiCake applies the filter to the root, even when the root itself is not a MiCake entity type, and derived types inherit it. If only a derived type implements `ISoftDeletable` and its root does not, model building throws `MiCakeConventionException`.
- **Custom conventions**: a convention that sets `EntityConventionContext.QueryFilter` without `EnableSoftDeletion` has no effect; MiCake logs a warning instead.

## Convention Diagnostics

Convention problems are logged under the category `MiCake.EntityFrameworkCore.Conventions`:

- a convention that throws is logged as a warning and model building continues, except for `MiCakeConventionException`, which always fails the build;
- a model built before the MiCake application has started has no convention engine; conventions, including the soft-delete filter, are skipped and a warning is logged.

Logs reach your application's log sinks when the context is registered with `AddDbContext` (or configured with `UseLoggerFactory`). A `DbContext` you create directly with no logger factory logs nowhere before MiCake starts; pass one explicitly with `modelBuilder.UseMiCakeConventions(loggerFactory)`.

`dotnet ef` normally does not start the MiCake application, so the engine is missing at design time and the message is logged at debug level. The built-in conventions do not change migrations; a custom convention that does change the schema would be skipped by the tooling.

## Migration Guide

| Removed / Changed API | Replacement |
|-----------------------|-------------|
| Anonymous `HasQueryFilter` on `ISoftDeletable` entities | Named filters, `HasQueryFilter("name", expression)`; see [Soft Delete and Query Filters](#soft-delete-and-query-filters) |
| `ModelBuilderExtensions.ClearConventionCache()` also clearing the convention engine | It now clears caches only; the engine stays configured for later model builds |
| Conventions applied only to the first `DbContext` type built in a process | Every `DbContext` type now receives MiCake conventions. In an application with several context types, the second and later ones start hiding soft-deleted rows and applying the other conventions; use `IgnoreQueryFilters([MiCakeQueryFilterNames.SoftDelete])` where deleted rows are needed |
| `IRepository.SaveChangesAsync()` | `IUnitOfWork.CommitAsync()` on the ambient unit of work |
| `IRepository.AddAndReturnAsync(...)` | `AddAsync(...)` plus `IUnitOfWork.FlushAsync()` where a generated key is required |
| `IDbContextWrapper` | `IUnitOfWorkResource` (provider integration contract) |
| `PersistenceStrategy` / `Timeout` | Removed; every writable unit of work uses explicit transactions |
| `UseMiCakeInterceptors()` / `UseMiCakeInterceptors(sp)` | Removed; interceptors are installed automatically via `ConfigureDbContext` — register the DbContext in the container and the MiCake EF Core module |
| `IEFCoreContextFactory` / `IEFCoreAnchoredContextFactory` / `GetDbContextWrapper()` | Merged into `IEFCoreContextFactory<TDbContext>` with `GetDbContext()` and `GetOrCreateWrapperFor(DbContext)`; the non-generic interface and the parameterless wrapper method were removed |

## Documentation

📚 [Full Documentation](https://micake.github.io)

## License

MIT License - see [LICENSE](https://github.com/MiCake/MiCake/blob/master/LICENSE)
