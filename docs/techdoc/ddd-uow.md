# 领域驱动 - 工作单元

工作单元（Unit of Work, UoW）用于协调多个仓储操作的事务边界。MiCake 在 `MiCake.DDD.Uow` 中提供 `IUnitOfWork` 和 `IUnitOfWorkManager`。

## 使用工作单元

在应用服务中显式控制一个 UoW 生命周期：

```csharp
using MiCake.DDD.Uow;

public class OrderAppService
{
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly IRepository<Order, Guid> _orderRepository;

    public OrderAppService(IUnitOfWorkManager unitOfWorkManager,
                           IRepository<Order, Guid> orderRepository)
    {
        _unitOfWorkManager = unitOfWorkManager;
        _orderRepository = orderRepository;
    }

    public async Task PlaceOrderAsync(string orderNo)
    {
        await using var uow = _unitOfWorkManager.Begin();

        var order = new Order(orderNo);
        await _orderRepository.AddAndReturnAsync(order, saveNow: false);

        await uow.CompleteAsync();
    }
}
```

MiCake 与 EF Core 的 `DbContext` 集成，使得同一 UoW 内的所有仓储操作共用一个事务。
