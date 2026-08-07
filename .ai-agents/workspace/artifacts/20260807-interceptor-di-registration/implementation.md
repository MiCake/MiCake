---
id: '20260807-interceptor-di-registration'
status: 'implemented'
---

# Implementation: Interceptor Installation via ConfigureDbContext (方案 C 修订版)

## Implementation Summary

实现拦截器经 EF Core 9+ `ConfigureDbContext` 机制自动安装（方案 C 修订版，design.md ADR-1）。新增 `MiCakeDbContextOptionsConfigurator<TContext>`（`IDbContextOptionsConfiguration<TContext>`），在 `Configure(sp, builder)` 内 `UseMiCake()`（安装 per-context options extension）并 `AddInterceptors`（显式附加从 `sp` 解析的拦截器单例）。`MiCakeEFCoreModule.PreConfigureServices` 注册配置器 + 拦截器 singleton 服务；两个拦截器 ctor 改为注入 `IUnitOfWorkAmbientAccessor`；守卫策略改为 Permissive（无 ambient UoW 的裸 DbContext 写放行，仅 UoW 内写被守卫/绑定/生命周期处理）。删除 `UseMiCakeInterceptors` 全部 3 个重载与 `IMiCakeInterceptorFactory`/`MiCakeInterceptorFactory`（含 Helper），新增 `UseMiCake()` 作为唯一 options 入口。实施过程纠正了两个诊断误判（探针误导、模块配置器强转 InvalidCastException），最终全解决方案测试通过。

## Files Touched

| Path | Action | Intent |
|------|--------|--------|
| `src/framework/MiCake.EntityFrameworkCore/Internal/MiCakeDbContextOptionsConfigurator.cs` | create | `IDbContextOptionsConfiguration<TContext>` 实现：`UseMiCake()` + `AddInterceptors(sp-resolved)` |
| `src/framework/MiCake.EntityFrameworkCore/Modules/MiCakeEFCoreModule.cs` | modify | 注册拦截器 singleton + `ConfigureDbContext` 配置器（反射调用泛型重载）；移除 factory 注册 |
| `src/framework/MiCake.EntityFrameworkCore/Internal/MiCakeEFCoreInterceptor.cs` | modify | ctor 注入 `IUnitOfWorkAmbientAccessor`；Permissive 放行；`ResolveMaxSaveCycles` 读全局 options |
| `src/framework/MiCake.EntityFrameworkCore/Internal/MiCakeDbCommandInterceptor.cs` | modify | ctor 注入 accessor；Permissive 放行 |
| `src/framework/MiCake.EntityFrameworkCore/Internal/MiCakeInterceptorPipeline.cs` | modify | 解析辅助改接 accessor 参数 |
| `src/framework/MiCake.EntityFrameworkCore/Extensions/DbContextExtensions.cs` | modify | 新增 `UseMiCake()`；删除 3 个 `UseMiCakeInterceptors` 重载 |
| `src/framework/MiCake.EntityFrameworkCore/MiCakeDbContext.cs` | modify | `OnConfiguring` → `UseMiCake()` |
| `src/framework/MiCake.EntityFrameworkCore/Internal/IMiCakeInterceptorFactory.cs` | delete | 退役（配置器取代） |
| `src/framework/MiCake.EntityFrameworkCore/Internal/MiCakeInterceptorFactory.cs` | delete | 退役（impl + Helper） |
| `src/tests/MiCake.EntityFrameworkCore.Tests/Internal/ConfigureDbContextPrototypeTests.cs` | create | 机制验证（Gate1-4） |
| `src/tests/MiCake.EntityFrameworkCore.Tests/Internal/MiCakeWriteGuardInterceptorsTests.cs` | modify | Permissive 语义更新 + 配置器挂载验证测试 |
| `src/tests/MiCake.EntityFrameworkCore.Tests/Internal/SaveOperationConcurrencyTests.cs` | modify | fixture 补 `UseMiCake()` |
| `src/tests/MiCake.EntityFrameworkCore.Tests/Internal/SaveOperationReentryFailureTests.cs` | modify | fixture 补 `UseMiCake()`（2 处） |
| `src/tests/MiCake.EntityFrameworkCore.Tests/Repository/EFRepositoryContractTests.cs` | modify | fixture 补 `UseMiCake()` |
| `src/tests/MiCake.EntityFrameworkCore.Tests/Integration/EndToEndIntegrationTests.cs` | modify | 过时引导测试改写为新语义 |
| `src/tests/MiCake.EntityFrameworkCore.Tests/Integration/MiCakeDbContextIntegrationTests.cs` | modify | 过时引导测试改写为新语义 |
| `src/tests/MiCake.EntityFrameworkCore.Tests/Internal/MiCakeInterceptorFactoryTests.cs` | delete | 退役 |
| `src/tests/MiCake.EntityFrameworkCore.Tests/Extensions/DbContextExtensionsEnhancedTests.cs` | delete | 退役 |
| `src/tests/MiCake.EntityFrameworkCore.Tests/Summary/NoServiceProviderDependencySummaryTests.cs` | delete | 退役 |
| `src/tests/MiCake.IntegrationTests/Uow/SqliteUnitOfWorkFixture.cs` | modify | fixture 补 `UseMiCake()`（3 处）+ 拦截器 singleton |
| `src/tests/MiCake.IntegrationTests/Uow/UnitOfWorkWritePathTests.cs` | modify | `WriteWithoutUoW_IsRejected` → Permissive 语义 |
| `src/tests/MiCake.IntegrationTests/Uow/AuditIntegrationTests.cs` | modify | 移除 `UseMiCakeInterceptors(sp)` |
| `src/tests/MiCake.IntegrationTests/Uow/GenericAuditIntegrationTests.cs` | modify | 移除 `UseMiCakeInterceptors(sp)` |
| `src/tests/MiCake.IntegrationTests/Uow/OwnedEntityAuditIntegrationTests.cs` | modify | 移除 `UseMiCakeInterceptors(sp)` |
| `src/tests/MiCake.IntegrationTests/Uow/UnitOfWorkLazyImmediateModeIntegrationTests.cs` | modify | 移除 `(sp)` + 拦截器 singleton |
| `src/tests/MiCake.IntegrationTests/Repository/CommonFilterPagingQueryIntegrationTests.cs` | modify | 移除 `(sp)` + 拦截器 singleton |
| `src/tests/MiCake.IntegrationTests/Fixtures/MiCakeAppFixture.cs` | modify | 移除 Helper.Reset() 引用 |

