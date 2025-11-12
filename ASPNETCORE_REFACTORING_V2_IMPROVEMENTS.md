# MiCake.AspNetCore Refactoring - Design Improvements

## Overview
This document describes the design improvements made to address feedback on the initial refactoring, focusing on simplification and flexibility.

## Problems Addressed

### 1. Over-Granular UoW Configuration Options

**Previous Design**:
```csharp
public class MiCakeAspNetUowOption
{
    public bool IsAutoBeginEnabled { get; set; } = true;
    public bool IsAutoCommitEnabled { get; set; } = true;
    public bool IsAutoRollbackEnabled { get; set; } = true;
    public List<string> KeyWordForCloseAutoCommit { get; set; } = ["Find", "Get", "Query", "Search"];
}
```

**Issues**:
- Too many configuration flags increased complexity
- Users needed to understand relationships between flags
- Potential for misconfiguration (e.g., auto-begin enabled but auto-commit disabled)
- Confusing naming (`KeyWordForCloseAutoCommit`)

**New Design**:
```csharp
public class MiCakeAspNetUowOption
{
    /// <summary>
    /// Enables automatic transaction management (begin, commit on success, rollback on failure).
    /// When false, you must manually manage transactions.
    /// </summary>
    public bool IsAutoTransactionEnabled { get; set; } = true;
    
    /// <summary>
    /// Action names starting with these keywords are treated as read-only operations.
    /// Read-only operations skip transaction commit for better performance.
    /// </summary>
    public List<string> ReadOnlyActionKeywords { get; set; } = ["Find", "Get", "Query", "Search"];
}
```

**Benefits**:
- Single flag for all automatic transaction behavior
- Clear naming: `IsAutoTransactionEnabled` vs `IsAutoBeginEnabled`
- Better naming: `ReadOnlyActionKeywords` vs `KeyWordForCloseAutoCommit`
- Impossible to misconfigure

### 2. Inflexible Transaction Initialization

**Previous Design**:
- Only "lazy initialization" supported
- Transactions started when first DbContext was accessed
- No way to guarantee transactions were active before repository access

**Issues**:
- Misleading: `Begin()` didn't actually start transactions immediately
- No option for scenarios requiring guaranteed transaction semantics
- Difficult to reason about transaction lifecycle

**New Design**:

Added `TransactionInitializationMode` enum:

```csharp
public enum TransactionInitializationMode
{
    /// <summary>
    /// Lazy - Transaction starts when first resource (DbContext) is accessed (default)
    /// </summary>
    Lazy = 0,
    
    /// <summary>
    /// Immediate - Transaction starts immediately when UoW is created
    /// </summary>
    Immediate = 1
}

public class UnitOfWorkOptions
{
    public TransactionInitializationMode InitializationMode { get; set; } = TransactionInitializationMode.Lazy;
    // ...
}
```

**Benefits**:
- Explicit choice between lazy and immediate initialization
- Users can choose based on their requirements
- Clear semantics: Immediate mode guarantees active transactions

## New Features

### 3. Declarative Transaction Control with Attributes

**Problem**: No way to override transaction behavior at Controller or Action level without changing global configuration.

**Solution**: Added `[UnitOfWork]` attribute:

```csharp
/// <summary>
/// Attribute to control Unit of Work behavior at Controller or Action level.
/// When applied, overrides default automatic transaction behavior.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class UnitOfWorkAttribute : Attribute
{
    /// <summary>
    /// Whether to enable automatic transaction management.
    /// When null, uses default from MiCakeAspNetOptions.
    /// </summary>
    public bool? IsEnabled { get; set; }
    
    public bool IsReadOnly { get; set; } = false;
    public IsolationLevel IsolationLevel { get; set; } = IsolationLevel.ReadCommitted;
    public TransactionInitializationMode InitializationMode { get; set; } = TransactionInitializationMode.Lazy;
}
```

**Usage Examples**:

