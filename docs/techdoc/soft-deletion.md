# 软删除支持

MiCake 通过 `MiCake.Audit.SoftDeletion` 命名空间提供软删除能力，并在存储层应用查询过滤器。

## 软删除接口

- `ISoftDeletion`：表示实体支持软删除
- `IHasDeletionTime` / `IHasAuditWithSoftDeletion`：包含删除时间 + 审计字段

```csharp
using MiCake.Audit.SoftDeletion;
using MiCake.DDD.Domain;

public class User : AggregateRoot<long>, ISoftDeletion
{
    public string UserName { get; set; }
    public bool IsDeleted { get; set; }
}
```

## 启用软删除

```csharp
using MiCake.Audit;

public override Task ConfigureServices(ModuleConfigServiceContext context)
{
    context.Services.Configure<MiCakeAuditOptions>(options =>
    {
        options.UseSoftDeletion = true;
    });

    return Task.CompletedTask;
}
```

启用后：

- 在保存实体时，`SoftDeletionRepositoryLifetime` 会拦截删除操作，将实体标记为 `IsDeleted = true`，并可选写入 `DeletionTime`
- `SoftDeletionConvention` 为实现 `ISoftDeletion` 的实体自动添加查询过滤器：`entity => !entity.IsDeleted`

因此：
- 调用仓储或 `DbContext` 的删除方法时，记录仍保留在数据库中
- 普通查询不会返回已软删除记录，如有需要可显式忽略过滤器
