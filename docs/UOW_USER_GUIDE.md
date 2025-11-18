# MiCake Unit of Work (UoW) 使用文档

## 概述

MiCake框架提供了一个强大而灵活的工作单元（Unit of Work）实现，用于管理数据库事务。该实现采用了**两阶段注册模式**，确保了异步操作的正确性和嵌套事务的作用域清晰性。

## 核心概念

### 什么是工作单元（Unit of Work）？

工作单元是一种设计模式，用于：
- 跟踪业务事务期间对象的所有更改
- 将多个数据库操作作为单个事务提交
- 在出现错误时回滚所有更改

### 两阶段注册模式

MiCake的UoW采用两阶段模式来处理数据库事务：

1. **准备阶段（Prepare）** - 同步，无I/O操作
   - 注册资源（DbContext）
   - 存储事务配置（如隔离级别）
   - 立即返回，不阻塞

2. **激活阶段（Activate）** - 异步，可能涉及I/O
   - 实际开启数据库事务
   - 在Lazy模式下，于Commit前执行
   - 在Immediate模式下，于Begin后立即执行

## 基本用法

### 1. 自动工作单元（推荐）

在ASP.NET Core应用中，工作单元会自动管理：

```csharp
// Startup.cs 或 Program.cs
services.AddMiCakeWithDefault<MyModule, MyDbContext>();
```

**Controller示例**：
```csharp
public class OrderController : ControllerBase
{
    private readonly IRepository<Order> _orderRepository;

    public OrderController(IRepository<Order> orderRepository)
    {
        _orderRepository = orderRepository;
    }

    // ✅ 工作单元自动创建、提交或回滚
    [HttpPost]
    public async Task<IActionResult> CreateOrder(CreateOrderDto dto)
    {
        var order = new Order(dto.CustomerId, dto.Items);
        await _orderRepository.AddAsync(order);
        
        // UoW会在action执行成功后自动提交
        return Ok(order.Id);
    }
}
```

### 2. 手动工作单元

对于非Web场景或需要精确控制的情况：

```csharp
public class OrderService
{
    private readonly IUnitOfWorkManager _uowManager;
    private readonly IRepository<Order> _orderRepository;

    public OrderService(
        IUnitOfWorkManager uowManager,
        IRepository<Order> orderRepository)
    {
        _uowManager = uowManager;
        _orderRepository = orderRepository;
    }

    public async Task ProcessOrderAsync(int orderId)
    {
        // ✅ 使用BeginAsync创建工作单元（推荐）
        using var uow = await _uowManager.BeginAsync();
        
        var order = await _orderRepository.FindAsync(orderId);
        order.Process();
        
        // 提交事务
        await uow.CommitAsync();
    }
}
```

## 事务初始化模式

### Lazy模式（默认）

事务在第一次数据库操作前**延迟初始化**：

```csharp
using var uow = await _uowManager.BeginAsync();
// 此时事务尚未开启

var order = await _orderRepository.FindAsync(1);
// DbContext注册并准备（同步）

await uow.CommitAsync();
// 此时事务才真正开启并提交（异步）
```

**优点**：
- 性能最优，只在需要时才开启事务
- 适合读多写少的场景
- 如果操作是只读的，可能根本不需要开启事务

### Immediate模式

事务在创建UoW时**立即初始化**：

```csharp
var options = new UnitOfWorkOptions
{
    InitializationMode = TransactionInitializationMode.Immediate
};

using var uow = await _uowManager.BeginAsync(options);
// 事务已经开启，所有已注册的DbContext都已激活

var order = await _orderRepository.FindAsync(1);
// 直接使用已激活的事务

await uow.CommitAsync();
```

**使用场景**：
- 需要保证事务语义的关键操作
- 分布式场景需要明确事务边界
- 调试时需要清晰的事务生命周期

### 通过Attribute控制

```csharp
// Controller级别：所有Action使用立即初始化
[UnitOfWork(InitializationMode = TransactionInitializationMode.Immediate)]
public class CriticalOperationController : ControllerBase
{
    // ...
}

// Action级别：特定操作使用立即初始化
public class OrderController : ControllerBase
{
    [UnitOfWork(InitializationMode = TransactionInitializationMode.Immediate)]
    public async Task<IActionResult> CriticalOperation()
    {
        // 事务在action开始前就已开启
    }
}
```

## 嵌套事务

MiCake支持嵌套工作单元，内层UoW会自动使用外层UoW的事务：

```csharp
public async Task ComplexOperationAsync()
{
    // 外层工作单元
    using var outerUow = await _uowManager.BeginAsync();
    
    var order = await _orderRepository.FindAsync(1);
    order.Update();
    
    // 内层工作单元（嵌套）
    using var innerUow = await _uowManager.BeginAsync();
    
    var product = await _productRepository.FindAsync(1);
    product.DecreaseStock();
    
    await innerUow.CommitAsync();  // 标记内层完成
    await outerUow.CommitAsync();  // 外层提交时统一激活并提交
}
```

