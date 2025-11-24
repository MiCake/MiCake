# 领域驱动 - 仓储

仓储（Repository）用于对聚合根进行持久化操作。MiCake 在 `MiCake.DDD.Domain` 中定义通用接口，并通过 EF Core 扩展自动生成实现。

## 仓储接口

```csharp
using MiCake.DDD.Domain;

public interface IRepository<TAggregateRoot, TKey> : IReadOnlyRepository<TAggregateRoot, TKey>
    where TAggregateRoot : class, IAggregateRoot<TKey>
    where TKey : notnull
{
    Task<TAggregateRoot> AddAndReturnAsync(TAggregateRoot aggregateRoot, bool saveNow = true, CancellationToken cancellationToken = default);
    Task UpdateAsync(TAggregateRoot aggregateRoot, CancellationToken cancellationToken = default);
    Task DeleteAsync(TAggregateRoot aggregateRoot, CancellationToken cancellationToken = default);
}
```

## 注册仓储

在应用模块中通过 `AutoRegisterRepositories` 自动注册：

```csharp
public class ApplicationModule : MiCakeModule
{
    public override Task ConfigureServices(ModuleConfigServiceContext context)
    {
        context.AutoRegisterRepositories(typeof(ApplicationModule).Assembly);
        return Task.CompletedTask;
    }
}
```

## 使用仓储

```csharp
public class OrderService
{
    private readonly IRepository<Order, Guid> _orderRepository;

    public OrderService(IRepository<Order, Guid> orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<Order> CreateAsync(string orderNo)
    {
        var order = new Order(orderNo);
        return await _orderRepository.AddAndReturnAsync(order);
    }
}
```