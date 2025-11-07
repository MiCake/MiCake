# MiCake Disposed Object Issue - 解决方案

## 问题确认

**问题根源**: IntegrationTestBase在构造时创建**两个连续的ServiceProvider**：
1. 第一个用于初始化MiCake framework
2. 第二个加入DbContext后重新构建

第一个ServiceProvider**从未被Dispose**，导致其中的Singleton缓存（如LazyEFSaveChangesLifetime）仍然指向disposed的IServiceScopeFactory。

## 解决方案

### 方案A：正确处理两个ServiceProvider的Dispose（推荐）

```csharp
protected IntegrationTestBase()
{
    var services = new ServiceCollection();
    services.AddLogging();

    _connection = new SqliteConnection("Data Source=:memory:");
    _connection.Open();

    // Build MiCake application FIRST
    _micakeApp = services.AddMiCake<TestModule>()
            .UseAudit()
            .UseEFCore<TestDbContext>()
            .Build();

    // Build and store first ServiceProvider - KEEP REFERENCE
    IServiceProvider firstProvider = services.BuildServiceProvider();
    _firstServiceProvider = firstProvider;  // ← STORE for later disposal

    try
    {
        var micakeApp = firstProvider.GetService<IMiCakeApplication>();
        if (micakeApp is IDependencyReceiver<IServiceProvider> needServiceProvider)
        {
            needServiceProvider.AddDependency(firstProvider);
        }
        micakeApp?.Start().GetAwaiter().GetResult();
    }
    finally
    {
        // Dispose first provider immediately after Start()
        // This ensures no lingering references
        (firstProvider as IDisposable)?.Dispose();
    }

    // Now add DbContext AFTER Framework is initialized
    services.AddDbContext<TestDbContext>(options =>
        options.UseSqlite(_connection));

    // Build final ServiceProvider with DbContext
    ServiceProvider = services.BuildServiceProvider();

    using (var scope = ServiceProvider.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        dbContext.Database.EnsureCreated();
    }

    UowManager = ServiceProvider.GetRequiredService<IUnitOfWorkManager>();

    Fakes.ProductPriceChangedHandler.Clear();
    Fakes.ProductStockDecreasedHandler.Clear();
    Fakes.OrderCompletedHandler.Clear();
}

public virtual void Dispose()
{
    _micakeApp?.ShutDown();
    _connection?.Close();
    _connection?.Dispose();
    (ServiceProvider as IDisposable)?.Dispose();
    // First provider already disposed in constructor
}
```

**优点**:
- 确保第一个provider立即清理
- 清晰的lifecycle管理
- 避免Singleton缓存污染

**缺点**:
- 需要修改MiCake框架的初始化时序

---

### 方案B：使用单一ServiceProvider（最简洁）

```csharp
protected IntegrationTestBase()
{
    var services = new ServiceCollection();
    services.AddLogging();

    _connection = new SqliteConnection("Data Source=:memory:");
    _connection.Open();

    // Register DbContext FIRST, before building MiCake
    services.AddDbContext<TestDbContext>(options =>
        options.UseSqlite(_connection));

    // Then build MiCake with DbContext already available
    _micakeApp = services.AddMiCake<TestModule>()
            .UseAudit()
            .UseEFCore<TestDbContext>()
            .Build();

    // Build ServiceProvider ONCE
    ServiceProvider = services.BuildServiceProvider();

    var micakeApp = ServiceProvider.GetService<IMiCakeApplication>();
    if (micakeApp is IDependencyReceiver<IServiceProvider> needServiceProvider)
    {
        needServiceProvider.AddDependency(ServiceProvider);
    }

    micakeApp?.Start().GetAwaiter().GetResult();

    using (var scope = ServiceProvider.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        dbContext.Database.EnsureCreated();
    }

    UowManager = ServiceProvider.GetRequiredService<IUnitOfWorkManager>();

    Fakes.ProductPriceChangedHandler.Clear();
    Fakes.ProductStockDecreasedHandler.Clear();
    Fakes.OrderCompletedHandler.Clear();
}

public virtual void Dispose()
{
    _micakeApp?.ShutDown();
    _connection?.Close();
    _connection?.Dispose();
    (ServiceProvider as IDisposable)?.Dispose();
}
```

**优点**:
- 只有一个ServiceProvider，lifecycle清晰
- 最简单的实现
- 无污染

**缺点**:
- 需要DbContext在MiCake构建前可用（可能导致顺序问题）

---

### 方案C：使用IAsyncLifetime（xUnit推荐模式）

```csharp
public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected IServiceProvider ServiceProvider { get; private set; }
    protected IUnitOfWorkManager UowManager { get; private set; }

    private SqliteConnection _connection;
    private IMiCakeApplication _micakeApp;

    public async Task InitializeAsync()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        services.AddDbContext<TestDbContext>(options =>
            options.UseSqlite(_connection));

        _micakeApp = services.AddMiCake<TestModule>()
                .UseAudit()
                .UseEFCore<TestDbContext>()
                .Build();

        ServiceProvider = services.BuildServiceProvider();

        var micakeApp = ServiceProvider.GetService<IMiCakeApplication>();
        if (micakeApp is IDependencyReceiver<IServiceProvider> needServiceProvider)
        {
            needServiceProvider.AddDependency(ServiceProvider);
        }

        await micakeApp?.Start();

        using (var scope = ServiceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            dbContext.Database.EnsureCreated();
        }

        UowManager = ServiceProvider.GetRequiredService<IUnitOfWorkManager>();

        Fakes.ProductPriceChangedHandler.Clear();
        Fakes.ProductStockDecreasedHandler.Clear();
        Fakes.OrderCompletedHandler.Clear();
    }

    public async Task DisposeAsync()
    {
        if (_micakeApp != null)
        {
            await _micakeApp.ShutDown();
        }
        (ServiceProvider as IDisposable)?.Dispose();
        _connection?.Close();
        _connection?.Dispose();
    }
}
```

**优点**:
- xUnit官方推荐的async lifecycle management
- 异步初始化/清理
- Per-test isolation

**缺点**:
- 需要所有test classes改成async/await
- 需要xUnit v2.5+

---

## 推荐实施方案

**对于当前codebase**: **方案B（单一ServiceProvider）** 最好
- 改动最小
- 不需要修改test classes
- 最稳定

**步骤**:
1. 将DbContext注册移到MiCake构建之前
2. 移除第一个ServiceProvider的构建
3. 只保留一个BuildServiceProvider()调用

```csharp
// 修改顺序
services.AddDbContext<TestDbContext>(...)  // ← FIRST
_micakeApp = services.AddMiCake<TestModule>()...  // ← THEN
ServiceProvider = services.BuildServiceProvider()  // ← ONCE
```

## 为什么原因分析很重要

这个issue展示了一个常见的.NET DI lifecycle问题：

**Singleton服务持有对特定provider instance的引用** → 当该provider disposed，Singleton仍然活跃 → 后续访问触发ObjectDisposedException

这种问题很难调试因为：
1. 只在多个tests一起运行时出现（test isolation hiding）
2. 错误发生在completely different place（LazyEFSaveChangesLifetime）
3. Stack trace可能指向错误的地方（DbContext SaveChanges）

## 验证修复

修复后运行：
```bash
dotnet test src/tests/MiCake.IntegrationTests/MiCake.IntegrationTests.csproj --no-build -v minimal
```

应该全部通过：
```
通过! - 失败: 0, 通过: 60
```
