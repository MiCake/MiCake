# MiCake Framework - 重构方案 v2.0 变更说明

**更新日期**: 2025-11-09  
**版本**: 2.0  

## 重要变更摘要

基于反馈意见，v2.0对重构方案进行了以下重要调整：

### 1. 保持现有4层架构 ✅

**决策**: 不引入新的NuGet包（MiCake.Abstractions、MiCake.Hosting）

**理由**:
- 现有4层架构（MiCake.Core、MiCake、MiCake.AspNetCore、MiCake.EntityFrameworkCore）已经足够清晰
- 便于用户理解和使用NuGet包
- 避免过度复杂化

**保持的架构**:
```
MiCake.Core           → 基础层
MiCake                → DDD实现层  
MiCake.AspNetCore     → Web集成层
MiCake.EntityFrameworkCore → 数据访问层
```

### 2. 模块系统保持框架无关 ✅

**决策**: 模块接口不依赖AspNetCore

**调整后的设计**:
```csharp
// ❌ 原方案（被拒绝）
public interface IMiCakeModule
{
    void ConfigureServices(IServiceCollection services, IConfiguration configuration);
    void ConfigureApplication(IApplicationBuilder app);  // 依赖AspNetCore
}

// ✅ 新方案（采纳）
public interface IMiCakeModule
{
    bool IsFrameworkLevel { get; }
    bool IsAutoRegisterServices { get; }
    
    // 保持现有签名，通过Context提供所需信息
    Task ConfigureServices(ModuleConfigServiceContext context);
    Task OnApplicationInitialization(ModuleLoadContext context);
    Task OnApplicationShutdown(ModuleLoadContext context);
}
```

**优势**:
- 模块可用于任何.NET应用（Console、WPF、Worker Service等）
- ModuleConfigServiceContext提供Services、配置和MiCake上下文
- 保持框架的通用性和可移植性

### 3. 保留InjectServiceAttribute ✅

**决策**: 不替换为ServiceAttribute，避免破坏性变更

**保持现有设计**:
```csharp
// 保持
[InjectService(typeof(IMyService), Lifetime = MiCakeServiceLifetime.Scoped)]
public class MyService : IMyService { }

// 或使用标记接口
public class MyService : IMyService, IScopedService { }
```

**增强计划**:
- 保留InjectServiceAttribute的所有功能
- 优化自动服务注册性能（使用源代码生成器）
- 添加更多文档和示例

### 4. 移除ConcurrencyToken ✅

**决策**: 不在AggregateRoot中强制添加ConcurrencyToken

**理由**:
- EF Core已经提供了乐观并发控制机制
- 用户可以根据需要自行在聚合根中添加
- MiCake不应该强制特定的并发控制策略

**用户可选实现**:
```csharp
// 用户可以自行添加
public class Order : AggregateRoot<int>
{
    [Timestamp]  // EF Core特性
    public byte[] RowVersion { get; set; }
    
    // 或
    [ConcurrencyCheck]
    public string ConcurrencyToken { get; set; }
}
```

### 5. 依赖包装器模式 ✅

**决策**: 对于多依赖的构造函数，使用依赖包装器

**新设计**:
```csharp
// ❌ 原方案：直接注入所有依赖（过多参数）
public class RepositoryBase<TDbContext, TEntity, TKey>
{
    public RepositoryBase(
        IDbContextAccessor<TDbContext> contextAccessor,
        IUnitOfWorkManager unitOfWorkManager,
        ILogger<RepositoryBase> logger,
        IObjectAccessor<Options> options,
        // ... 更多依赖
    ) { }
}

// ✅ 新方案：使用依赖包装器
public class RepositoryDependencies<TDbContext>
{
    public IDbContextAccessor<TDbContext> ContextAccessor { get; }
    public IUnitOfWorkManager UnitOfWorkManager { get; }
    public ILogger Logger { get; }
    
    public RepositoryDependencies(
        IDbContextAccessor<TDbContext> contextAccessor,
        IUnitOfWorkManager unitOfWorkManager,
        ILogger logger)
    {
        ContextAccessor = contextAccessor;
        UnitOfWorkManager = unitOfWorkManager;
        Logger = logger;
    }
}

public class RepositoryBase<TDbContext, TEntity, TKey>
{
    protected readonly RepositoryDependencies<TDbContext> Dependencies;
    
    public RepositoryBase(RepositoryDependencies<TDbContext> dependencies)
    {
        Dependencies = dependencies;
    }
}

// 用户继承时更简洁
public class MyRepository : RepositoryBase<MyDbContext, MyEntity, int>
{
    public MyRepository(RepositoryDependencies<MyDbContext> dependencies)
        : base(dependencies)
    {
    }
}
```

**优势**:
- 构造函数参数减少
- 继承时更简洁
- 相关依赖逻辑组织在一起
- 便于测试（mock一个对象而不是多个）

## 核心重构要点

