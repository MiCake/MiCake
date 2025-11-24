# 自动审计

MiCake 在 `MiCake.Audit` 命名空间下提供自动审计能力，包括创建时间、修改时间等元数据填充。

## 审计接口

- `IHasCreationTime`
- `IHasModificationTime`
- `IHasAudit`（组合创建和修改时间）

```csharp
using MiCake.Audit;
using MiCake.DDD.Domain;

public class Article : AggregateRoot<Guid>, IHasAudit
{
    public string Title { get; set; }

    public DateTime CreationTime { get; set; }
    public DateTime? ModificationTime { get; set; }
}
```

## 启用审计

在模块中启用审计选项：

```csharp
using MiCake.Audit;

public override Task ConfigureServices(ModuleConfigServiceContext context)
{
    context.Services.Configure<MiCakeAuditOptions>(options =>
    {
        options.UseAudit = true;          // 默认为 true
    });

    return Task.CompletedTask;
}
```

当仓储执行保存时，`DefaultAuditExecutor` 会根据实体状态自动设置时间字段。
