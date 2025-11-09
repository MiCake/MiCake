# MiCake Framework v2.0 - 重构规范文档

**文档版本**: 1.0  
**发布日期**: 2025-11-09  
**适用范围**: MiCake Framework v2.0 重构工作  
**目标受众**: 开发团队成员

---

## 文档目的

本文档为MiCake Framework v2.0重构工作提供统一的技术规范和编码标准，确保团队成员在重构过程中保持代码质量和一致性。

---

## 第一部分：架构设计规范

### 1.1 总体架构

**保持现有4层架构，不引入新包**

**层级职责**:
- **MiCake.Core**: 基础设施（模块系统、DI抽象、工具类）
- **MiCake**: DDD实现（Entity、Repository、UoW、Domain Events）
- **MiCake.AspNetCore**: Web集成（中间件、数据包装）
- **MiCake.EntityFrameworkCore**: 数据访问（仓储实现、UoW实现）

### 1.2 依赖规则

**强制规则**:
1. 依赖方向必须向内（Application → AspNetCore/EFCore → DDD → Core）
2. Core层不得依赖任何其他层
3. DDD层不得依赖AspNetCore或EFCore
4. AspNetCore和EFCore可以依赖DDD层，但不得互相依赖

### 1.3 命名空间规范

**标准格式**:
```
MiCake.{Layer}.{Feature}.{SubFeature}
```

**示例**:
```csharp
// ✅ 正确
namespace MiCake.Core.Modularity;
namespace MiCake.DDD.Domain;
namespace MiCake.AspNetCore.DataWrapper;
namespace MiCake.EntityFrameworkCore.Repository;

// ❌ 错误
namespace MiCake.Core.Modularity.Internal;  // 使用Internal而非Internals
namespace MiCake.Abstractions;  // 不创建新的顶层命名空间
```

---

## 第二部分：模块系统规范

### 2.1 模块接口定义

**标准模块接口**:

```csharp
namespace MiCake.Core.Modularity;

/// <summary>
/// MiCake模块接口 - 简化的3方法生命周期
/// </summary>
public interface IMiCakeModule
{
    /// <summary>
    /// 模块是否为框架级别（框架模块不需要用户显式声明依赖）
    /// </summary>
    bool IsFrameworkLevel { get; }
    
    /// <summary>
    /// 是否自动注册服务（通过InjectServiceAttribute或标记接口）
    /// </summary>
    bool IsAutoRegisterServices { get; }

    /// <summary>
    /// 模块描述
    /// </summary>
    string Description { get; }
    
    /// <summary>
    /// 配置服务 - 在应用启动时注册服务到DI容器
    /// </summary>
    /// <param name="context">包含Services、其他模块、应用选项的上下文</param>
    Task ConfigureServices(object context);
    
    /// <summary>
    /// 应用初始化 - 在应用启动完成后执行
    /// </summary>
    /// <param name="context">包含ServiceProvider、其他模块、应用选项的上下文</param>
    Task OnApplicationInitialization(object context);
    
    /// <summary>
    /// 应用关闭 - 在应用关闭时执行清理工作
    /// </summary>
    /// <param name="context">包含ServiceProvider、其他模块、应用选项的上下文</param>
    Task OnApplicationShutdown(object context);
}

/// <summary>
/// 高级模块接口 - 需要精细控制的场景（可选实现）
/// </summary>
public interface IMiCakeModuleAdvanced : IMiCakeModule
{
    /// <summary>
    /// 配置服务前 - 在ConfigureServices之前执行
    /// </summary>
    Task PreConfigureServices(object context);
    
    /// <summary>
    /// 配置服务后 - 在ConfigureServices之后执行
    /// </summary>
    Task PostConfigureServices(object context);
    
    /// <summary>
    /// 初始化前 - 在OnApplicationInitialization之前执行
    /// </summary>
    Task PreInitialization(object context);
    
    /// <summary>
    /// 初始化后 - 在OnApplicationInitialization之后执行
    /// </summary>
    Task PostInitialization(object context);
}
```

