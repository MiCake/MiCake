# 领域驱动 - 值对象

值对象（Value Object）是通过属性值进行比较的不可变对象，不关心唯一标识。MiCake 在 `MiCake.DDD.Domain` 中提供 `ValueObject` 基类。

## 定义值对象

```csharp
using MiCake.DDD.Domain;
using System.Collections.Generic;

public class Money : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }

    public Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}
```

## 使用建议

- 只暴露只读属性，保证不可变性
- 在 `GetEqualityComponents` 中返回所有参与相等性的成员
- 可直接嵌入到实体或聚合根中，由 EF Core 作为拥有实体（Owned Entity）进行持久化