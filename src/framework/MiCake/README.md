# MiCake

A lightweight Domain-Driven Design (DDD) toolkit for .NET.

## Overview

`MiCake` is the main DDD package providing all essential components:

- **Entity & Aggregate Root** - DDD tactical pattern implementations
- **Value Objects** - Immutable value type support
- **Repository Pattern** - Data access abstraction
- **Domain Events** - Event-driven domain modeling
- **Unit of Work** - Transaction management (the sole persistence owner)
- **Audit Support** - Automatic timestamp tracking

## Installation

```bash
dotnet add package MiCake
```

## Quick Start

```csharp
// Define your aggregate root
public class Order : AggregateRoot<Guid>
{
    public string CustomerName { get; private set; }
    public decimal TotalAmount { get; private set; }
    
    public void UpdateTotal(decimal amount)
    {
        TotalAmount = amount;
        AddDomainEvent(new OrderUpdatedEvent(Id));
    }
}
```

## Key Components

| Component | Description |
|-----------|-------------|
| `Entity<TKey>` | Base class for domain entities |
| `AggregateRoot<TKey>` | Base class for aggregate roots |
| `ValueObject` | Base class for value objects |
| `IRepository<T>` | Repository interface for data access |
| `IDomainEvent` | Interface for domain events |

## Unit of Work

The unit of work is the **sole persistence owner**. Repositories track changes but never save or commit independently; persistence happens when the ambient unit of work flushes and commits.

### Basic Writable Flow

```csharp
public class BookService
{
    private readonly IRepository<Book, Guid> _bookRepository;
    private readonly IUnitOfWorkManager _uowManager;

    public async Task<Guid> CreateBookAsync(string name)
    {
        using var uow = await _uowManager.BeginAsync();

        var book = new Book(name);
        await _bookRepository.AddAsync(book);

        // Explicit flush is only required when the database generates the key.
        // If the key is application-generated, it is already available on the instance.
        await uow.FlushAsync();

        await uow.CommitAsync(); // Dispatches domain events and commits the transaction
        return book.Id;
    }
}
```

- `AddAsync` tracks the aggregate; it does not persist.
- `FlushAsync` writes tracked changes without committing and returns the affected row count.
- `CommitAsync` flushes, dispatches domain events, and commits the transaction.
- `RollbackAsync` rolls back every participating resource.

### Read-Only Unit of Work

Read-only units of work reject resource flush and write activation — every attempted write fails before a command executes:

```csharp
var uow = await _uowManager.BeginAsync(UnitOfWorkOptions.ReadOnly);
```

### Isolated Execution (`requiresNew`)

Use callback-based APIs instead of the removed `requiresNew` boolean overloads. `ExecuteRequiresNewAsync` runs the callback in a fully isolated DI scope with its own root unit of work, and restores the previous ambient unit of work afterwards:

```csharp
await uowManager.ExecuteRequiresNewAsync(async (provider, ct) =>
{
    var repo = provider.GetRequiredService<IRepository<Order, Guid>>();
    await repo.AddAsync(new Order(...), ct);
    // The inner unit of work commits on success and rolls back on failure
}, options: null, cancellationToken: ct);
```

`ExecuteRequiresNewAsync` requires a live outer unit of work.

### Standalone Execution

`IStandaloneUnitOfWorkExecutor` executes an operation without any ambient unit of work, in its own DI scope with a root writable unit of work. It rejects an existing ambient unit of work to keep its contract unambiguous:

```csharp
var executor = provider.GetRequiredService<IStandaloneUnitOfWorkExecutor>();
await executor.ExecuteAsync(async (isolatedProvider, ct) =>
{
    var repo = isolatedProvider.GetRequiredService<IRepository<Order, Guid>>();
    await repo.AddAsync(new Order(...), ct);
});
```

### Savepoints

Root units of work expose savepoint management for partial rollback:

```csharp
var name = await uow.CreateSavepointAsync("checkpoint");
await uow.RollbackToSavepointAsync(name);
await uow.ReleaseSavepointAsync(name);
```

### Multi-Resource Semantics

Resources are committed in registration order. If a resource fails, already committed resources remain committed, later resources are rolled back, and a `PartialUnitOfWorkCommitException` carries the complete per-resource outcome together with the original failures. Best-effort semantics: use a single provider per unit of work unless you accept partial commit.

## Migration Guide

The following APIs were removed or changed. Replace them as shown:

| Removed / Changed API | Replacement |
|-----------------------|-------------|
| `IRepository.AddAndReturnAsync(...)` | `IRepository.AddAsync(...)` followed by `IUnitOfWork.FlushAsync()` where a generated key is required |
| `IRepository.SaveChangesAsync()` | `IUnitOfWork.CommitAsync()` on the ambient unit of work (or `FlushAsync()` to write without committing) |
| `IRepository.ClearChangeTrackingAsync()` | Remove the instance from tracking state; the unit of work owns the change tracker |
| `IUnitOfWorkManager.BeginAsync(options, requiresNew: true)` | `IUnitOfWorkManager.ExecuteRequiresNewAsync(callback, ...)` |
| `PersistenceStrategy` | Removed. Every writable unit of work uses explicit transactions; read-only units of work are the only non-transactional mode |
| `UnitOfWorkOptions.Timeout` | Removed. It previously had no runtime effect |
| `IDbContextWrapper` | `IUnitOfWorkResource` (provider integration contract) |

## Documentation

📚 [Full Documentation](https://micake.github.io)

## License

MIT License - see [LICENSE](https://github.com/MiCake/MiCake/blob/master/LICENSE)
