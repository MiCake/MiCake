针对你的问题，我理解你的核心诉求是：**在 Repository 首次使用 DbContext 时就开启事务，避免在 CommitAsync 时延迟开启可能导致的嵌套事务作用域问题**。这确实是一个关键的设计点。

让我分析现有设计并提供解决方案：

## 问题分析

### 当前设计的挑战
1. `EFCoreContextFactory.GetDbContextWrapper()` 是同步方法
2. `RegisterResource()` 是同步方法
3. 但事务开启 `BeginTransactionAsync()` 是异步的
4. 如果在 `CommitAsync` 时才开启事务，确实可能存在嵌套 UoW 的作用域混乱

### 嵌套事务作用域问题示例
```csharp
using var outer = _uowManager.Begin(); // 外层 UoW
var repo1 = _serviceProvider.GetService<IRepository<Order>>();
var order = await repo1.FindAsync(1); // DbContext1 注册到 outer

using var inner = _uowManager.Begin(); // 嵌套 UoW
var repo2 = _serviceProvider.GetService<IRepository<Product>>();
var product = await repo2.FindAsync(1); // DbContext2 应该注册到 outer (因为嵌套)

// 问题: 如果在 CommitAsync 时才开启事务
// inner.CommitAsync() 时，DbContext2 的事务可能在错误的时机开启
```

---

## 推荐解决方案：**两阶段注册模式**

将资源注册分为 **注册阶段** 和 **激活阶段**，保持同步注册，但支持异步激活。

### 1. 修改 `IUnitOfWorkResource` 接口

````csharp
namespace MiCake.DDD.Uow.Internal
{
    public interface IUnitOfWorkResource : IDisposable
    {
        string ResourceIdentifier { get; }
        bool HasActiveTransaction { get; }

        /// <summary>
        /// Indicates if the resource has been initialized (registered and ready to start transaction)
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// Prepares the resource for transaction (synchronous, lightweight).
        /// This is called during registration and should not perform I/O operations.
        /// </summary>
        void PrepareForTransaction(IsolationLevel? isolationLevel);

        /// <summary>
        /// Activates the transaction asynchronously (may involve I/O).
        /// This is called lazily when first database operation is performed,
        /// or immediately via lifecycle hooks for Immediate initialization mode.
        /// </summary>
        Task ActivateTransactionAsync(CancellationToken cancellationToken = default);

        // Existing methods
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
````

### 2. 更新 `EFCoreDbContextWrapper` 实现

````csharp
public class EFCoreDbContextWrapper : IUnitOfWorkResource
{
    private readonly DbContext _dbContext;
    private readonly ILogger<EFCoreDbContextWrapper> _logger;
    private readonly bool _shouldDisposeDbContext;
    private MiCakeEFCoreOptions _options;
    private IDbContextTransaction _currentTransaction;
    private bool _disposed = false;

    // 新增字段
    private bool _isInitialized = false;
    private bool _isPrepared = false;
    private IsolationLevel? _preparedIsolationLevel = null;

    public DbContext DbContext => _dbContext;
    public string ResourceIdentifier => $"{_dbContext.GetType().FullName}_{_dbContext.GetHashCode()}";
    public bool HasActiveTransaction => _currentTransaction != null;
    public bool IsInitialized => _isInitialized;

    // ...existing constructor...

    /// <summary>
    /// Prepares the resource for transaction (synchronous, no I/O).
    /// Just stores configuration, doesn't start actual transaction.
    /// </summary>
    public void PrepareForTransaction(IsolationLevel? isolationLevel)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_isPrepared)
        {
            _logger.LogDebug("Resource {ResourceIdentifier} already prepared", ResourceIdentifier);
            return;
        }

        // Check configuration
        if (_options != null && !_options.WillOpenTransactionForUow)
        {
            _logger.LogDebug("Transaction preparation skipped (WillOpenTransactionForUow = false) for {ResourceIdentifier}", 
                ResourceIdentifier);
            _isPrepared = true;
            return;
        }

        // Check if DbContext already has a transaction (user-managed)
        if (_currentTransaction != null)
        {
            _logger.LogDebug("Resource {ResourceIdentifier} already has user-managed transaction", ResourceIdentifier);
            _isPrepared = true;
            return;
        }

