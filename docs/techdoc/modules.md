# 模块化 - 模块使用

MiCake 采用模块化设计，通过 `MiCakeModule` 描述应用的组成部分及其依赖关系。核心代码在 `MiCake.Core.Modularity` 中。

## 定义模块

```csharp
using MiCake.Core.Modularity;

[RelyOn(typeof(MiCakeEssentialModule))]
public class ApplicationModule : MiCakeModule
{
    public override Task ConfigureServices(ModuleConfigServiceContext context)
    {
        // 注册应用服务、仓储等
        context.AutoRegisterRepositories(typeof(ApplicationModule).Assembly);
        return Task.CompletedTask;
    }
}
```

常见模块类型：
- 应用自定义模块：继承 `MiCakeModule`
- 框架基础模块：如 `MiCakeRootModule`、`MiCakeEssentialModule`、`MiCakeEFCoreModule` 等

## 模块生命周期

模块提供一组按顺序调用的生命周期方法：

- `PreConfigureServices`
- `ConfigureServices`
- `PostConfigureServices`
- `PreInitialize`
- `Initialize`
- `PostInitialize`

一般情况下，只需要在 `ConfigureServices` 中注册服务，如果需要在容器构建前后做额外工作，可选择实现其他阶段。
