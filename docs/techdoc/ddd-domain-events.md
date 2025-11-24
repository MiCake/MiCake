# 领域驱动 - 领域事件

领域事件（Domain Event）用于表达领域内发生的“有意义的事情”。MiCake 提供 `IDomainEvent` 接口和事件调度基础设施。

## 定义领域事件

```csharp
using MiCake.DDD.Domain;

public class OrderCreatedEvent : IDomainEvent
{
    public Guid OrderId { get; }

    public OrderCreatedEvent(Guid orderId)
    {
        OrderId = orderId;
    }
}
```

## 在聚合根中发布事件

```csharp
public class Order : AggregateRoot<Guid>
{
    public string OrderNo { get; private set; }

    public Order(string orderNo)
    {
        Id = Guid.NewGuid();
        OrderNo = orderNo;

        // 创建时发布领域事件
        RaiseDomainEvent(new OrderCreatedEvent(Id));
    }
}
```

当仓储调用 `SaveChanges`/`SaveChangesAsync` 时，MiCake 会自动收集并分发这些事件。

## 处理领域事件

MiCake 使用基于接口的事件处理器模型，一般为：

```csharp
using MiCake.DDD.Domain.EventDispatch;

public class OrderCreatedEventHandler : IDomainEventHandler<OrderCreatedEvent>
{
    public Task HandleAsync(OrderCreatedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        // 发送邮件、写审计日志等副作用
        return Task.CompletedTask;
    }
}
```

事件处理器会在模块启动时通过 `services.RegisterDomainEventHandler(context.MiCakeModules)` 自动注册。