**嵌套规则**：
1. 内层UoW的资源会注册到外层UoW
2. 只有最外层UoW负责事务的实际提交
3. 如果内层UoW失败，会标记外层UoW回滚
4. 嵌套深度无限制，但建议不超过3层

## Attribute声明式控制

### 启用工作单元

```csharp
[UnitOfWork]
public class ProductController : ControllerBase
{
    // 所有Action都会自动创建UoW
}
```

### 禁用工作单元

```csharp
[DisableUnitOfWork]
public class ReportController : ControllerBase
{
    // 纯查询Controller，不需要事务
}
```

或在Action级别覆盖：

```csharp
public class MixedController : ControllerBase
{
    // 默认启用UoW
    
    [DisableUnitOfWork]
    public async Task<IActionResult> GetCachedData()
    {
        // 此Action不创建UoW
    }
}
```

### 自定义隔离级别

```csharp
[UnitOfWork(IsolationLevel = IsolationLevel.Serializable)]
public async Task<IActionResult> HighConsistencyOperation()
{
    // 使用最高隔离级别
}
```

### 只读操作优化

MiCake会自动识别只读操作（根据Action名称）：

```csharp
public class OrderController : ControllerBase
{
    // 自动识别为只读（跳过事务提交）
    public async Task<IActionResult> GetOrder(int id) { }
    public async Task<IActionResult> FindOrders() { }
    public async Task<IActionResult> QueryOrders() { }
    public async Task<IActionResult> SearchOrders() { }
}
```

可通过配置自定义只读关键字：

```csharp
services.AddMiCakeWithDefault<MyModule, MyDbContext>(
    miCakeAspNetConfig: options =>
    {
        options.UnitOfWork.ReadOnlyActionKeywords = ["Get", "Find", "Query", "Search", "List", "Fetch"];
    });
```

## 高级场景

### 1. 禁用自动工作单元

```csharp
services.AddMiCakeWithDefault<MyModule, MyDbContext>(
    miCakeAspNetConfig: options =>
    {
        options.UnitOfWork.IsAutoTransactionEnabled = false;
    });
```

此时需要手动管理所有工作单元。

### 2. Savepoint（保存点）

在长事务中创建保存点，支持部分回滚：

```csharp
using var uow = await _uowManager.BeginAsync();

// 执行一些操作
await ProcessStep1();

// 创建保存点
var savepoint = await uow.CreateSavepointAsync("step1");

try
{
    // 执行可能失败的操作
    await ProcessStep2();
}
catch
{
    // 回滚到保存点，保留step1的更改
    await uow.RollbackToSavepointAsync("step1");
}

await uow.CommitAsync();
```

### 3. 手动回滚

```csharp
using var uow = await _uowManager.BeginAsync();

try
{
    await ProcessOrder();
    
    if (someCondition)
    {
        // 手动回滚
        await uow.RollbackAsync();
        return;
    }
    
    await uow.CommitAsync();
}
catch
{
    // 异常时自动回滚
    throw;
}
```

### 4. 监听工作单元事件

```csharp
using var uow = await _uowManager.BeginAsync();

uow.OnCommitting += (sender, args) =>
{
    _logger.LogInformation("UoW {Id} is committing", args.UnitOfWorkId);
};

uow.OnCommitted += (sender, args) =>
{
    _logger.LogInformation("UoW {Id} committed successfully", args.UnitOfWorkId);
};

uow.OnRolledBack += (sender, args) =>
{
    _logger.LogWarning(args.Exception, "UoW {Id} rolled back", args.UnitOfWorkId);
};

await ProcessOrder();
await uow.CommitAsync();
```

## 配置选项

### ASP.NET Core配置

```csharp
services.AddMiCakeWithDefault<MyModule, MyDbContext>(
    miCakeAspNetConfig: options =>
    {
        // 启用/禁用自动事务（默认：true）
        options.UnitOfWork.IsAutoTransactionEnabled = true;
        
        // 只读Action关键字
        options.UnitOfWork.ReadOnlyActionKeywords = ["Find", "Get", "Query", "Search"];
    });
```

### UnitOfWork选项

```csharp
var options = new UnitOfWorkOptions
{
    // 隔离级别（默认：ReadCommitted）
    IsolationLevel = IsolationLevel.ReadCommitted,
    
    // 初始化模式（默认：Lazy）
    InitializationMode = TransactionInitializationMode.Lazy,
    
    // 是否只读（默认：false）
    IsReadOnly = false,
    
    // 超时时间（秒）
    Timeout = 30
};

using var uow = await _uowManager.BeginAsync(options);
```

## 最佳实践

### ✅ 推荐做法

1. **优先使用BeginAsync()**
   ```csharp
   // ✅ 好
   using var uow = await _uowManager.BeginAsync();
   
   // ❌ 避免（仅用于向后兼容）
   using var uow = _uowManager.Begin();
   ```