```csharp
// Disable UoW for entire controller
[DisableUnitOfWork]
public class ReportController : ControllerBase
{
    // No transactions for any actions in this controller
}

// Override at action level
public class OrderController : ControllerBase
{
    [UnitOfWork(IsReadOnly = true)]
    public async Task<IActionResult> GetOrders() { }
    
    [UnitOfWork(InitializationMode = TransactionInitializationMode.Immediate)]
    public async Task<IActionResult> CreateOrder() { }
    
    [UnitOfWork(IsolationLevel = IsolationLevel.Serializable)]
    public async Task<IActionResult> CriticalOperation() { }
}

// Mix global and local settings
public class HybridController : ControllerBase
{
    // Uses global settings from MiCakeAspNetOptions
    public async Task<IActionResult> DefaultBehavior() { }
    
    // Override for specific action
    [DisableUnitOfWork]
    public async Task<IActionResult> NoTransaction() { }
}
```

## Implementation Architecture

### Lifecycle Hook Pattern

To support immediate transaction initialization without violating layer dependencies, we introduced a lifecycle hook pattern:

**MiCake (DDD Layer)**:
```csharp
/// <summary>
/// Hook interface for customizing Unit of Work lifecycle.
/// </summary>
public interface IUnitOfWorkLifecycleHook
{
    Task OnUnitOfWorkCreatedAsync(IUnitOfWork unitOfWork, UnitOfWorkOptions options, CancellationToken cancellationToken);
}
```

**MiCake.EntityFrameworkCore Layer**:
```csharp
/// <summary>
/// Lifecycle hook that handles immediate transaction initialization.
/// </summary>
[InjectService(typeof(IUnitOfWorkLifecycleHook), Lifetime = MiCakeServiceLifetime.Scoped)]
public class ImmediateTransactionLifecycleHook : IUnitOfWorkLifecycleHook
{
    public async Task OnUnitOfWorkCreatedAsync(IUnitOfWork unitOfWork, UnitOfWorkOptions options, CancellationToken cancellationToken)
    {
        if (options.InitializationMode == TransactionInitializationMode.Immediate)
        {
            await _initializer.InitializeTransactionsAsync(unitOfWork, cancellationToken);
        }
    }
}
```

**Benefits**:
- DDD layer remains independent of EFCore
- Extensible: can add custom lifecycle hooks
- Follows Open/Closed Principle

### DbContext Type Registry

To support immediate initialization, we need to know which DbContext types are registered:

```csharp
public interface IDbContextTypeRegistry
{
    void RegisterDbContextType(Type dbContextType);
    IReadOnlyList<Type> GetRegisteredTypes();
}
```

**Auto-registration**: When `UseEFCore<TDbContext>()` is called, the DbContext type is automatically registered:

```csharp
public static IServiceCollection AddUowCoreServices(this IServiceCollection services, Type dbContextType)
{
    // ... register factory and dependencies
    
    // Auto-register DbContext type for immediate initialization
    var sp = services.BuildServiceProvider();
    var registry = sp.GetService<IDbContextTypeRegistry>();
    if (registry != null)
    {
        registry.RegisterDbContextType(dbContextType);
    }
    
    return services;
}
```

### Immediate Transaction Initializer

When immediate mode is requested, the initializer proactively creates DbContext wrappers for all registered types:

```csharp
public class ImmediateTransactionInitializer : IImmediateTransactionInitializer
{
    public Task InitializeTransactionsAsync(IUnitOfWork unitOfWork, CancellationToken cancellationToken)
    {
        var registeredTypes = _typeRegistry.GetRegisteredTypes();
        
        foreach (var dbContextType in registeredTypes)
        {
            // Get factory for this DbContext type
            var factoryType = typeof(IEFCoreContextFactory<>).MakeGenericType(dbContextType);
            var factory = _serviceProvider.GetService(factoryType);
            
            // Get DbContext wrapper (this registers it with UoW and starts transaction)
            var wrapper = factory.GetDbContextWrapper();
        }
        
        return Task.CompletedTask;
    }
}
```

## Configuration Examples

### Basic Usage

**Default (auto-managed, lazy initialization)**:
```csharp
services.AddMiCakeWithDefault<MyModule, MyDbContext>();
```

**Disable auto-transaction**:
```csharp
services.AddMiCakeWithDefault<MyModule, MyDbContext>(
    miCakeAspNetConfig: options => {
        options.UnitOfWork.IsAutoTransactionEnabled = false;
    });
```

**Custom read-only keywords**:
```csharp
services.AddMiCakeWithDefault<MyModule, MyDbContext>(
    miCakeAspNetConfig: options => {
        options.UnitOfWork.ReadOnlyActionKeywords = ["Find", "Get", "Query", "Search", "List", "Fetch"];
    });
```

