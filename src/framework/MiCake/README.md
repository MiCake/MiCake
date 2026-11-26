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

### Choosing an Execution Mode

| Intent | Ambient UoW at call site | Entry |
|--------|--------------------------|-------|
| Own a transaction boundary (host edge, long-running flow) | any | `BeginAsync` (a nested `BeginAsync` shares the root when an ambient UoW exists) |
| Isolated committed write block | unknown — entry-agnostic service | `ExecuteIsolatedAsync` (recommended default) |
| Isolated committed write block | always present; misuse must fail fast | `ExecuteRequiresNewAsync` (strict — throws without an ambient UoW) |
| Isolated committed write block | never present; misuse must fail fast | `IStandaloneUnitOfWorkExecutor` (strict — throws with an ambient UoW) |

The two strict entries are wiring self-check variants of `ExecuteIsolatedAsync`: their preconditions turn
misuse into an immediate, diagnosable failure.

### Context-Agnostic Isolated Execution (`ExecuteIsolatedAsync`)

`ExecuteIsolatedAsync` runs the callback in a fully isolated DI scope with its own root unit of work
regardless of any ambient unit of work. With an ambient UoW it is suspended and restored on every exit
path (success, failure, cancellation, commit failure, rollback failure); without one, restoration is a
no-op. It commits on success and rolls back on failure — the recommended default entry for isolated
write blocks in any host:

```csharp
await uowManager.ExecuteIsolatedAsync(async (provider, ct) =>
{
    var repo = provider.GetRequiredService<IRepository<Order, Guid>>();
    await repo.AddAsync(new Order(...), ct);
    // Commits on success, rolls back on failure
}, options: null, cancellationToken: ct);
```

### Isolated Execution (`requiresNew`, strict)

Use callback-based APIs instead of the removed `requiresNew` boolean overloads. `ExecuteRequiresNewAsync` runs the callback in a fully isolated DI scope with its own root unit of work, and restores the previous ambient unit of work afterwards. As a wiring self-check it requires an ambient UoW — use `ExecuteIsolatedAsync` when the same call site must also run without one:

```csharp
await uowManager.ExecuteRequiresNewAsync(async (provider, ct) =>
{
    var repo = provider.GetRequiredService<IRepository<Order, Guid>>();
    await repo.AddAsync(new Order(...), ct);
    // The inner unit of work commits on success and rolls back on failure
}, options: null, cancellationToken: ct);
```

`ExecuteRequiresNewAsync` requires a live outer unit of work; without one it throws `InvalidOperationException` naming the remediation paths (`BeginAsync` boundary frame, `ExecuteIsolatedAsync`, or `IStandaloneUnitOfWorkExecutor`).

### Standalone Execution (strict)

`IStandaloneUnitOfWorkExecutor` executes an operation without any ambient unit of work, in its own DI scope with a root writable unit of work. As a wiring self-check it rejects an existing ambient unit of work to keep its contract unambiguous — use `ExecuteIsolatedAsync` when the same call site must also run with one:

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