### 2.2 模块实现规范

**基类使用**:

```csharp
/// <summary>
/// 模块基类 - 提供默认实现
/// 所有的MiCake模块应继承此基类
/// </summary>
public abstract class MiCakeModule : IMiCakeModule
{
    public virtual string Description => string.Empty;
    public virtual bool IsFrameworkLevel => false;
    public virtual bool IsAutoRegisterServices => true;

    public virtual Task ConfigureServices(object context)
    {
        return Task.CompletedTask;
    }

    public virtual Task OnApplicationInitialization(object context)
    {
        return Task.CompletedTask;
    }

    public virtual Task OnApplicationShutdown(object context)
    {
        return Task.CompletedTask;
    }
}
```

### 2.3 模块依赖规范

**依赖声明**:

```csharp
// ✅ 正确：使用RelyOnAttribute声明依赖
[RelyOn(typeof(MiCakeDomainModule), typeof(MiCakeEFCoreModule))]
public class MyModule : MiCakeModuleBase
{
}

// ❌ 错误：不在代码中手动检查依赖
public class MyModule : MiCakeModuleBase
{
    public override Task ConfigureServices(ModuleConfigServiceContext context)
    {
        // ❌ 不要这样做
        if (!context.MiCakeModules.Any(m => m.ModuleType == typeof(OtherModule)))
            throw new Exception("Requires OtherModule");
    }
}
```

---

## 第三部分：依赖注入规范

### 3.1 服务注册规范

**自动注册 - 使用InjectServiceAttribute**:

```csharp
// ✅ 推荐方式1：使用InjectServiceAttribute
[InjectService(typeof(IOrderService), Lifetime = MiCakeServiceLifetime.Scoped)]
public class OrderService : IOrderService
{
    // ...
}

// ✅ 推荐方式2：使用标记接口
public class OrderService : IOrderService, IScopedService
{
    // ...
}

// ✅ 多个接口注册
[InjectService(
    typeof(IOrderService), 
    typeof(IOrderQueryService),
    Lifetime = MiCakeServiceLifetime.Scoped,
    IncludeSelf = true)]  // 也注册自身类型
public class OrderService : IOrderService, IOrderQueryService
{
    // ...
}

// ❌ 避免：手动注册（除非有特殊需求）
public override Task ConfigureServices(ModuleConfigServiceContext context)
{
    // ❌ 避免手动注册（自动注册更好）
    context.Services.AddScoped<IOrderService, OrderService>();
}
```

**标记接口**:

```csharp
// 三种生命周期标记接口
public interface ITransientService { }  // 瞬态
public interface IScopedService { }     // 作用域
public interface ISingletonService { }  // 单例

// 使用示例
public class MyTransientService : IMyService, ITransientService { }
public class MyScopedService : IMyService, IScopedService { }
public class MySingletonService : IMyService, ISingletonService { }
```

### 3.2 依赖包装器模式

**何时使用**: 当构造函数参数 ≥ 2个且该类将会提供给外界用户（使用MiCake框架的用户）时，使用依赖包装器

**标准实现**:

