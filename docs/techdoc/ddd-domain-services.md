# 领域驱动 - 领域服务

当某些业务逻辑无法自然地归属到单个实体或聚合根上时，可以使用领域服务（Domain Service）。

领域服务应：
- 聚焦领域规则而非应用流程
- 依赖仓储接口而非基础设施实现

```csharp
using MiCake.DDD.Domain;

public class PricingDomainService
{
    private readonly IRepository<Order, Guid> _orderRepository;

    public PricingDomainService(IRepository<Order, Guid> orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<decimal> CalculateOrderPriceAsync(Guid orderId)
    {
        var order = await _orderRepository.FindAsync(orderId);
        // 根据订单行、优惠策略等计算总价
        return 0m;
    }
}
```

> 领域服务通常是普通的类，不需要继承特定基类，可通过依赖注入在应用服务或控制器中使用。
