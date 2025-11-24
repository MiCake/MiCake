# 领域驱动 - 聚合根

聚合根（Aggregate Root）是聚合的入口，负责维护聚合内实体和值对象的一致性。MiCake 在 `MiCake.DDD.Domain` 中提供 `AggregateRoot` 基类。

## 定义聚合根

```csharp
using MiCake.DDD.Domain;

public class Order : AggregateRoot<Guid>
{
    public string OrderNo { get; private set; }

    protected Order() { }

    public Order(string orderNo)
    {
        Id = Guid.NewGuid();
        OrderNo = orderNo;
    }

    public void ChangeOrderNo(string newOrderNo)
    {
        OrderNo = newOrderNo;
    }
}
```

## 领域事件

聚合根可以通过 `RaiseDomainEvent` 添加领域事件，MiCake 会在持久化时统一分发（详见“领域事件”章节）：

```csharp
public void ChangeOrderNo(string newOrderNo)
{
    OrderNo = newOrderNo;
    RaiseDomainEvent(new OrderNumberChangedEvent(Id, newOrderNo));
}
```