# MiCake v2 变更与迁移指南

> **适用范围**: MiCake UoW v2（`20260806-uow-transaction-reliability`）与拦截器自动安装
> （`20260807-interceptor-di-registration`）
> **分支**: `uow_enhance` — 发布时间窗：2026-08
> **完整架构参考**: [`docs/UoW-v2-Architecture-and-Usage.md`](UoW-v2-Architecture-and-Usage.md)

MiCake v2 将 **Unit of Work 确立为唯一持久化所有者**，并改为**自动安装** EF Core 拦截器。
升级后请对照下列变更逐项迁移：

1. [移除 `IRepository.SaveChangesAsync`](#11-移除-savechangesasync)
2. [移除 `AddAndReturnAsync` 与 `saveNow` 参数](#12-移除-addandreturnasync-与-savenow)
3. [移除 `ClearChangeTrackingAsync`](#13-移除-clearchangetrackingasync)
4. [`UpdateAsync` 语义：完整替换](#14-updateasync-语义变更)
5. [`DeleteByIdAsync` 语义：跟踪式删除](#15-deletebyidasync-语义变更)
6. [`requiresNew` 改为回调执行](#21-beginasyncrequiresnew-true-已移除)
7. [移除 `PersistenceStrategy` / `OptimizeForSingleWrite`](#22-移除-persistencestrategy)
8. [移除 `Timeout`](#23-移除-timeout)
9. [`IDbContextWrapper` 改为 `IUnitOfWorkResource`](#24-idbcontextwrapper-已移除)
10. [新增 `FlushAsync`、保存点、事件钩子](#25-新增能力)
11. [`UnitOfWorkAttribute` 精简](#31-unitofworkattribute-精简)
12. [`IsUowEnabled` 改为 `[DisableUnitOfWork]`](#32-isuowenabled-已移除)
13. [只读 action 推断改为 opt-in](#33-只读-action-名称推断改为-opt-in)
14. [上下文工厂接口合并](#41-上下文工厂接口合并)
15. [`BypassUnitOfWorkCheck` 更名](#42-bypassunitofworkcheck-更名)
16. [拦截器自动安装（`UseMiCakeInterceptors` 移除）](#43-拦截器自动安装)
17. [行为变更：无 UoW 写入放行等](#5-行为变更)

若要快速完成迁移，可直接跳转到[迁移清单](#6-迁移清单)。

---

## 1. 仓储 API

### 1.1 移除 `SaveChangesAsync`

仓储方法不再自行保存或提交。变更仅修改 UoW 的跟踪状态，持久化由 UoW 统一拥有。
将每个 `repo.SaveChangesAsync()` 替换为环境 UoW 上的 `CommitAsync()`：

```csharp
// 迁移前
await _bookRepository.AddAsync(book);
await _bookRepository.SaveChangesAsync();   // 竞争性持久化边界

// 迁移后
await _bookRepository.AddAsync(book);
await _unitOfWork.CommitAsync();            // 在 UoW 边界统一提交
```

### 1.2 移除 `AddAndReturnAsync` 与 `saveNow`

原来使用 `AddAndReturnAsync` 的地方，**建议迁移到 v2 新增的
`EFRepository<TDbContext, TAggregateRoot, TKey>.AddAndGetIdAsync(...)`**——它保持了
「添加 + 立即获取数据库生成键」的原有语义：添加聚合根后自动调用 `FlushAsync`
填充生成键并返回 `TKey`。flush 发生在环境可写 UoW 事务内，**不提交**，
仅在 UoW 提交后持久化；需要环境可写 UoW，否则抛出 `InvalidOperationException`：

```csharp
// 迁移前
var book = await _bookRepository.AddAndReturnAsync(new Book { Title = "x" });

// 迁移后（推荐）：语义等价，返回生成的键
var id = await _efRepo.AddAndGetIdAsync(new Book { Title = "x" });   // EFRepository 实现类
await _unitOfWork.CommitAsync();   // 提交后数据持久化
```

> **注意**：`AddAndGetIdAsync` 现已加入 `IRepository<TAggregateRoot, TKey>` 接口，
> 通过接口注入也可直接调用。其行为与便捷路径完全一致（添加 + `FlushAsync` 填充生成键，
> 不提交；需要环境可写 UoW）。接口注入场景亦可使用 `AddAsync` + `IUnitOfWork.FlushAsync()`
> 组合，行为等价：
>
> ```csharp
> // 迁移后（接口注入场景，与 AddAndGetIdAsync 等价）
> var book = new Book { Title = "x" };
> await _bookRepository.AddAsync(book);
> await _unitOfWork.FlushAsync();   // 生成键，book.Id 已填充，事务未提交
> // ...后续操作，最后统一 CommitAsync()
> ```

`AddAsync(..., saveNow: true)` 中的 `saveNow` 参数同样移除，按上述方式处理。

### 1.3 移除 `ClearChangeTrackingAsync`

不再提供。清理变更跟踪属于 EF Core 关注点，需要时通过 `DbContext` 直接分离条目。

### 1.4 `UpdateAsync` 语义变更

未跟踪实例的 `UpdateAsync` 现在是**完整分离聚合替换**：写入实例的全部属性。
配置的并发令牌被保留，因此过期实例会以 `DbUpdateConcurrencyException` 暴露冲突，
而不再静默覆盖：

```csharp
// 迁移前：部分更新，静默覆盖并发变更
await _repo.UpdateAsync(detachedBook);

// 迁移后：完整替换；并发冲突在 flush/commit 时抛出 DbUpdateConcurrencyException
await _repo.UpdateAsync(detachedBook);
await _unitOfWork.CommitAsync();
```

> **注意**：load-and-modify 仍是首选工作流；并发冲突现在在 `FlushAsync` / `CommitAsync`
> 时抛出，而不是在 `UpdateAsync` 调用时。

### 1.5 `DeleteByIdAsync` 语义变更

现在先加载聚合到稳定的 UoW 上下文，再执行**跟踪式删除**——软删除、审计、
领域事件、回滚语义与 `DeleteAsync` 完全一致：

```csharp
await _repo.DeleteByIdAsync(id);   // 语义：load → tracked delete → UoW 提交后生效
await _unitOfWork.CommitAsync();
```

需要绕过生命周期立即物理删除时，改用显式物理删除 API
（见 [5.8 物理/批量操作](#58-物理批量操作显式化)）。

---

## 2. Unit of Work API

### 2.1 `BeginAsync(requiresNew: true)` 已移除

`requiresNew` 布尔参数被**隔离回调执行**取代。`ExecuteRequiresNewAsync` 创建独立 DI 作用域，
回调必须从传入的 `IServiceProvider` 解析仓储/DbContext；提交、回滚、释放自动完成：

```csharp
// 迁移前
var uow = await _unitOfWorkManager.BeginAsync(requiresNew: true);
var repo = _bookRepository;                    // 外层作用域服务——错误
await repo.AddAsync(book);
await uow.CommitAsync();

// 迁移后
await _unitOfWorkManager.ExecuteRequiresNewAsync(async (sp, ct) =>
{
    var repo = sp.GetRequiredService<IRepository<Book, int>>();  // 从回调 provider 解析
    await repo.AddAsync(book);
    // 成功自动提交，失败自动回滚，作用域自动释放
});
```

> **注意**：捕获外层 scoped 服务会触发所有权校验失败；无外层 UoW 时调用会抛出
> `InvalidOperationException`。后台作业等无环境 UoW 的场景改用
> `IStandaloneUnitOfWorkExecutor`。

### 2.2 移除 `PersistenceStrategy`

`PersistenceStrategy` / `OptimizeForSingleWrite` 已移除。每个可写 UoW 都使用**显式事务**：

```csharp
// 迁移前
await _uowManager.BeginAsync(new UnitOfWorkOptions { PersistenceStrategy = PersistenceStrategy.OptimizeForSingleWrite });

// 迁移后
await _uowManager.BeginAsync();   // 默认即可：Lazy 激活显式事务
// 需要提前激活事务时：UnitOfWorkOptions.Immediate
```

> **原因**：`OptimizeForSingleWrite` 允许 EF 隐式事务在 post-save 生命周期处理完成前提交，
> 可能造成“数据已持久化但返回错误”的矛盾结果。

### 2.3 移除 `Timeout`

`UnitOfWorkOptions.Timeout` 已移除——它原本就**没有运行时效果**。超时改用
EF/provider 的命令、锁、事务超时配置。

### 2.4 `IDbContextWrapper` 已移除

被 provider 无关的 `IUnitOfWorkResource` 取代。仅影响自定义持久化 provider 集成方；
普通应用无需迁移。

### 2.5 新增能力

`IUnitOfWork` 新增：

- `FlushAsync()` — 按注册顺序激活并 flush 全部资源，返回受影响行数，不提交
- 保存点 — `CreateSavepointAsync` / `RollbackToSavepointAsync` / `ReleaseSavepointAsync`
- `MarkAsCompletedAsync()` — 只读边界跳过提交
- 事务事件 — `OnCommitting` / `OnCommitted` / `OnRollingBack` / `OnRolledBack`
- `IAsyncDisposable`

```csharp
// 保存点示例：事务内部分回滚
await _uow.CreateSavepointAsync("step1");
// ...执行一批操作
await _uow.RollbackToSavepointAsync("step1");   // 只撤销该点之后的变更
```

---

## 3. ASP.NET Core 边界

### 3.1 `UnitOfWorkAttribute` 精简

`InitializationMode`、`CreateOptions()` 已移除。属性现在只有两个选项：

```csharp
[UnitOfWork(IsReadOnly = true)]                     // 只读：写操作快速失败
[UnitOfWork(IsolationLevel = IsolationLevel.Serializable)]
```

### 3.2 `IsUowEnabled` 已移除


```csharp
// 迁移前
[UnitOfWork(IsUowEnabled = false)]

// 迁移后
[DisableUnitOfWork]
```

### 3.3 只读 action 名称推断改为 opt-in

以前 GET action 自动按名称推断为只读；现在默认**关闭**，显式元数据始终优先：

```csharp
// Startup.cs —— 依赖旧推断行为时重新开启：
services.Configure<MiCakeAspNetOptions>(o => o.EnableReadOnlyActionNameInference = true);
```

---

## 4. EF Core 集成

### 4.1 上下文工厂接口合并

非泛型 `IEFCoreContextFactory`、`IEFCoreAnchoredContextFactory`、无参
`GetDbContextWrapper()` 已合并为单一公共契约。自定义工厂实现只需实现两个方法：

```csharp
public class MyFactory<TDbContext> : IEFCoreContextFactory<TDbContext>
    where TDbContext : DbContext
{
    public TDbContext GetDbContext() { /* ... */ }
    public EFCoreDbContextWrapper GetOrCreateWrapperFor(DbContext context) { /* ... */ }
}
```

框架会通过适配器自动调用你的实现，无需了解内部视图。

### 4.2 `BypassUnitOfWorkCheck` 更名

`MiCakeEFCoreOptions.BypassUnitOfWorkCheck` 更名为 `AllowDbContextAccessWithoutUoW`，
默认仍为 `false`：

```csharp
// 迁移前
options.BypassUnitOfWorkCheck = true;

// 迁移后
options.AllowDbContextAccessWithoutUoW = true;
```

> **注意**：该选项现在仅放宽**上下文解析**（返回无 UoW 集成的 standalone wrapper，
> 用于只读 filter/middleware）。v2 中无 UoW 的**写入**整体按 Permissive 策略放行，
> 不再需要、也不应依赖此选项放行写入。

### 4.3 拦截器自动安装

`UseMiCakeInterceptors` 全部 3 个重载、`IMiCakeInterceptorFactory` 及其实现已移除。
拦截器由模块的 `ConfigureDbContext` 配置器**自动挂载**——只需在容器中注册 DbContext：

```csharp
// 迁移前
services.AddDbContext<AppDbContext>((sp, opt) =>
{
    opt.UseSqlite(connectionString);
    opt.UseMiCakeInterceptors(sp);   // 已移除
});

// 迁移后 —— 无需任何拦截器相关调用：
services.AddDbContext<AppDbContext>(opt =>
{
    opt.UseSqlite(connectionString);
    opt.UseMiCake();                 // 可选：安装 per-context options extension
});
```

> **要求**：EF Core **9+**（`ConfigureDbContext` / `IDbContextOptionsConfiguration<TContext>`
> 机制，EF Core 10 内置，已在 EF Core 10 上验证）。
> `MiCakeDbContext` 子类无需调用 `UseMiCake()`——`OnConfiguring` 会自动调用。

---

## 5. 行为变更

以下变更不涉及 API 删除，但会改变运行时行为，迁移后必须验证。

### 5.1 无 UoW 写入：Permissive 放行

v1 会拒绝无 UoW 的直接 `DbContext` 写入；v2 按原生 EF Core 语义**放行**（隐式事务，
无回滚/生命周期保证）。仓储/UoW 路径始终受守卫：

```csharp
// 无环境 UoW 时：
await dbContext.SaveChangesAsync();   // 放行，等同原生 EF（无 MiCake 保证）
```

需要事务保证时，开启 UoW 或使用 `IStandaloneUnitOfWorkExecutor`。

### 5.2 嵌套 UoW：共享提交 / 根回滚

- 嵌套 `CommitAsync` 只标记完成，**物理提交发生在根 UoW**
- 嵌套 `RollbackAsync` 标记**根** UoW rollback-only，根回滚覆盖全部资源

### 5.3 多资源提交：best-effort + 结构化结果

多资源提交按注册顺序确定性执行；部分失败抛出 `PartialUnitOfWorkCommitException`，
携带逐资源的结构化、非敏感结果（ID、类型、状态）。跨资源原子性需自行选择 outbox
或补偿工作流。

### 5.4 回滚/清理失败不再丢失

回滚或清理失败时抛出 `UnitOfWorkBoundaryException`，同时携带主异常 + 回滚失败 +
清理失败，便于诊断。

### 5.5 SaveChanges 重入：有界状态机

生命周期/领域事件处理器中递归调用 `SaveChangesAsync` 不再是原始递归：
嵌套保存被合并为同一事务内的保存周期，事件按实例去重。无进展或达到
`MaxSaveCycles`（默认 **16**）抛出 `SaveChangesReentryException` 并标记 UoW rollback-only：

```csharp
// MiCakeEFCoreOptions
options.MaxSaveCycles = 32;   // 按需调整上界
```

### 5.6 分页：强制全序

分页前必须产生**全序**：无调用方排序时按每个主键属性升序；有排序时缺失的主键属性
作为最后升序 `ThenBy` 追加；无键实体被拒绝。依赖旧隐式顺序的调用点请提供显式排序。

### 5.7 启动校验：DbContext 生命周期与执行策略

- DbContext 必须注册为 **scoped 或 pooled**；singleton/transient 启动即失败并给出指引
- `RetriesOnFailure = true` 的执行策略对环境可写 UoW **被拒绝**（需应用拥有的
  可重放边界，如独立重试执行器）

### 5.8 物理/批量操作显式化

绕过聚合生命周期的删除改用显式 API，要求在环境可写 UoW 内，且保持在 UoW 事务中：

```csharp
// 迁移前：仓储内直接物理删除
// 迁移后：显式物理删除执行器
var executor = sp.GetRequiredService<IEFCorePhysicalOperationExecutor<AppDbContext>>();
await executor.ExecuteDeleteAsync<Book>(b => b.PublishedYear < 2000);
```

直接使用 EF `ExecuteUpdate` / `ExecuteDelete` / `ExecuteSqlRaw`：UoW 内被守卫并绑定事务，
UoW 外按原生语义放行。

---

## 6. 迁移清单

### 第 1 步 — 仓储调用点

- [ ] `SaveChangesAsync()` → `IUnitOfWork.CommitAsync()`
- [ ] `AddAndReturnAsync(x)` → **`AddAndGetIdAsync(x)`（推荐，已加入 `IRepository` 接口）**；也可用 `AddAsync(x)` + `FlushAsync()`（行为等价）
- [ ] 移除 `AddAsync` 的 `saveNow:` 参数
- [ ] 移除 `ClearChangeTrackingAsync()` 调用
- [ ] 立即物理删除场景改用 `IEFCorePhysicalOperationExecutor<TDbContext>`

### 第 2 步 — UoW 用法

- [ ] `BeginAsync(requiresNew: true)` → `ExecuteRequiresNewAsync(...)`，全部从回调 provider 解析服务
- [ ] 移除 `PersistenceStrategy` / `OptimizeForSingleWrite` / `Timeout` 用法
- [ ] 后台作业/无环境 UoW 操作改用 `IStandaloneUnitOfWorkExecutor.ExecuteAsync(...)`
- [ ] 确保创建的 UoW 被释放（支持异步释放；ASP.NET 边界自动处理）

### 第 3 步 — ASP.NET Core

- [ ] 移除 `[UnitOfWork]` 上的 `InitializationMode` / `CreateOptions()` / `IsUowEnabled`
- [ ] `[UnitOfWork(IsUowEnabled = false)]` → `[DisableUnitOfWork]`
- [ ] 依赖 GET 只读推断时设置 `EnableReadOnlyActionNameInference = true`

### 第 4 步 — EF Core 注册

- [ ] 移除所有 `UseMiCakeInterceptors(...)` 调用与自定义 `IMiCakeInterceptorFactory`
- [ ] 确认 DbContext 注册为 **scoped 或 pooled**
- [ ] `BypassUnitOfWorkCheck` → `AllowDbContextAccessWithoutUoW`（仅只读场景保留）
- [ ] 不要注册自定义 `IUnitOfWorkAmbientAccessor`（框架 singleton）
- [ ] 每种上下文类型仅保留一个 `UseEFCore<TDbContext>()` / `AddUowCoreServices` 调用

### 第 5 步 — 行为验证

- [ ] 审查无 UoW 的直接 `DbContext` 写入——现在会**放行**，需要保证时包进 UoW
- [ ] 检查依赖隐式顺序的分页调用点，提供显式排序
- [ ] 检查 `UpdateAsync` 调用点：并发冲突现在在 flush/commit 时抛出
- [ ] 检查递归调用 `SaveChangesAsync` 的处理器：无进展循环抛 `SaveChangesReentryException`

---

## 7. 新 API 速查

| API | 用途 |
|---|---|
| `IUnitOfWork.FlushAsync()` | 激活事务 + 按注册顺序 flush；返回受影响行数；不提交 |
| `EFRepository.AddAndGetIdAsync(...)` | `AddAndReturnAsync` 的**推荐迁移目标**：添加 + FlushAsync 填充生成键并返回 `TKey`（已加入 `IRepository<TAggregateRoot, TKey>` 接口，需环境可写 UoW） |
| `IUnitOfWorkManager.ExecuteRequiresNewAsync(...)` | 隔离内部边界（取代 `requiresNew`） |
| `IStandaloneUnitOfWorkExecutor` | 无环境 UoW 的隔离作用域执行；成功提交、失败回滚 |
| `IUnitOfWorkResource` | provider 无关资源契约（取代 `IDbContextWrapper`） |
| 保存点三件套 | 事务内部分回滚；创建后注册的资源被拒绝 |
| `IEFCorePhysicalOperationExecutor<TDbContext>` | 显式物理删除（绕过聚合生命周期） |
| `UnitOfWorkBoundaryException` | 主异常 + 回滚/清理失败合并 |
| `PartialUnitOfWorkCommitException` | best-effort 多资源提交的结构化结果 |
| `SaveChangesReentryException` | 无进展 / `MaxSaveCycles` 重入失败 |
| `UseMiCake()`（options builder） | 仅安装 options 的 EF Core 集成入口 |
| `[DisableUnitOfWork]` | 为 action/controller 退出 ASP.NET UoW 边界 |
| `MiCakeEFCoreOptions.MaxSaveCycles`（默认 16） | 重入循环上界 |
| `MiCakeEFCoreOptions.AllowDbContextAccessWithoutUoW` | 只读 filter/middleware 放宽上下文解析 |

---

## 8. FAQ

**问：安装拦截器还需要调用什么吗？**
不需要。在容器中注册 DbContext 即可——模块的 `ConfigureDbContext` 配置器自动挂载
拦截器与 options。要求 EF Core 9+。

**问：UoW 外直接 `SaveChangesAsync` 以前会失败，现在呢？**
按原生 EF Core 语义放行（Permissive 策略），无 MiCake 事务/回滚/生命周期保证。
需要保证时开启 UoW 或使用 `IStandaloneUnitOfWorkExecutor`。

**问：`Timeout` 没了，超时怎么设？**
使用 EF/provider 的命令、锁、事务超时配置。`Timeout` 在 v1 从未生效。

**问：为什么移除 `OptimizeForSingleWrite`？**
它允许隐式事务在 post-save 生命周期处理前提交，post-save 失败会在数据已持久化后
返回错误。v2 每个可写 UoW 使用显式事务（Lazy 首写前激活，或 Immediate）。

**问：`UpdateAsync` 现在抛 `DbUpdateConcurrencyException`？**
分离替换保留并发令牌，过期实例暴露冲突而非静默覆盖。建议 load-and-modify。

**问：`requiresNew` 回调捕获外层服务还能用吗？**
不能——所有权校验拒绝外层作用域捕获的上下文/仓储。全部从回调 `IServiceProvider` 解析。

---

## 9. 相关文档

- [`docs/UoW-v2-Architecture-and-Usage.md`](UoW-v2-Architecture-and-Usage.md) — v2 完整架构、API 参考、使用指南
- `src/framework/MiCake/README.md`、`src/framework/MiCake.EntityFrameworkCore/README.md`、
  `src/framework/MiCake.AspNetCore/README.md` — 各包指引
- `samples/BaseMiCakeApplication/` — 已迁移的示例应用