```csharp
/// <summary>
/// 仓储依赖包装器 - 封装仓储所需的所有依赖
/// </summary>
/// <typeparam name="TDbContext">DbContext类型</typeparam>
public class RepositoryDependencies<TDbContext> where TDbContext : DbContext
{
    public IDbContextAccessor<TDbContext> ContextAccessor { get; }
    public IUnitOfWorkManager UnitOfWorkManager { get; }
    public ILogger Logger { get; }
    public MiCakeEFCoreOptions Options { get; }
    
    public RepositoryDependencies(
        IDbContextAccessor<TDbContext> contextAccessor,
        IUnitOfWorkManager unitOfWorkManager,
        ILogger logger,
        IObjectAccessor<MiCakeEFCoreOptions> optionsAccessor)
    {
        ContextAccessor = contextAccessor 
            ?? throw new ArgumentNullException(nameof(contextAccessor));
        UnitOfWorkManager = unitOfWorkManager 
            ?? throw new ArgumentNullException(nameof(unitOfWorkManager));
        Logger = logger 
            ?? throw new ArgumentNullException(nameof(logger));
        Options = optionsAccessor?.Value 
            ?? throw new ArgumentNullException(nameof(optionsAccessor));
    }
}

/// <summary>
/// 仓储基类 - 使用依赖包装器
/// </summary>
public abstract class RepositoryBase<TDbContext, TEntity, TKey>
    where TDbContext : DbContext
    where TEntity : class, IAggregateRoot<TKey>
{
    protected readonly RepositoryDependencies<TDbContext> Dependencies;
    
    protected RepositoryBase(RepositoryDependencies<TDbContext> dependencies)
    {
        Dependencies = dependencies 
            ?? throw new ArgumentNullException(nameof(dependencies));
    }
    
    // 使用包装器访问依赖
    protected IDbContextAccessor<TDbContext> ContextAccessor 
        => Dependencies.ContextAccessor;
    protected IUnitOfWorkManager UnitOfWorkManager 
        => Dependencies.UnitOfWorkManager;
    protected ILogger Logger 
        => Dependencies.Logger;
}

/// <summary>
/// 用户自定义仓储 - 构造函数简洁明了
/// </summary>
public class OrderRepository : RepositoryBase<OrderDbContext, Order, int>
{
    // ✅ 只需一个参数
    public OrderRepository(RepositoryDependencies<OrderDbContext> dependencies)
        : base(dependencies)
    {
    }
    
    // 业务方法...
}
```

**依赖包装器注册**:

```csharp
// 在模块中注册依赖包装器
public override Task ConfigureServices(ModuleConfigServiceContext context)
{
    // 注册依赖包装器为Scoped（与DbContext生命周期一致）
    context.Services.AddScoped<RepositoryDependencies<OrderDbContext>>();
    
    return Task.CompletedTask;
}
```

### 3.3 避免服务定位器

**规则**: 禁止在构造函数中注入`IServiceProvider`来获取依赖

```csharp
// ❌ 禁止：服务定位器反模式
public class MyService
{
    public MyService(IServiceProvider serviceProvider)
    {
        _dependency = serviceProvider.GetRequiredService<IDependency>();
        // 隐藏了真实依赖，难以测试
    }
}

// ✅ 正确：显式依赖注入
public class MyService
{
    private readonly IDependency _dependency;
    
    public MyService(IDependency dependency)
    {
        _dependency = dependency 
            ?? throw new ArgumentNullException(nameof(dependency));
    }
}

// ✅ 正确：使用依赖包装器（多依赖情况）
public class MyService
{
    private readonly MyServiceDependencies _dependencies;
    
    public MyService(MyServiceDependencies dependencies)
    {
        _dependencies = dependencies 
            ?? throw new ArgumentNullException(nameof(dependencies));
    }
}
```

**例外情况**: 仅在以下场景允许使用`IServiceProvider`:
1. 工厂模式实现
2. 动态服务解析（如插件系统）
3. 生命周期管理（如创建Scope）

---

## 第四部分：DDD设计规范

### 4.1 实体和聚合根

**实体基类**:

```csharp
/// <summary>
/// 实体基类
/// </summary>
public abstract class Entity<TKey> : IEntity<TKey> where TKey : notnull
{
    private readonly List<IDomainEvent> _domainEvents = new();
    
    /// <summary>
    /// 实体标识 - 使用init确保不可变
    /// </summary>
    public TKey Id { get; init; } = default!;
    
    /// <summary>
    /// 领域事件集合（只读）
    /// </summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents 
        => _domainEvents.AsReadOnly();
    
    /// <summary>
    /// 添加领域事件
    /// </summary>
    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Add(domainEvent);
    }
    
    /// <summary>
    /// 清除领域事件
    /// </summary>
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
    
    // 相等比较实现...
}

/// <summary>
/// 聚合根基类
/// </summary>
public abstract class AggregateRoot<TKey> : Entity<TKey>, IAggregateRoot<TKey>
    where TKey : notnull
{

}
```