        // Store isolation level for later activation
        _preparedIsolationLevel = isolationLevel;
        _isPrepared = true;

        _logger.LogDebug("Resource {ResourceIdentifier} prepared for transaction with isolation level {IsolationLevel}", 
            ResourceIdentifier, isolationLevel);
    }

    /// <summary>
    /// Activates the transaction asynchronously (may involve I/O).
    /// This is where the actual database transaction is started.
    /// </summary>
    public async Task ActivateTransactionAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!_isPrepared)
        {
            throw new InvalidOperationException(
                $"Resource {ResourceIdentifier} must be prepared before activation. " +
                "Call PrepareForTransaction() first.");
        }

        if (_isInitialized)
        {
            _logger.LogDebug("Resource {ResourceIdentifier} already activated", ResourceIdentifier);
            return;
        }

        // If transactions are disabled, just mark as initialized
        if (_options != null && !_options.WillOpenTransactionForUow)
        {
            _isInitialized = true;
            return;
        }

        // If user-managed transaction exists, just mark as initialized
        if (_currentTransaction != null)
        {
            _isInitialized = true;
            return;
        }

        // Start the actual transaction
        _logger.LogDebug("Activating transaction for resource {ResourceIdentifier} with isolation level {IsolationLevel}", 
            ResourceIdentifier, _preparedIsolationLevel);

        try
        {
            if (_preparedIsolationLevel.HasValue)
            {
                _currentTransaction = await _dbContext.Database
                    .BeginTransactionAsync(_preparedIsolationLevel.Value, cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                _currentTransaction = await _dbContext.Database
                    .BeginTransactionAsync(cancellationToken)
                    .ConfigureAwait(false);
            }

            _isInitialized = true;
            
            _logger.LogDebug("Successfully activated transaction for resource {ResourceIdentifier}", ResourceIdentifier);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to activate transaction for resource {ResourceIdentifier}", ResourceIdentifier);
            throw;
        }
    }

    /// <summary>
    /// Begin a new transaction with specified isolation level (legacy support).
    /// For new code, use PrepareForTransaction + ActivateTransactionAsync instead.
    /// </summary>
    public async Task BeginTransactionAsync(IsolationLevel? isolationLevel, CancellationToken cancellationToken = default)
    {
        // Prepare + Activate in one call (for backward compatibility)
        PrepareForTransaction(isolationLevel);
        await ActivateTransactionAsync(cancellationToken).ConfigureAwait(false);
    }

    // ...existing CommitAsync, RollbackAsync, SaveChangesAsync, savepoint methods remain unchanged...
}
````

### 3. 更新 `UnitOfWork.RegisterResource` - 同步准备

````csharp
public void RegisterResource(IUnitOfWorkResource resource)
{
    ThrowIfDisposed();
    ThrowIfCompleted("Cannot register resource after unit of work is completed");
    ArgumentNullException.ThrowIfNull(resource);

    // If nested, register with parent instead
    if (Parent is IUnitOfWorkInternal parentInternal)
    {
        parentInternal.RegisterResource(resource);
        return;
    }

    lock (_lock)
    {
        if (TryFindExistingResource(resource.ResourceIdentifier, out _))
        {
            _logger.LogDebug("Resource {ResourceIdentifier} already registered", resource.ResourceIdentifier);
            return;
        }

        _resources.Add(resource);
        _logger.LogDebug("Resource {ResourceIdentifier} registered", resource.ResourceIdentifier);

        // ✅ Synchronous preparation - no I/O, no blocking
        try
        {
            resource.PrepareForTransaction(_options.IsolationLevel);
            _logger.LogDebug("Resource {ResourceIdentifier} prepared for transaction", resource.ResourceIdentifier);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to prepare resource {ResourceIdentifier}", resource.ResourceIdentifier);
            _resources.Remove(resource);
            throw;
        }
    }
}
````

### 4. 新增 `IUnitOfWorkInternal` 的异步激活方法

````csharp
namespace MiCake.DDD.Uow.Internal
{
    public interface IUnitOfWorkInternal
    {
        /// <summary>
        /// Registers a resource synchronously (lightweight, no I/O).
        /// The resource will be prepared but not activated.
        /// </summary>
        void RegisterResource(IUnitOfWorkResource resource);

