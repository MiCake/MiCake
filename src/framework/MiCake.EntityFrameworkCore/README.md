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

## Migration Guide

| Removed / Changed API | Replacement |
|-----------------------|-------------|
| `IRepository.SaveChangesAsync()` | `IUnitOfWork.CommitAsync()` on the ambient unit of work |
| `IRepository.AddAndReturnAsync(...)` | `AddAsync(...)` plus `IUnitOfWork.FlushAsync()` where a generated key is required |
| `IDbContextWrapper` | `IUnitOfWorkResource` (provider integration contract) |
| `PersistenceStrategy` / `Timeout` | Removed; every writable unit of work uses explicit transactions |
| `IEFCoreContextFactory` / `IEFCoreAnchoredContextFactory` / `GetDbContextWrapper()` | Merged into `IEFCoreContextFactory<TDbContext>` with `GetDbContext()` and `GetOrCreateWrapperFor(DbContext)`; the non-generic interface and the parameterless wrapper method were removed |

## Documentation

📚 [Full Documentation](https://micake.github.io)

## License

MIT License - see [LICENSE](https://github.com/MiCake/MiCake/blob/master/LICENSE)