### 4.2 值对象

**使用record类型**:

```csharp
// ✅ 推荐：使用record类型（简洁）
public record Money(decimal Amount, string Currency);
public record Address(string Street, string City, string ZipCode, string Country);
public record DateRange(DateTime Start, DateTime End);

// ✅ 也可以：使用传统ValueObject基类（复杂场景）
public class ComplexValueObject : ValueObject
{
    public string Property1 { get; }
    public int Property2 { get; }
    
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Property1;
        yield return Property2;
    }
}

// 使用示例
public class Order : AggregateRoot<int>
{
    public Money TotalAmount { get; private set; }
    public Address ShippingAddress { get; private set; }
    
    public Order(Money totalAmount, Address shippingAddress)
    {
        TotalAmount = totalAmount;
        ShippingAddress = shippingAddress;
    }
}
```

### 4.3 仓储模式

**仓储接口**:

```csharp
/// <summary>
/// 仓储接口 - 标准实现
/// </summary>
public interface IRepository<TEntity, TKey>
    where TEntity : class, IAggregateRoot<TKey>
    where TKey : notnull
{
    // 查询方法
    Task<TEntity?> FindAsync(TKey id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(TKey id, CancellationToken cancellationToken = default);
    IQueryable<TEntity> Query();  // 返回IQueryable支持LINQ
    
    // 异步流（大数据集）
    IAsyncEnumerable<TEntity> GetAllAsync(
        CancellationToken cancellationToken = default);
    
    // 命令方法
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task RemoveAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task RemoveByIdAsync(TKey id, CancellationToken cancellationToken = default);
}
```

**仓储实现规范**:

```csharp
public class EFRepository<TDbContext, TEntity, TKey> 
    : IRepository<TEntity, TKey>
    where TDbContext : DbContext
    where TEntity : class, IAggregateRoot<TKey>
    where TKey : notnull
{
    private readonly RepositoryDependencies<TDbContext> _dependencies;
    
    public EFRepository(RepositoryDependencies<TDbContext> dependencies)
    {
        _dependencies = dependencies;
    }
    
    public async Task<TEntity?> FindAsync(
        TKey id, 
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<TEntity>()
            .FindAsync(new object[] { id }, cancellationToken)
            .ConfigureAwait(false);
    }
}
```

---

## 第五部分：代码风格规范

### 5.1 命名约定

**类和接口**:
```csharp
// ✅ 正确
public class OrderService { }
public interface IOrderService { }
public abstract class RepositoryBase { }
public record Money { }
public enum OrderStatus { }

// ❌ 错误
public class orderService { }       // 首字母小写
public interface OrderService { }   // 接口缺少I前缀
public class RepositoryBaseClass { }  // 不要使用Class后缀
```

**方法和属性**:
```csharp
// ✅ 正确：PascalCase
public string OrderNumber { get; set; }
public void PlaceOrder() { }
public async Task<Order> GetOrderAsync(int id) { }

// ❌ 错误
public string orderNumber { get; set; }  // camelCase
public void place_order() { }            // snake_case
public async Task<Order> GetOrder(int id) { }  // 异步方法缺少Async后缀
```

**参数和局部变量**:
```csharp
// ✅ 正确：camelCase
public void ProcessOrder(int orderId, string customerName)
{
    var orderService = GetService();
    var result = orderService.Process();
}

// ❌ 错误
public void ProcessOrder(int OrderId, string CustomerName)  // PascalCase
{
    var OrderService = GetService();  // PascalCase
}
```

**私有字段**:
```csharp
// ✅ 正确：_camelCase（下划线前缀）
private readonly IOrderService _orderService;
private int _count;

// ❌ 错误
private readonly IOrderService orderService;   // 缺少下划线
private readonly IOrderService m_orderService; // 不使用m_前缀
private readonly IOrderService _OrderService;  // PascalCase
```

