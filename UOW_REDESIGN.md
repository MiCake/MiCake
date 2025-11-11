# Unit of Work Redesign - Design Document

## Overview
This document describes the redesigned Unit of Work architecture that properly separates concerns between the MiCake DDD layer and the MiCake.EntityFrameworkCore layer.

## Problems Addressed

### 1. IUnitOfWork exposes RegisterDbContext method
**Problem**: The current `IUnitOfWork` interface exposes `void RegisterDbContext(IDbContextWrapper wrapper)`, which is an implementation detail that should not be part of the public API.

**Solution**: Move DbContext registration to an internal interface `IUnitOfWorkInternal` that only the EntityFrameworkCore module uses.

### 2. ProxyRepository creates unnecessary complexity
**Problem**: ProxyRepository adds an extra layer of indirection that doesn't provide real value and makes the architecture harder to understand.

**Solution**: Remove ProxyRepository entirely. Users should either:
- Inject `IRepositoryProvider<T, K>` directly (for simple cases)
- Create custom repositories inheriting from `EFRepositoryBase` (recommended for DDD)

## New Architecture

### Layer Separation

```
┌─────────────────────────────────────────────────────────────┐
│  User Code                                                  │
│  - Custom Repositories (OrderRepository : EFRepositoryBase) │
│  - Domain Services                                          │
└────────────────┬────────────────────────────────────────────┘
                 │
┌────────────────▼────────────────────────────────────────────┐
│  MiCake Layer (DDD Abstractions)                            │
│  - IUnitOfWork (public interface)                           │
│  - IUnitOfWorkManager                                       │
│  - IRepository<T, K>                                        │
│  - IRepositoryProvider<T, K>                                │
└────────────────┬────────────────────────────────────────────┘
                 │
┌────────────────▼────────────────────────────────────────────┐
│  MiCake.EntityFrameworkCore Layer                           │
│  - IUnitOfWorkInternal (internal registration)              │
│  - EFRepositoryBase (accesses DbContext via factory)        │
│  - EFCoreContextFactory (registers with UoW internally)     │
│  - EFCoreDbContextWrapper                                   │
└─────────────────────────────────────────────────────────────┘
```

### Interface Design

#### MiCake Layer - Public Interface

```csharp
namespace MiCake.DDD.Uow
{
    /// <summary>
    /// Public Unit of Work interface for users
    /// </summary>
    public interface IUnitOfWork : IDisposable
    {
        Guid Id { get; }
        bool IsDisposed { get; }
        bool IsCompleted { get; }
        bool HasActiveTransactions { get; }
        IsolationLevel? IsolationLevel { get; }
        IUnitOfWork? Parent { get; }
        
        Task CommitAsync(CancellationToken cancellationToken = default);
        Task RollbackAsync(CancellationToken cancellationToken = default);
        Task MarkAsCompletedAsync(CancellationToken cancellationToken = default);
        
        // New: Savepoint support
        Task<string> CreateSavepointAsync(string name, CancellationToken cancellationToken = default);
        Task RollbackToSavepointAsync(string name, CancellationToken cancellationToken = default);
        Task ReleaseSavepointAsync(string name, CancellationToken cancellationToken = default);
        
        // New: Event hooks
        event EventHandler<UnitOfWorkEventArgs>? OnCommitting;
        event EventHandler<UnitOfWorkEventArgs>? OnCommitted;
        event EventHandler<UnitOfWorkEventArgs>? OnRollingBack;
        event EventHandler<UnitOfWorkEventArgs>? OnRolledBack;
    }
}
```

#### MiCake Layer - Internal Interface

```csharp
namespace MiCake.DDD.Uow.Internal
{
    /// <summary>
    /// Internal interface for framework components to register with UoW
    /// This is NOT exposed in the public API
    /// </summary>
    public interface IUnitOfWorkInternal
    {
        /// <summary>
        /// Register a resource (like DbContext) with this unit of work
        /// Only called by framework components (e.g., EFCoreContextFactory)
        /// </summary>
        void RegisterResource(IUnitOfWorkResource resource);
    }
    
    /// <summary>
    /// Generic resource interface for UoW participation
    /// </summary>
    public interface IUnitOfWorkResource : IDisposable
    {
        string ResourceIdentifier { get; }
        Task BeginTransactionAsync(IsolationLevel? isolationLevel, CancellationToken cancellationToken);
        Task CommitAsync(CancellationToken cancellationToken);
        Task RollbackAsync(CancellationToken cancellationToken);
        Task SaveChangesAsync(CancellationToken cancellationToken);
        
        // Savepoint support
        Task<string> CreateSavepointAsync(string name, CancellationToken cancellationToken);
        Task RollbackToSavepointAsync(string name, CancellationToken cancellationToken);
        Task ReleaseSavepointAsync(string name, CancellationToken cancellationToken);
    }
}
```

### How EFRepositoryBase Gets DbContext

The interaction flow:

```
┌──────────────────┐
│ EFRepositoryBase │
│  (User's code)   │
└────────┬─────────┘
         │ 1. Needs DbContext
         ▼
┌──────────────────────────┐
│ EFCoreContextFactory     │
│  .GetDbContext()         │
└────────┬─────────────────┘
         │ 2. Check current UoW
         ▼
┌──────────────────────────┐
│ IUnitOfWorkManager       │
│  .Current                │
└────────┬─────────────────┘
         │ 3. Get/Create DbContext from DI
         ▼
┌──────────────────────────┐
│ ServiceProvider          │
│  .GetService<TDbContext> │
└────────┬─────────────────┘
         │ 4. Wrap and register
         ▼
┌──────────────────────────┐
│ EFCoreDbContextWrapper   │
│  (wraps DbContext)       │
└────────┬─────────────────┘
         │ 5. Register internally
         ▼
┌──────────────────────────┐
│ IUnitOfWorkInternal      │
│  .RegisterResource()     │
└────────┬─────────────────┘
         │ 6. Return DbContext
         ▼
┌──────────────────────────┐
│ EFRepositoryBase         │
│  (uses DbContext)        │
└──────────────────────────┘
```