### Immediate Initialization

**Via UnitOfWorkOptions**:
```csharp
var uow = _uowManager.Begin(UnitOfWorkOptions.Immediate);
// Transactions are already started for all registered DbContexts
```

**Via Attribute**:
```csharp
[UnitOfWork(InitializationMode = TransactionInitializationMode.Immediate)]
public async Task<IActionResult> CriticalOperation()
{
    // Transaction is guaranteed to be active when action starts
}
```

### Attribute-Based Control

**Disable for specific controller**:
```csharp
[DisableUnitOfWork]
public class ReadOnlyController : ControllerBase
{
    public async Task<IActionResult> GetData() 
    {
        // No UoW created - manual transaction management if needed
    }
}
```

**Mix and match**:
```csharp
public class OrderController : ControllerBase
{
    // Default behavior
    public async Task<IActionResult> GetOrders() { }
    
    // Read-only optimization
    [UnitOfWork(IsReadOnly = true)]
    public async Task<IActionResult> SearchOrders() { }
    
    // Immediate initialization for critical path
    [UnitOfWork(InitializationMode = TransactionInitializationMode.Immediate)]
    public async Task<IActionResult> PlaceOrder() { }
    
    // No transaction for caching operation
    [DisableUnitOfWork]
    public async Task<IActionResult> GetCachedData() { }
}
```

## Design Principles Followed

### 1. Single Responsibility Principle
- Each component has one clear purpose
- `UnitOfWorkFilter`: Manages UoW lifecycle for actions
- `ImmediateTransactionInitializer`: Handles immediate initialization
- `DbContextTypeRegistry`: Tracks registered types

### 2. Open/Closed Principle
- Extensible via `IUnitOfWorkLifecycleHook` without modifying core
- Can add custom hooks for logging, metrics, etc.

### 3. Dependency Inversion Principle
- DDD layer defines `IUnitOfWorkLifecycleHook` interface
- EFCore layer implements the interface
- No direct dependency from DDD to EFCore

### 4. Interface Segregation Principle
- Small, focused interfaces
- `IImmediateTransactionInitializer` has single method
- `IDbContextTypeRegistry` has minimal surface area

### 5. Liskov Substitution Principle
- `UnitOfWorkAttribute` can be replaced with `DisableUnitOfWorkAttribute`
- Both work correctly in all contexts

## Migration Guide

### From Previous Refactoring

**Options changes**:
```csharp
// Before
options.UnitOfWork.IsAutoBeginEnabled = true;
options.UnitOfWork.IsAutoCommitEnabled = true;
options.UnitOfWork.IsAutoRollbackEnabled = true;
options.UnitOfWork.KeyWordForCloseAutoCommit = ["Find", "Get"];

// After
options.UnitOfWork.IsAutoTransactionEnabled = true;  // Single flag
options.UnitOfWork.ReadOnlyActionKeywords = ["Find", "Get"];  // Better name
```

**Immediate initialization**:
```csharp
// New capability - wasn't possible before
var uow = _uowManager.Begin(UnitOfWorkOptions.Immediate);
```

**Declarative control**:
```csharp
// New capability - use attributes instead of global config
[UnitOfWork(IsReadOnly = true)]
public async Task<IActionResult> MyAction() { }
```

## Summary

### What Changed
1. ✅ Simplified configuration (3 flags → 1 flag)
2. ✅ Added immediate transaction initialization mode
3. ✅ Added declarative control via attributes
4. ✅ Better naming (`ReadOnlyActionKeywords` vs `KeyWordForCloseAutoCommit`)
5. ✅ Extensible lifecycle hooks

### Benefits
1. **Simpler**: One flag instead of three, clearer naming
2. **More Flexible**: Support both lazy and immediate initialization
3. **More Expressive**: Attributes make intent clear
4. **Better Performance**: Read-only optimization still available
5. **Extensible**: Lifecycle hooks for custom logic

### Design Quality
- ✅ Follows SOLID principles
- ✅ Maintains layer separation
- ✅ Backward compatible (same default behavior)
- ✅ Well-documented with XML comments
- ✅ All framework projects build successfully

---
**Version**: 2.1.0  
**Date**: 2025-11-12  
**Commit**: 350f0c3