**常量**:
```csharp
// ✅ 正确：PascalCase
public const string DefaultCurrency = "USD";
private const int MaxRetryCount = 3;

// ❌ 错误
public const string DEFAULT_CURRENCY = "USD";  // UPPER_SNAKE_CASE（仅C风格）
public const string defaultCurrency = "USD";   // camelCase
```

### 5.2 异步编程规范

**异步方法命名**:
```csharp
// ✅ 正确：Async后缀
public async Task<Order> GetOrderAsync(int id) { }
public async Task SaveAsync() { }
public async ValueTask DisposeAsync() { }

// ❌ 错误
public async Task<Order> GetOrder(int id) { }  // 缺少Async后缀
```

**ConfigureAwait使用**:
```csharp
// ✅ 正确：库代码中使用ConfigureAwait(false)
public async Task<Order> GetOrderAsync(int id)
{
    var dbContext = await GetDbContextAsync()
        .ConfigureAwait(false);  // ✅ 库代码必须使用
    
    return await dbContext.Orders
        .FindAsync(id)
        .ConfigureAwait(false);  // ✅
}

// ❌ 错误：库代码中不使用ConfigureAwait
public async Task<Order> GetOrderAsync(int id)
{
    var dbContext = await GetDbContextAsync();  // ❌ 缺少ConfigureAwait
    return await dbContext.Orders.FindAsync(id);  // ❌
}
```

**避免阻塞式异步调用**:
```csharp
// ❌ 禁止：阻塞式异步调用（可能死锁）
public void SyncMethod()
{
    var result = GetOrderAsync().Result;  // ❌ 禁止使用.Result
    GetOrderAsync().Wait();  // ❌ 禁止使用.Wait()
    GetOrderAsync().GetAwaiter().GetResult();  // ❌ 禁止
}

// ✅ 正确：保持异步调用链
public async Task AsyncMethod()
{
    var result = await GetOrderAsync().ConfigureAwait(false);
}
```

### 5.3 文件组织规范

**文件命名**: 一个文件一个主要类型，文件名与类型名一致

```
✅ OrderService.cs          → public class OrderService
✅ IOrderService.cs         → public interface IOrderService
✅ OrderStatus.cs           → public enum OrderStatus
✅ Money.cs                 → public record Money

❌ Services.cs              → 包含多个服务类
❌ Interfaces.cs            → 包含多个接口
```

**命名空间与文件夹对应**:
```
src/framework/MiCake/DDD/Domain/
├── Entity.cs               → namespace MiCake.DDD.Domain;
├── AggregateRoot.cs        → namespace MiCake.DDD.Domain;
└── ValueObject.cs          → namespace MiCake.DDD.Domain;
```

**using指令顺序**:
```csharp
// 1. System命名空间
using System;
using System.Collections.Generic;
using System.Linq;

// 2. Microsoft和其他第三方命名空间
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// 3. MiCake命名空间
using MiCake.Core;
using MiCake.DDD.Domain;

// 空行

namespace MiCake.MyNamespace;

public class MyClass
{
}
```

### 5.4 注释规范

**XML文档注释 - 必须** （注释必须为英文）:

```csharp
/// <summary>
/// Order service - Handles order-related business logic
/// </summary>
public class OrderService : IOrderService
{
    /// <summary>
    /// Creates a new order
    /// </summary>
    /// <param name="command">Create order command, contains order details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Newly created order ID</returns>
    /// <exception cref="ArgumentNullException">When command is null</exception>
    /// <exception cref="InvalidOperationException">When order data is invalid</exception>
    public async Task<int> CreateOrderAsync(
        CreateOrderCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        
        // 实现...
    }
}
```

**代码注释 （英文注释） - 适度使用**:

```csharp
// ✅ Good comment: Explains complex logic
// Use topological sort instead of depth-first search, as it needs to detect circular dependencies
var sorted = TopologicalSort(modules);

// ❌ Bad comment: Repeats code meaning
// Create order
var order = new Order();

// ✅ Good comment: Marks temporary solution
// TODO: Optimize performance - Consider using caching (Issue #123)
// HACK: Temporary solution, waiting for upstream library fix
// WARNING: This code may have issues in high-concurrency scenarios
```

