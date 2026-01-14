# MiCake

A lightweight Domain-Driven Design (DDD) toolkit for .NET.

## Overview

`MiCake` is the main DDD package providing all essential components:

- **Entity & Aggregate Root** - DDD tactical pattern implementations
- **Value Objects** - Immutable value type support
- **Repository Pattern** - Data access abstraction
- **Domain Events** - Event-driven domain modeling
- **Unit of Work** - Transaction management
- **Audit Support** - Automatic timestamp tracking

## Installation

```bash
dotnet add package MiCake
```

## Quick Start

```csharp
// Define your aggregate root
public class Order : AggregateRoot<Guid>
{
    public string CustomerName { get; private set; }
    public decimal TotalAmount { get; private set; }
    
    public void UpdateTotal(decimal amount)
    {
        TotalAmount = amount;
        AddDomainEvent(new OrderUpdatedEvent(Id));
    }
}
```

## Key Components

| Component | Description |
|-----------|-------------|
| `Entity<TKey>` | Base class for domain entities |
| `AggregateRoot<TKey>` | Base class for aggregate roots |
| `ValueObject` | Base class for value objects |
| `IRepository<T>` | Repository interface for data access |
| `IDomainEvent` | Interface for domain events |

## Documentation

📚 [Full Documentation](https://micake.github.io)

## License

MIT License - see [LICENSE](https://github.com/MiCake/MiCake/blob/master/LICENSE)