**Key Points**:

1. **EFRepositoryBase** never directly accesses `IUnitOfWork`
2. **EFCoreContextFactory** handles all UoW interaction internally
3. **Registration** happens automatically when factory creates/retrieves DbContext
4. **DbContext lifecycle** is managed by DI container (respects Scoped/Singleton lifetime)
5. **UoW** manages transaction coordination, not DbContext creation

### Savepoint Support

Savepoints allow partial rollback within a transaction:

```csharp
using (var uow = _uowManager.Begin())
{
    // Do some work
    await repository.AddAsync(entity1);
    
    // Create savepoint before risky operation
    var savepoint = await uow.CreateSavepointAsync("BeforeRiskyOp");
    
    try
    {
        // Risky operation
        await repository.AddAsync(entity2);
    }
    catch (Exception)
    {
        // Rollback to savepoint (keeps entity1)
        await uow.RollbackToSavepointAsync(savepoint);
    }
    
    // Commit everything (or just entity1 if rolled back)
    await uow.CommitAsync();
}
```

### Transaction Event Hooks

Events allow users to hook into transaction lifecycle:

```csharp
using (var uow = _uowManager.Begin())
{
    uow.OnCommitting += (sender, args) =>
    {
        // Before commit - validate state
        Console.WriteLine($"Committing UoW {args.UnitOfWorkId}");
    };
    
    uow.OnCommitted += (sender, args) =>
    {
        // After commit - clear cache, send notifications
        Console.WriteLine($"Committed UoW {args.UnitOfWorkId}");
    };
    
    uow.OnRollingBack += (sender, args) =>
    {
        // Before rollback - log
        Console.WriteLine($"Rolling back UoW {args.UnitOfWorkId}");
    };
    
    uow.OnRolledBack += (sender, args) =>
    {
        // After rollback - cleanup
        Console.WriteLine($"Rolled back UoW {args.UnitOfWorkId}");
    };
    
    // Do work...
    await uow.CommitAsync();
}
```

## Implementation Plan

### Phase 1: Internal Interface
1. Create `IUnitOfWorkInternal` interface with `RegisterResource` method
2. Create `IUnitOfWorkResource` to replace `IDbContextWrapper` dependency
3. Update `UnitOfWork` to implement both `IUnitOfWork` and `IUnitOfWorkInternal`
4. Keep old `RegisterDbContext` temporarily for backward compatibility

### Phase 2: EF Core Adaptation
1. Make `EFCoreDbContextWrapper` implement `IUnitOfWorkResource`
2. Update `EFCoreContextFactory` to use `IUnitOfWorkInternal` for registration
3. Add savepoint support to `EFCoreDbContextWrapper`

### Phase 3: Remove ProxyRepository
1. Remove `ProxyRepository` and `ProxyReadOnlyRepository` classes
2. Remove `IRepositoryFactory` interface (no longer needed)
3. Update module registration to remove proxy repository registration
4. Update documentation with migration guide

### Phase 4: Enhanced Features
1. Add savepoint methods to `IUnitOfWork`
2. Implement savepoint support in `UnitOfWork`
3. Add event hooks (OnCommitting, OnCommitted, etc.)
4. Implement event raising in `UnitOfWork`

### Phase 5: Cleanup
1. Remove old `RegisterDbContext` method from `IUnitOfWork`
2. Remove `IDbContextWrapper` from MiCake layer (move to EF Core layer)
3. Update tests

## Benefits

1. **Better Separation of Concerns**: MiCake layer doesn't know about DbContext
2. **Cleaner Public API**: No implementation details exposed
3. **More Flexible**: Easy to add other persistence layers (Dapper, ADO.NET)
4. **Simpler for Users**: No more proxy indirection
5. **Advanced Features**: Savepoints and event hooks for complex scenarios
6. **Testability**: Easier to mock and test components

## Migration Guide for Users

### Before (with ProxyRepository)
```csharp
public class OrderService
{
    private readonly IRepository<Order, int> _repository;
    
    public OrderService(IRepository<Order, int> repository)
    {
        _repository = repository;
    }
}
```

### After (recommended)
```csharp
public class OrderRepository : EFRepositoryBase<MyDbContext, Order, int>, IRepository<Order, int>
{
    public OrderRepository(IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
    }
    
    public IQueryable<Order> Query() => Entities;
    public Task<Order?> FindAsync(int id, CancellationToken ct) 
        => Entities.FirstOrDefaultAsync(o => o.Id == id, ct);
    // ... implement other methods
}

public class OrderService
{
    private readonly OrderRepository _repository;
    
    public OrderService(OrderRepository repository)
    {
        _repository = repository;
    }
}
```

### After (alternative, for simple cases)
```csharp
public class OrderService
{
    private readonly IRepositoryProvider<Order, int> _provider;
    
    public OrderService(IRepositoryProvider<Order, int> provider)
    {
        _provider = provider;
    }
    
    public async Task DoWork()
    {
        var repository = _provider.GetRepository();
        // use repository
    }
}
```