### 5.5 异常处理规范

**参数验证**:
```csharp
public void ProcessOrder(Order order, int quantity)
{
    // ✅ 使用ArgumentNullException.ThrowIfNull（.NET 6+）
    ArgumentNullException.ThrowIfNull(order);
    ArgumentNullException.ThrowIfNull(order.Customer);
    
    // ✅ 使用ArgumentException验证参数有效性
    if (quantity <= 0)
        throw new ArgumentException(
            "Quantity must be greater than zero", 
            nameof(quantity));
    
    // ❌ 不要使用通用Exception
    // throw new Exception("Invalid parameter");
}
```

**业务异常**:
```csharp
// ✅ 定义特定的业务异常
public class DomainException : Exception
{
    public string ErrorCode { get; }
    
    public DomainException(string message, string errorCode = null)
        : base(message)
    {
        ErrorCode = errorCode;
    }
}

public class OrderNotFoundException : DomainException
{
    public OrderNotFoundException(int orderId)
        : base($"Order {orderId} not found", "ORDER_NOT_FOUND")
    {
    }
}

// 使用
public async Task<Order> GetOrderAsync(int id)
{
    var order = await _repository.FindAsync(id);
    if (order == null)
        throw new OrderNotFoundException(id);  // ✅ 特定异常
    
    return order;
}
```

---

## 第六部分：性能优化规范

### 6.1 反射优化

**使用编译表达式树替代Activator.CreateInstance**:

```csharp
/// <summary>
/// 快速激活器 - 使用编译表达式树（10x faster）
/// </summary>
public static class CompiledActivator
{

}

// 使用
// ❌ 慢：Activator.CreateInstance(moduleType)
// ✅ CompiledActivator.CreateInstance<MiCakeModule>(moduleType)
```

### 6.2 缓存策略

**合理使用缓存**:

```csharp
// ✅ 正确：缓存不变的计算结果
public class ModuleManager
{
    private readonly ConcurrentDictionary<Type, ModuleDescriptor> _descriptorCache = new();
    
    public ModuleDescriptor GetDescriptor(Type moduleType)
    {
        return _descriptorCache.GetOrAdd(moduleType, type =>
        {
            // 反射获取模块信息（只计算一次）
            return BuildDescriptor(type);
        });
    }
}

// ❌ 错误：缓存会变化的数据
// private readonly Dictionary<int, Order> _orderCache = new();  // ❌ 订单会变化
```

### 6.3 字符串处理

**使用StringBuilder拼接大量字符串**:

```csharp
// ✅ 正确：大量拼接使用StringBuilder
var sb = new StringBuilder();
foreach (var item in items)
{
    sb.AppendLine($"Item: {item.Name}, Price: {item.Price}");
}
var result = sb.ToString();

// ❌ 错误：循环中使用+拼接
string result = "";
foreach (var item in items)
{
    result += $"Item: {item.Name}, Price: {item.Price}\n";  // ❌ 性能差
}
```

---

## 第七部分：测试规范

### 7.1 单元测试结构

**测试类命名**:
```csharp
// 格式：{被测试类}Tests
public class OrderServiceTests { }
public class RepositoryBaseTests { }

// ❌ 错误
public class TestOrderService { }
public class OrderServiceTest { }  // 单数
```

**测试方法命名**:
```csharp
// 格式：{方法名}_{场景}_{预期结果}
public class OrderServiceTests
{
    [Fact]
    public void CreateOrder_WithValidData_ShouldSucceed() { }
    
    [Fact]
    public void CreateOrder_WithNullCommand_ShouldThrowArgumentNullException() { }
    
    [Fact]
    public async Task GetOrder_WithNonExistentId_ShouldReturnNull() { }
}
```

