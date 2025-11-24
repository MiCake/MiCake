# 统一返回

MiCake.AspNetCore 在 `MiCake.AspNetCore.Responses` 中提供统一返回包装能力，用于规范 API 的响应结构。

## 核心类型

- `ApiResponse`：标准响应模型
- `ErrorResponse`：错误响应模型
- `IResponseWrapper`：包装器接口
- `ResponseWrapperFactory`：根据上下文选择合适包装器
- `ResponseWrapperOptions`：包装配置

一个典型的响应结构类似：

```json
{
  "success": true,
  "data": { ... },
  "error": null
}
```

## 启用数据包装

在 ASP.NET Core 集成时配置 `MiCakeAspNetOptions`：

```csharp
services.AddMiCake<ApplicationModule>()
        .UseAspNetCore(options =>
        {
            options.DataWrapperOptions.Enabled = true;
        });
```

启用后，控制器返回的 `object` / `IActionResult` 会自动被包装为 `ApiResponse`。

## 自定义返回结果

可以实现自定义的 `IResponseWrapper`，并通过工厂进行注册：

```csharp
using MiCake.AspNetCore.Responses;

public class MyCustomResponseWrapper : IResponseWrapper
{
    public bool CanWrap(WrapperContext context)
    {
        // 根据路由、返回类型等决定是否包装
        return true;
    }

    public object Wrap(WrapperContext context)
    {
        return new ApiResponse
        {
            Success = true,
            Data = context.OriginalData
        };
    }
}
``;

在模块中注册包装器：

```csharp
public override Task ConfigureServices(ModuleConfigServiceContext context)
{
    context.Services.AddSingleton<IResponseWrapper, MyCustomResponseWrapper>();
    return Task.CompletedTask;
}
```
