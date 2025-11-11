# MiCake UoW Redesign - Implementation Summary

## Date: 2025-11-11

## Overview
This document summarizes the implementation of the redesigned Unit of Work architecture based on feedback from @uoyoCsharp.

## Issues Addressed

### 1. ❌ IUnitOfWork exposed RegisterDbContext - FIXED
**Problem**: Public interface exposed implementation details that should be internal to persistence layer.

**Solution**:
- Created `IUnitOfWorkInternal` interface for framework-internal operations
- Moved `RegisterResource()` to internal interface
- `RegisterDbContext()` marked as `[Obsolete]` for backward compatibility
- Only persistence layer (EF Core) uses internal interface

### 2. ❌ ProxyRepository adds unnecessary complexity - REMOVED
**Problem**: ProxyRepository adds indirection without providing real value.

**Solution**:
- Removed ProxyRepository, ProxyReadOnlyRepository completely
- Removed IRepositoryFactory, DefaultRepositoryFactory
- Users now either:
  1. Create custom repositories inheriting from EFRepositoryBase (recommended)
  2. Inject IRepositoryProvider<T, K> directly (simple cases)

### 3. ✅ Added Savepoint Support
**New Feature**: Partial rollback within transactions

**API**:
```csharp
var savepoint = await uow.CreateSavepointAsync("BeforeRiskyOp");
await uow.RollbackToSavepointAsync(savepoint);
await uow.ReleaseSavepointAsync(savepoint);
```

### 4. ✅ Added Transaction Event Hooks
**New Feature**: Hook into transaction lifecycle for cross-cutting concerns

**API**:
```csharp
uow.OnCommitting += (sender, args) => { /* before commit */ };
uow.OnCommitted += (sender, args) => { /* after commit */ };
uow.OnRollingBack += (sender, args) => { /* before rollback */ };
uow.OnRolledBack += (sender, args) => { /* after rollback */ };
```

## Architecture Changes

### Before
```
User Code
    ↓ inject IRepository<Order, int>
ProxyRepository
    ↓ uses IRepositoryFactory
DefaultRepositoryFactory  
    ↓ uses IRepositoryProvider
EFRepositoryProvider
    ↓ creates
EFRepository (actual implementation)
```

### After
```
User Code
    ↓ Custom OrderRepository : EFRepositoryBase
EFRepositoryBase
    ↓ uses IEFCoreContextFactory
EFCoreContextFactory
    ↓ gets IUnitOfWorkManager.Current
    ↓ wraps DbContext as IUnitOfWorkResource
    ↓ calls IUnitOfWorkInternal.RegisterResource()
UnitOfWork (implements both IUnitOfWork and IUnitOfWorkInternal)
```

## Files Changed

### Added
- `src/framework/MiCake/DDD/Uow/Internal/IUnitOfWorkInternal.cs` - Internal registration interface
- `src/framework/MiCake/DDD/Uow/UnitOfWorkEventArgs.cs` - Event args for hooks
- `UOW_REDESIGN.md` - Complete design documentation

### Modified
- `src/framework/MiCake/DDD/Uow/IUnitOfWork.cs` - Added savepoints, events, deprecated RegisterDbContext
- `src/framework/MiCake/DDD/Uow/Internal/UnitOfWork.cs` - Implements new features
- `src/framework/MiCake/DDD/Uow/Internal/UnitOfWorkManager.cs` - Supports new interface
- `src/framework/MiCake/Modules/MiCakeEssentialModule.cs` - Removed proxy registration

### Removed
- `src/framework/MiCake/DDD/Extensions/Internal/ProxyRepository.cs` ❌
- `src/framework/MiCake/DDD/Extensions/Internal/ProxyReadOnlyRepository.cs` ❌
- `src/framework/MiCake/DDD/Extensions/IRepositoryFactory.cs` ❌
- `src/framework/MiCake/DDD/Extensions/Internal/DefaultRepositoryFactory.cs` ❌

## Key Interfaces

### IUnitOfWork (Public - for users)
```csharp
public interface IUnitOfWork : IDisposable
{
    // Properties
    Guid Id { get; }
    bool IsCompleted { get; }
    IsolationLevel? IsolationLevel { get; }
    IUnitOfWork? Parent { get; }
    
    // Core operations
    Task CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);
    
    // Savepoints
    Task<string> CreateSavepointAsync(string name, CancellationToken cancellationToken = default);
    Task RollbackToSavepointAsync(string name, CancellationToken cancellationToken = default);
    Task ReleaseSavepointAsync(string name, CancellationToken cancellationToken = default);
    
    // Events
    event EventHandler<UnitOfWorkEventArgs>? OnCommitting;
    event EventHandler<UnitOfWorkEventArgs>? OnCommitted;
    event EventHandler<UnitOfWorkEventArgs>? OnRollingBack;
    event EventHandler<UnitOfWorkEventArgs>? OnRolledBack;
    
    // Deprecated
    [Obsolete]
    void RegisterDbContext(IDbContextWrapper wrapper);
}
```

