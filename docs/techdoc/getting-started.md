# 开始

## MiCake 简介

MiCake 是一个基于领域驱动设计（DDD）的轻量级 .NET 应用框架，专注于在不侵入业务代码的前提下，为复杂业务提供清晰的架构边界、模块化能力和丰富的基础设施支持（审计、软删除、工作单元、统一返回等）。

核心目标：
- 让 DDD 在实际项目中更容易落地
- 与 ASP.NET Core 深度集成，但保持低侵入性
- 通过模块系统和约定优于配置降低样板代码

## 快速开始

MiCake 与 ASP.NET Core 通过扩展方法集成，典型入口在 `Startup` 或 `Program` 中：

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();

        // 注册 MiCake 核心 + ASP.NET Core 集成
        services.AddMiCake<ApplicationModule>(options =>
        {
            // 配置领域层程序集，MiCake 会在其中扫描实体、聚合根、值对象
            options.DomainLayerAssemblies = new[] { typeof(ApplicationModule).Assembly };
        })
        .UseAspNetCore();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseRouting();

        // 在 UseEndpoints 之前启动 MiCake
        app.StartMiCake();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }
}
```

其中 `ApplicationModule` 是应用的入口模块，继承自 `MiCakeModule`：

```csharp
public class ApplicationModule : MiCakeModule
{
    public override Task ConfigureServices(ModuleConfigServiceContext context)
    {
        // 自动发现并注册仓储实现
        context.AutoRegisterRepositories(typeof(ApplicationModule).Assembly);
        return Task.CompletedTask;
    }
}
```

## 核心概念

- **MiCakeModule**：模块系统的基础单元，用于配置依赖注入、仓储、审计等。模块之间可以通过 `RelyOn/DependsOn` 指定依赖关系。
- **MiCakeApplication**：封装整个应用的启动生命周期，由 `AddMiCake` 和 `StartMiCake` 负责创建和启动。
- **Domain Layer Assemblies**：领域层所在程序集列表，MiCake 会在这些程序集里扫描实体、聚合根、值对象、领域事件等。
- **Unit of Work (UoW)**：通过 `IUnitOfWork` 与 EF Core 集成，统一控制事务边界。
- **Repository**：面向聚合根的仓储接口 `IRepository<TAggregate,TKey>`，由 MiCake 自动生成具体实现。

> 更多完整示例可参考 `samples/BaseMiCakeApplication` 项目。