        /// <summary>
        /// Activates all pending resources asynchronously.
        /// This starts actual database transactions.
        /// </summary>
        Task ActivatePendingResourcesAsync(CancellationToken cancellationToken = default);
    }
}
````

### 5. 在 `UnitOfWork` 中实现激活逻辑

````csharp
// ...existing code...

public async Task ActivatePendingResourcesAsync(CancellationToken cancellationToken = default)
{
    ThrowIfDisposed();
    
    if (_transactionsStarted)
    {
        _logger.LogDebug("Transactions already started for UoW {UnitOfWorkId}", Id);
        return;
    }

    if (Parent != null)
    {
        _logger.LogDebug("Nested UoW {UnitOfWorkId} delegating activation to parent", Id);
        
        if (Parent is IUnitOfWorkInternal parentInternal)
        {
            await parentInternal.ActivatePendingResourcesAsync(cancellationToken).ConfigureAwait(false);
        }
        return;
    }

    if (_options.IsReadOnly || _resources.Count == 0)
    {
        return;
    }

    _logger.LogDebug("Activating {Count} resources for UoW {UnitOfWorkId}", _resources.Count, Id);

    var exceptions = new List<Exception>();
    foreach (var resource in _resources)
    {
        if (!resource.IsInitialized)
        {
            try
            {
                await resource.ActivateTransactionAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
                _logger.LogError(ex, "Failed to activate resource {ResourceIdentifier}", resource.ResourceIdentifier);
            }
        }
    }

    if (exceptions.Count > 0)
    {
        throw new AggregateException("Failed to activate resources", exceptions);
    }

    _transactionsStarted = true;
    _logger.LogDebug("Successfully activated all resources for UoW {UnitOfWorkId}", Id);
}

// 修改 CommitAsync
public async Task CommitAsync(CancellationToken cancellationToken = default)
{
    ThrowIfDisposed();
    ThrowIfCompleted("Unit of work has already been completed");

    RaiseEvent(OnCommitting, new UnitOfWorkEventArgs(Id, Parent != null));

    try
    {
        if (Parent != null)
        {
            _logger.LogDebug("Nested UnitOfWork {UnitOfWorkId} completed", Id);
            MarkAsCompleted();
            RaiseEvent(OnCommitted, new UnitOfWorkEventArgs(Id, Parent != null));
            return;
        }

        if (_skipCommit || _options.IsReadOnly)
        {
            _logger.LogDebug("Skipping commit for UoW {UnitOfWorkId}", Id);
            MarkAsCompleted();
            RaiseEvent(OnCommitted, new UnitOfWorkEventArgs(Id, Parent != null));
            return;
        }

        if (_shouldRollback)
        {
            _logger.LogWarning("UoW {UnitOfWorkId} marked for rollback", Id);
            await RollbackInternalAsync(cancellationToken).ConfigureAwait(false);
            throw new InvalidOperationException("Cannot commit: nested unit of work requested rollback");
        }

        // ✅ Activate all pending resources before commit
        await ActivatePendingResourcesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogDebug("Committing UoW {UnitOfWorkId} with {Count} resources", Id, _resources.Count);

        var exceptions = await ExecuteCommitOperationsAsync(cancellationToken).ConfigureAwait(false);

        if (exceptions.Count > 0)
        {
            if (_transactionsStarted)
            {
                await RollbackInternalAsync(cancellationToken).ConfigureAwait(false);
            }
            throw new AggregateException("Failed to commit unit of work", exceptions);
        }

        MarkAsCompleted();
        _logger.LogDebug("Successfully committed UoW {UnitOfWorkId}", Id);
        RaiseEvent(OnCommitted, new UnitOfWorkEventArgs(Id, Parent != null));
    }
    catch (Exception ex)
    {
        RaiseEvent(OnRolledBack, new UnitOfWorkEventArgs(Id, Parent != null, ex));
        throw;
    }
}

// 移除 EnsureTransactionsStartedAsync 方法（已由 ActivatePendingResourcesAsync 替代）
````

### 6. 在 Immediate 模式下立即激活