**AAA模式**:
```csharp
[Fact]
public async Task CreateOrder_WithValidData_ShouldSucceed()
{
    // Arrange（准备）
    var dependencies = CreateRepositoryDependencies();
    var repository = new OrderRepository(dependencies);
    var order = new Order { /* ... */ };
    
    // Act（执行）
    await repository.AddAsync(order);
    await unitOfWork.CommitAsync();
    
    // Assert（断言）
    var saved = await repository.FindAsync(order.Id);
    Assert.NotNull(saved);
    Assert.Equal(order.OrderNumber, saved.OrderNumber);
}
```

### 7.2 Mock使用规范

```csharp
[Fact]
public async Task OrderService_Should_Call_Repository()
{
    // Arrange
    var mockRepository = new Mock<IRepository<Order, int>>();
    mockRepository
        .Setup(r => r.AddAsync(It.IsAny<Order>(), default))
        .Returns(Task.CompletedTask);
    
    var service = new OrderService(mockRepository.Object);
    
    // Act
    await service.CreateOrderAsync(new CreateOrderCommand());
    
    // Assert
    mockRepository.Verify(
        r => r.AddAsync(It.IsAny<Order>(), default),
        Times.Once);
}
```

---

## 第八部分：检查清单

### 8.1 代码审查清单

**架构和设计**:
- [ ] 遵循4层架构，依赖方向正确
- [ ] 没有引入新的顶层包

**模块系统**:
- [ ] 模块继承自MiCakeModule
- [ ] 使用RelyOnAttribute声明依赖
- [ ] 实现了3个核心方法（ConfigureServices、OnApplicationInitialization、OnApplicationShutdown）
- [ ] 高级场景实现IMiCakeModuleAdvanced接口

**依赖注入**:
- [ ] 使用InjectServiceAttribute或标记接口进行服务注册
- [ ] ≥2个依赖且对外暴露时使用依赖包装器模式
- [ ] 没有使用IServiceProvider作为构造函数参数（除非工厂模式）
- [ ] 所有依赖都显式声明

**DDD设计**:
- [ ] 实体继承自Entity<TKey>或AggregateRoot<TKey>
- [ ] 值对象使用record类型或继承ValueObject
- [ ] 仓储只操作聚合根
- [ ] 并发控制由用户根据需要添加（不强制）

**代码风格**:
- [ ] 类名和接口名使用PascalCase
- [ ] 方法和属性使用PascalCase
- [ ] 参数和局部变量使用camelCase
- [ ] 私有字段使用_camelCase
- [ ] 异步方法有Async后缀
- [ ] 库代码中使用ConfigureAwait(false)

**性能**:
- [ ] 没有阻塞式异步调用（.Result、.Wait()、.GetAwaiter().GetResult()）
- [ ] 使用CompiledActivator替代Activator.CreateInstance
- [ ] 适当使用缓存
- [ ] 大量字符串拼接使用StringBuilder

**测试**:
- [ ] 编写了单元测试
- [ ] 测试覆盖率 >80%
- [ ] 测试方法遵循命名约定
- [ ] 使用AAA模式

**文档**:
- [ ] 公共API有XML文档注释
- [ ] 复杂逻辑有代码注释

---

## 附录A：常见问题

**Q: 何时使用IMiCakeModuleAdvanced？**  
A: 只有在需要精细控制配置顺序时（如基础设施模块、配置覆盖）才使用高级接口。大多数模块只需实现标准的3个方法。

**Q: 是否需要为所有类创建依赖包装器？**  
A: 不需要。只有当该类需要提供给最终的用户使用且构造函数参数 ≥2个时才考虑使用依赖包装器；若只是MiCake内部框架使用，则不需要。

**Q: 如何处理遗留代码？**  
A: 移除不需要的遗留代码，重构复杂或不符合规范的代码以符合本规范。确保所有新代码均遵循规范。并更新相关单元测试。

**Q: ConfigureAwait(false)什么时候必须使用？**  
A: 在所有库代码（MiCake框架本身）的异步方法中必须使用。应用层代码（用户代码）可以不使用。

---

**文档版本**: 1.0  
**最后更新**: 2025-11-09  
**维护者**: MiCake Framework Team