2. **使用using语句确保Dispose**
   ```csharp
   // ✅ 好
   using var uow = await _uowManager.BeginAsync();
   // ... 操作
   await uow.CommitAsync();
   
   // ❌ 差
   var uow = await _uowManager.BeginAsync();
   // ... 操作
   await uow.CommitAsync();
   // 忘记Dispose!
   ```

3. **明确提交或回滚**
   ```csharp
   using var uow = await _uowManager.BeginAsync();
   
   try
   {
       // ... 操作
       await uow.CommitAsync();  // ✅ 明确提交
   }
   catch
   {
       // Dispose时会记录警告
       throw;
   }
   ```

4. **合理使用Attribute**
   ```csharp
   // ✅ 在Controller级别声明，减少重复
   [UnitOfWork]
   public class OrderController : ControllerBase { }
   
   // ✅ Action级别覆盖特殊情况
   [DisableUnitOfWork]
   public async Task<IActionResult> GetCachedData() { }
   ```

### ❌ 反模式

1. **不要在循环中创建多个UoW**
   ```csharp
   // ❌ 差
   foreach (var order in orders)
   {
       using var uow = await _uowManager.BeginAsync();
       await ProcessOrder(order);
       await uow.CommitAsync();
   }
   
   // ✅ 好
   using var uow = await _uowManager.BeginAsync();
   foreach (var order in orders)
   {
       await ProcessOrder(order);
   }
   await uow.CommitAsync();
   ```

2. **不要在UoW外部使用Repository**
   ```csharp
   // ❌ 差
   var order = await _orderRepository.FindAsync(1);  // 没有UoW上下文
   
   // ✅ 好
   using var uow = await _uowManager.BeginAsync();
   var order = await _orderRepository.FindAsync(1);
   // ... 操作
   await uow.CommitAsync();
   ```

3. **避免过长的事务**
   ```csharp
   // ❌ 差
   using var uow = await _uowManager.BeginAsync();
   await DoLotsOfWork();  // 10分钟的操作
   await DoMoreWork();
   await uow.CommitAsync();
   
   // ✅ 好 - 将长操作拆分
   await DoLotsOfWork();  // 不在事务中
   
   using var uow = await _uowManager.BeginAsync();
   await DoCriticalWork();  // 只有关键部分在事务中
   await uow.CommitAsync();
   ```

## 迁移指南

### 从旧版本迁移

如果您使用的是旧版本的MiCake UoW：

1. **更新同步Begin()调用**
   ```csharp
   // 旧代码
   using var uow = _uowManager.Begin();
   
   // 新代码（推荐）
   using var uow = await _uowManager.BeginAsync();
   ```

2. **更新配置选项**
   ```csharp
   // 旧配置
   options.UnitOfWork.IsAutoBeginEnabled = true;
   options.UnitOfWork.IsAutoCommitEnabled = true;
   options.UnitOfWork.IsAutoRollbackEnabled = true;
   
   // 新配置（简化）
   options.UnitOfWork.IsAutoTransactionEnabled = true;
   ```

3. **更新Attribute使用**
   ```csharp
   // 旧代码
   [UnitOfWork(IsEnabled = true, IsReadOnly = true)]
   
   // 新代码（简化）
   [UnitOfWork]  // IsEnabled和IsReadOnly已移除
   ```

## 故障排除

### 常见问题

**Q: "UnitOfWork disposed without being completed" 警告**
```
A: 确保在using块中明确调用CommitAsync()或RollbackAsync()：

using var uow = await _uowManager.BeginAsync();
// ... 操作
await uow.CommitAsync();  // 必须显式提交
```

**Q: 嵌套事务不工作**
```
A: 检查是否使用了requiresNew参数：

// ❌ 这会创建新的root UoW
using var nested = await _uowManager.BeginAsync(requiresNew: true);

// ✅ 这会创建嵌套UoW
using var nested = await _uowManager.BeginAsync();
```

**Q: 性能问题 - 太多小事务**
```
A: 考虑将多个操作合并到一个UoW中，或使用只读优化：

// ✅ 合并多个操作
using var uow = await _uowManager.BeginAsync();
await Operation1();
await Operation2();
await Operation3();
await uow.CommitAsync();

// ✅ 纯查询使用只读选项
var options = UnitOfWorkOptions.ReadOnly;
using var uow = await _uowManager.BeginAsync(options);
```

## 总结

MiCake的工作单元实现提供了：
- ✅ **两阶段注册模式** - 正确的异步支持
- ✅ **灵活的初始化模式** - Lazy和Immediate
- ✅ **嵌套事务支持** - 清晰的作用域
- ✅ **声明式控制** - Attribute简化配置
- ✅ **自动管理** - ASP.NET Core集成

通过合理使用工作单元，您可以：
- 确保数据一致性
- 简化事务管理
- 提高代码可维护性
- 避免常见的异步陷阱

## 更多资源

- [MiCake框架文档](https://github.com/MiCake/MiCake)
- [工作单元模式](https://martinfowler.com/eaaCatalog/unitOfWork.html)
- [Entity Framework Core事务](https://docs.microsoft.com/en-us/ef/core/saving/transactions)
