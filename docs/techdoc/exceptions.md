# 异常

MiCake 在 `MiCake.Core.Abstractions.Exceptions` 中定义了框架级异常类型，用于表达框架内部或通用错误场景。

## SlightMiCakeException

`SlightMiCakeException` 是一种轻量级异常，用于在不终止应用的前提下提示配置或使用不当等问题。

```csharp
using MiCake.Core;

throw new SlightMiCakeException("MiCake is not initialized correctly.");
```

在实际项目中，建议：
- 业务异常使用自定义异常类型
- 与 MiCake 生命周期、模块配置相关的问题使用 `SlightMiCakeException` 或派生异常

> 可以结合 ASP.NET Core 的异常中间件或过滤器，将这些异常统一转换为标准 API 返回格式。
