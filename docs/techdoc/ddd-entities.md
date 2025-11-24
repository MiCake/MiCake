# 领域驱动 - 实体

实体（Entity）是具有唯一标识的领域对象，其生命周期由聚合根管理。MiCake 在 `MiCake.DDD.Domain` 中提供了基础实体类型。

## 实体基类

- `Entity`：默认使用 `int` 作为主键类型
- `Entity<TKey>`：显式指定主键类型

```csharp
using MiCake.DDD.Domain;

public class Book : Entity<long>
{
    public string Name { get; private set; }
    public string Author { get; private set; }

    public Book(long id, string name, string author)
    {
        Id = id;
        Name = name;
        Author = author;
    }
}
```

## 审计和软删除接口

实体可以通过实现审计和软删除接口获得额外行为（见“自动审计”“软删除支持”章节）：

```csharp
using MiCake.Audit;
using MiCake.Audit.SoftDeletion;
using MiCake.DDD.Domain;

public class User : Entity<long>, IHasCreationTime, IHasModificationTime, ISoftDeletion
{
    public string UserName { get; private set; }

    public DateTime CreationTime { get; set; }
    public DateTime? ModificationTime { get; set; }
    public bool IsDeleted { get; set; }
}
```

当仓储保存该实体时，MiCake 根据配置自动填充 `CreationTime`/`ModificationTime` 并应用软删除逻辑。