## Design Compliance

| Check | Result | Reason |
|-------|--------|--------|
| Files touched == Change Tracking ± deviation | passed | 生产 5 modify + 1 create + 2 delete 与设计一致；测试迁移范围一致 |
| Module/layer placement | passed | 全部在 `MiCake.EntityFrameworkCore` 的 Internal/Modules/Extensions；ASP.NET 未改 |
| Public interfaces match Key Interfaces | passed | `UseMiCake()` 新增、3 重载删除、配置器签名与设计一致 |
| Forbidden cross-layer imports absent | passed | 无新跨层依赖 |
| Error handling at boundaries | passed | 拦截器/Permissive 放行逻辑无内部 catch 吞噬 |
| No new external deps | passed | 无 manifest 变更（`ConfigureDbContext` 属 EF Core 10 内置） |

## Deviations from Design

1. **配置器注册采用反射调用泛型 `ConfigureDbContext`**——EF Core 只有泛型重载，模块按 `Type` 注册需反射；设计未指定实现方式，属实现细节。
2. **审计测试通过 `AddMiCake` 模块路径注册**——模块配置器强转 `IDbContextOptionsConfiguration<DbContext>` 会 `InvalidCastException`，改为反射调用具体泛型接口的 `Configure`（`MiCakeEFCoreModule.PreConfigureServices` 内）。设计未覆盖此实现细节。
3. **`ResolveMaxSaveCycles` 改为读全局 `MiCakeEFCoreOptions` 优先、extension 回退**——设计 ADR-1a 隐含全局选项权威；修正原实现仅读 extension 的问题。
4. **删除 `ConfigureDbContextPrototypeTests` 未执行**——保留作为机制验证（Gate1-4），非偏差。

## Self-Check Results

- Type-checker: `dotnet build` 全解决方案 0 错误（framework + tests + sample）。
- 测试：MiCake.Tests 269/269、EF Core 235/235、Integration 178/178、ASP.NET 432/432 全通过。
- 验证过程：26 个 EF Core 失败 + 22 个集成失败全部修复（A 类 Permissive 语义 5、B 类过时引导 5、C 类探针 2、D 类 fixture 漏 `UseMiCake()` 12、E 类模块配置器强转 22）。

## Open TODOs

- `/mvt-review`：复核本次实施（设计合规 + Permissive 语义 + 配置器机制）。
- `/mvt-implement` 后续：README ×4 迁移文档（`UseMiCake()` 自动安装说明）——设计 Change Tracking 含但本次未做。
- 可选：`ConfigureDbContextPrototypeTests` 是否保留（当前保留作为机制回归）。

## Change Tracking

- Change: `20260807-interceptor-di-registration`（设计工件存在，无 plan.yaml）。
