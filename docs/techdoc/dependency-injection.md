# 依赖注入

MiCake 基于 ASP.NET Core 的 `IServiceCollection`，在 `MiCake.Core.DependencyInjection` 中提供了一层封装和约定。

## 自动注册生命周期

实现以下标记接口之一即可被自动注册到容器中：

- `ITransientService`：瞬时（每次解析创建新实例）
- `IScopedService`：作用域（每个请求一个实例）
- `ISingletonService`：单例（应用进程级别一个实例）

```csharp
using MiCake.Core.DependencyInjection;

public interface IUserAppService : IScopedService
{
    Task<UserDto> GetAsync(long id);
}

public class UserAppService : IUserAppService
{
    // ...
}
```

在模块的 `ConfigureServices` 中调用 `context.Services.AddMiCake()` 或使用框架提供的默认配置后，这些接口会被自动扫描并注册。

## 手动注册

仍可使用标准的 `IServiceCollection` 扩展方法手动注册：

```csharp
public override Task ConfigureServices(ModuleConfigServiceContext context)
{
    context.Services.AddScoped<IUserAppService, UserAppService>();
    return Task.CompletedTask;
}
```
