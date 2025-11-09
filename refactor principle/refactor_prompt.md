# 任务
根据MiCake Framework的重构规范，对以下的功能进行重构。

## 重构规范
文件`refactor principle\specification.md`包含了MiCake Framework的重构规范，请仔细阅读并遵循其中的指导原则和最佳实践进行重构。

## 项目描述
`.github\copilot-instructions.md`文件包含了现有项目的介绍。

## 重构分支
`refactor`分支

## 重构内容
重构MiCake现有的 MiCakeApplication 类以及 MiCake的模块化系统的实现（包括模块的注册、加载和依赖管理等功能）。

### 涉及文件
+ `src\framework\MiCake.Core\Abstractions\Modularity` 里面包含了 MiCakeApplication 类的定义和相关接口。
+ `src\framework\MiCake.Core\Modularity` 里面包含了模块化系统的实现代码。
+ `IMiCakeApplication` 接口定义了 MiCakeApplication 类的基本功能和行为。
+ `IMiCakeApplicationProvider` 接口定义了 MiCakeApplication 提供者的功能。
+ `IMiCakeBuilder` 接口定义了 MiCakeApplication 的构建功能。
+ `IMiCakeEnvironment` 接口定义了 MiCakeApplication 的环境相关功能。
+ `MiCakeApplicationOptions`
+ `MiCakeModuleHelper` 提供了模块化系统的辅助方法

### 现有的一些问题
+ 当前MiCake Application的部分 class的实例化并没有通过依赖注入容器进行管理，导致代码的耦合度较高，难以进行单元测试和维护。
+ Application在启动之前需要`IDependencyReceiver<IServiceProvider>.AddDependency(IServiceProvider parts)`来注入服务提供者，导致启动流程复杂且不直观。
+ 模块化系统的实现较为复杂，模块的注册和加载逻辑分散在多个类中，难以理解和维护。

### 新的目标
MiCake的模块化系统能够很灵活的支持每个模块的配置和初始化逻辑，且模块之间的依赖关系能够被清晰地管理和处理。

当最终重构完成后，用户应该能够通过以下代码来配置和使用MiCake Application：

```csharp
builder.Services.AddMiCake(ctx=>   // 注册MiCake，这将初始化MiCake Application并且解析模块
{
    ctx.AddEFCore<MyDbContext>(efLayerOptions=>{});   // 注册EFCore MiCake模块,并设置对应的配置项
    ctx.AddAudit(auditCtx=> 
    {
        auditCtx.UseDefaultOptions();              // 注册审计模块，并使用默认配置
    });
});

var app = builder.Build();

app.UseMiCake();  // 启动MiCake Application
```

这对用户来说，这基本是一种无感知的使用体验，用户只需要关注自己业务逻辑的实现，而不需要关心MiCake Application的启动和模块化系统的实现细节。

## 目标
对所需要重构的部分进行完全重构并移除旧代码，确保重构后的代码符合MiCake Framework的重构规范，并提升代码的质量和可维护性。

## 重构要求
1. 在重构之前，一定要理解现有代码的功能和逻辑，确保重构后的代码能够覆盖现有功能，且具有更优的表现和质量。
2. 确保代码的格式和样式符合MiCake Framework的编码规范。
3. 优化代码结构，提高可读性和可维护性。
4. 删除冗余代码，简化逻辑。
5. 新增的代码需要考虑对外的接口签名，便于用户能够方便地调用和使用。
6. 代码应该具有高扩展性，便于未来的功能扩展和修改。
7. 代码应该适当的引入设计模式，以提高代码的灵活性和可维护性。

## 注意事项
当前仅关注在MiCake.Core项目中的代码重构，其他项目暂不涉及。
即使在修改了MiCake.Core项目的代码导致了其他项目的编译错误，也不需要修改其他项目的代码。因为我们会在后续的重构中处理其他项目的代码兼容性问题。