### IUnitOfWorkInternal (Internal - for framework)
```csharp
public interface IUnitOfWorkInternal
{
    void RegisterResource(IUnitOfWorkResource resource);
}
```

### IUnitOfWorkResource (For persistence layers)
```csharp
public interface IUnitOfWorkResource : IDisposable
{
    string ResourceIdentifier { get; }
    bool HasActiveTransaction { get; }
    
    Task BeginTransactionAsync(IsolationLevel? isolationLevel, CancellationToken cancellationToken);
    Task CommitAsync(CancellationToken cancellationToken);
    Task RollbackAsync(CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
    
    // Savepoint support
    Task<string> CreateSavepointAsync(string name, CancellationToken cancellationToken);
    Task RollbackToSavepointAsync(string name, CancellationToken cancellationToken);
    Task ReleaseSavepointAsync(string name, CancellationToken cancellationToken);
}
```

## How EFRepositoryBase Gets DbContext

The complete flow (no direct IUnitOfWork access from repository):

1. **User calls repository method**
   ```csharp
   var order = await _orderRepository.FindAsync(id);
   ```

2. **EFRepositoryBase accesses DbContext property**
   ```csharp
   protected TDbContext DbContext => GetOrCreateCacheContext().DbContext;
   ```

3. **GetOrCreateCacheContext() calls factory**
   ```csharp
   dbContext = _contextFactory.GetDbContext();
   ```

4. **EFCoreContextFactory.GetDbContext()**
   ```csharp
   public TDbContext GetDbContext()
   {
       var currentUow = _unitOfWorkManager.Current; // Get active UoW
       var dbContext = _serviceProvider.GetRequiredService<TDbContext>(); // From DI
       var wrapper = new EFCoreDbContextWrapper(dbContext, ...);
       
       // Register with UoW internally
       if (currentUow is IUnitOfWorkInternal internalUow)
       {
           internalUow.RegisterResource(wrapper);
       }
       
       return dbContext;
   }
   ```

5. **UnitOfWork manages transaction**
   - Auto-starts transaction if configured
   - Tracks all registered resources
   - Commits/rolls back all resources together
   - Handles nested transactions

## Breaking Changes

### For End Users

1. **Cannot inject `IRepository<T, K>` directly anymore**
   ```csharp
   // OLD (no longer works)
   public class OrderService
   {
       private readonly IRepository<Order, int> _repository;
       public OrderService(IRepository<Order, int> repository) { }
   }
   
   // NEW Option 1: Custom repository (recommended)
   public class OrderRepository : EFRepositoryBase<MyDbContext, Order, int>
   {
       public OrderRepository(IServiceProvider serviceProvider) : base(serviceProvider) { }
       public IQueryable<Order> Query() => Entities;
       // ... implement IRepository methods
   }
   
   public class OrderService
   {
       private readonly OrderRepository _repository;
       public OrderService(OrderRepository repository) { }
   }
   
   // NEW Option 2: Use provider directly (simple cases)
   public class OrderService
   {
       private readonly IRepositoryProvider<Order, int> _provider;
       public OrderService(IRepositoryProvider<Order, int> provider) { }
   }
   ```

2. **RegisterDbContext is deprecated**
   ```csharp
   // Compiler warning - method will be removed in future
   uow.RegisterDbContext(wrapper);
   ```

## Benefits

1. **Better Separation of Concerns**
   - MiCake layer doesn't know about DbContext
   - Persistence layer handles its own registration
   - Clear boundary between layers

2. **Cleaner Public API**
   - No implementation details in public interface
   - Only user-facing methods exposed

3. **Enhanced Features**
   - Savepoints for partial rollback
   - Event hooks for cross-cutting concerns
   - Future-proof for other persistence layers

4. **Simpler Architecture**
   - No ProxyRepository indirection
   - Direct repository implementation
   - Easier to understand and debug

## Testing Status

- ✅ MiCake layer builds successfully
- ⏳ MiCake tests need updates (ProxyRepository removed)
- ⏳ EF Core layer needs updates (IUnitOfWorkResource implementation)

## Next Steps

### For EF Core Module (separate commit)
1. Make `EFCoreDbContextWrapper` implement `IUnitOfWorkResource`
2. Update `EFCoreContextFactory` to use `IUnitOfWorkInternal.RegisterResource()`
3. Add savepoint support to `EFCoreDbContextWrapper`
4. Update `BeginTransactionAsync()` signature to accept `IsolationLevel?`

### For Documentation
1. Update official docs with new repository pattern
2. Create migration guide for existing users
3. Add examples for savepoints and event hooks

### For Future
1. Remove deprecated `RegisterDbContext()` method (breaking change)
2. Remove `IDbContextWrapper` from MiCake layer (move to EF Core)
3. Consider adding support for other persistence layers (Dapper, ADO.NET)

## Conclusion

The redesign successfully addresses all identified issues while adding powerful new features (savepoints, event hooks). The architecture is now cleaner, more maintainable, and properly separates concerns between layers.

The MiCake layer is complete and compiles successfully. EF Core layer updates are needed to fully integrate the new design.