基于以上调整，重构方案的核心要点为：

### 1. 模块系统优化

**从8个生命周期简化为3个核心方法**:
```csharp
public interface IMiCakeModule
{
    // 1. 配置服务（注册到DI）
    Task ConfigureServices(ModuleConfigServiceContext context);
    
    // 2. 应用初始化（启动时）
    Task OnApplicationInitialization(ModuleLoadContext context);
    
    // 3. 应用关闭（关闭时）
    Task OnApplicationShutdown(ModuleLoadContext context);
}

// 高级场景可实现扩展接口
public interface IMiCakeModuleAdvanced : IMiCakeModule
{
    Task PreConfigureServices(ModuleConfigServiceContext context);
    Task PostConfigureServices(ModuleConfigServiceContext context);
    Task PreInitialization(ModuleLoadContext context);
    Task PostInitialization(ModuleLoadContext context);
}
```

### 2. MiCakeApplication重构

**修复状态管理bug**:
```csharp
// ❌ 当前问题
private readonly bool _isInitialized = false;  // readonly导致无法更新

// ✅ 解决方案：使用状态机
public enum ApplicationState
{
    NotInitialized,
    Initialized,
    Started,
    Shutdown
}

private ApplicationState _state = ApplicationState.NotInitialized;
```

**职责分离和清晰化**:
- 状态管理
- 生命周期控制
- 模块管理
- ServiceProvider管理

### 3. 依赖注入优化

**消除服务定位器**:
```csharp
// ❌ 当前
public EFRepositoryBase(IServiceProvider serviceProvider)
{
    _contextFactory = serviceProvider.GetRequiredService<...>();
    // 隐藏依赖
}

// ✅ 改进：使用依赖包装器
public EFRepositoryBase(RepositoryDependencies<TDbContext> dependencies)
{
    _dependencies = dependencies;
    // 依赖明确
}
```

### 4. DDD设计优化

**值对象使用record**:
```csharp
// 简化值对象实现
public record Money(decimal Amount, string Currency);
public record Address(string Street, string City, string ZipCode);
```

**仓储接口简化**:
```csharp
public interface IRepository<TEntity, TKey>
{
    // 查询
    Task<TEntity?> FindAsync(TKey id, CancellationToken cancellationToken = default);
    IQueryable<TEntity> Query();  // 返回IQueryable
    IAsyncEnumerable<TEntity> GetAllAsync(CancellationToken cancellationToken = default);
    
    // 命令
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task RemoveAsync(TEntity entity, CancellationToken cancellationToken = default);
}

// SaveChangesAsync由UoW统一管理
```

### 5. 性能优化

**编译表达式树替代反射**:
```csharp
// 10x性能提升
public static class FastActivator
{
    private static readonly ConcurrentDictionary<Type, Func<object[], object>> _cache = new();
    
    public static T CreateInstance<T>(params object[] args)
    {
        var factory = _cache.GetOrAdd(typeof(T), BuildFactory);
        return (T)factory(args);
    }
    
    private static Func<object[], object> BuildFactory(Type type)
    {
        // 使用Expression.Lambda编译
        // ...
    }
}
```

**消除阻塞式异步调用**:
```csharp
// ❌ 当前
_cachedDbContext = _contextFactory.GetDbContextAsync().GetAwaiter().GetResult();

// ✅ 改进：完全异步
protected async Task<TDbContext> GetDbContextAsync(CancellationToken cancellationToken = default)
{
    return await _contextFactory.GetDbContextAsync(cancellationToken).ConfigureAwait(false);
}
```

## 实施策略

### 向后兼容

v2.0将提供兼容层，支持现有代码平滑迁移：

```csharp
// 兼容旧的MiCakeModule
[Obsolete("Use MiCakeModuleBase and implement IMiCakeModule instead")]
public abstract class LegacyMiCakeModule : MiCakeModuleBase
{
    // 保留所有8个生命周期方法
    // 内部转换为新的3个方法调用
}
```

### 迁移路径

1. **Phase 1**: 发布v2.0-preview，包含新API和兼容层
2. **Phase 2**: 提供迁移工具和详细文档
3. **Phase 3**: v2.0正式版，标记旧API为Obsolete
4. **Phase 4**: v3.0移除已弃用API

## 总结

v2.0重构方案的核心调整：

1. ✅ **保持4层架构** - 不增加新包
2. ✅ **框架无关的模块** - 不依赖AspNetCore
3. ✅ **保留InjectServiceAttribute** - 避免破坏性变更
4. ✅ **移除强制ConcurrencyToken** - 用户可选
5. ✅ **依赖包装器模式** - 简化构造函数

这些调整使重构方案更加务实、可行，同时保持了原有的改进目标：

- 🏗️ 更好的模块化
- 🔧 更清晰的依赖注入
- 📦 更现代的DDD设计
- ⚡ 更高的性能
- 📚 更易于使用