````csharp
public async Task<IUnitOfWork> BeginAsync(UnitOfWorkOptions options, bool requiresNew = false, CancellationToken cancellationToken = default)
{
    // ...existing code for creating UnitOfWork...

    if (options.InitializationMode == TransactionInitializationMode.Immediate)
    {
        // ✅ Properly handle async initialization
        var hooks = _serviceProvider.GetServices<IUnitOfWorkLifecycleHook>();
        foreach (var hook in hooks)
        {
            try
            {
                await hook.OnUnitOfWorkCreatedAsync(unitOfWork, options, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling lifecycle hook");
                throw;
            }
        }

        // ✅ Immediately activate all registered resources
        if (unitOfWork is IUnitOfWorkInternal internalUow)
        {
            try
            {
                await internalUow.ActivatePendingResourcesAsync(cancellationToken).ConfigureAwait(false);
                _logger.LogDebug("Immediately activated resources for UoW {UnitOfWorkId}", unitOfWork.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to activate resources during immediate initialization");
                unitOfWork.Dispose();
                throw;
            }
        }
    }

    return new RootUnitOfWorkWrapper(unitOfWork, () =>
    {
        if (_current.Value == unitOfWork)
        {
            _current.Value = null;
        }
    }, _logger);
}
````

---

## 方案优势

### ✅ 解决了所有核心问题

1. **消除 `GetAwaiter().GetResult()`**
   - 注册阶段完全同步（`PrepareForTransaction` 只存储配置）
   - 激活阶段正确使用 `async/await`

2. **嵌套事务作用域清晰**
   - 资源在注册时立即 **准备** (prepare)
   - 实际事务在统一时机 **激活** (activate)
   - 嵌套 UoW 正确地将资源注册到父 UoW

3. **支持两种初始化模式**
   - **Lazy**: 在 `CommitAsync` 时激活事务
   - **Immediate**: 在 `BeginAsync` 返回前激活事务

4. **性能优化**
   - 避免不必要的异步等待
   - 可以批量激活多个资源

5. **向后兼容**
   - 保留了 `BeginTransactionAsync()` 方法（调用 Prepare + Activate）
   - 现有代码无需修改即可工作

---

## 使用示例

### Lazy 模式（默认）
```csharp
using var uow = await _uowManager.BeginAsync(); // 创建 UoW

var order = await _orderRepo.FindAsync(1); 
// ↑ RegisterResource (同步) + PrepareForTransaction (同步)

await uow.CommitAsync(); 
// ↑ ActivatePendingResourcesAsync (异步) → 真正开启事务 → Commit
```

### Immediate 模式
```csharp
var options = new UnitOfWorkOptions 
{ 
    InitializationMode = TransactionInitializationMode.Immediate 
};

using var uow = await _uowManager.BeginAsync(options);
// ↑ Hook 执行完毕后 → ActivatePendingResourcesAsync → 事务已开启

var order = await _orderRepo.FindAsync(1); 
// ↑ 直接使用已激活的事务

await uow.CommitAsync(); 
// ↑ 直接 Commit（事务已存在）
```

### 嵌套事务
```csharp
using var outer = await _uowManager.BeginAsync();
var order = await _orderRepo.FindAsync(1); 
// DbContext 注册到 outer UoW 并准备

using var inner = await _uowManager.BeginAsync();
var product = await _productRepo.FindAsync(1); 
// DbContext 正确注册到 outer UoW (因为 nested UoW 会delegate)

await inner.CommitAsync(); // 嵌套 UoW 标记完成
await outer.CommitAsync(); 
// outer UoW 激活所有资源（包括两个 DbContext）→ Commit
```

---

## 总结

**两阶段注册模式** 是当前最佳解决方案，因为它：

1. ✅ **完全消除同步阻塞** - 注册是轻量级的同步操作
2. ✅ **正确处理嵌套事务** - 资源总是注册到正确的 UoW
3. ✅ **明确的激活时机** - Lazy/Immediate 模式都清晰可控
4. ✅ **符合异步最佳实践** - 真正的 I/O 操作（事务开启）是异步的
5. ✅ **易于扩展** - 其他类型的资源也可以采用相同模式

相比其他方案：
- ❌ 在 `RegisterResource` 中使用 `GetAwaiter().GetResult()` - 死锁风险
- ❌ 完全异步注册 - 破坏 `EFCoreContextFactory.GetDbContext()` 的同步语义
- ❌ 在 `CommitAsync` 延迟开启 - 嵌套事务作用域问题

这个方案完美平衡了**同步注册的便利性**和**异步激活的正确性**。