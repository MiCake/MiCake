# MiCake DDD Patterns

## Domain Object Hierarchy

```
IEntity
  └── Entity<TKey>
        └── AggregateRoot<TKey>

IValueObject
  └── ValueObject
  └── RecordValueObject
```

## Entity

Base class for domain entities with identity.

```csharp
public class Order : Entity<long>
{
    public string CustomerName { get; private set; }
    public OrderStatus Status { get; private set; }
    
    protected Order() { } // EF Core
    
    public Order(string customerName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(customerName);
        CustomerName = customerName;
        Status = OrderStatus.Created;
    }
    
    public void Confirm()
    {
        if (Status != OrderStatus.Created)
            throw new DomainException("Can only confirm created orders");
        
        Status = OrderStatus.Confirmed;
        AddDomainEvent(new OrderConfirmedEvent(Id));
    }
}
```

Key points:
- Inherit from `Entity<TKey>`
- Private setters protect invariants
- Protected parameterless constructor for EF Core
- Domain methods enforce business rules
- Use `AddDomainEvent` for domain events

## Aggregate Root

Root entity that owns a consistency boundary.

```csharp
public class Order : AggregateRoot<long>
{
    private readonly List<OrderLine> _lines = new();
    public IReadOnlyList<OrderLine> Lines => _lines.AsReadOnly();
    
    public void AddLine(Product product, int quantity)
    {
        // Business logic within aggregate boundary
        var line = new OrderLine(product.Id, quantity, product.Price);
        _lines.Add(line);
        RecalculateTotal();
    }
}
```

Key points:
- Only aggregate roots have repositories
- Aggregate protects internal consistency
- External references by ID only
- Keep aggregates small

## Value Object

Immutable objects defined by their attributes.

```csharp
public class Money : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }
    
    public Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency ?? throw new ArgumentNullException(nameof(currency));
    }
    
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
    
    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new DomainException("Cannot add different currencies");
        return new Money(Amount + other.Amount, Currency);
    }
}
```

Or using record:

```csharp
public record Address(string Street, string City, string PostalCode) : RecordValueObject;
```

## Domain Events

Events that capture something that happened in the domain.

```csharp
public record OrderConfirmedEvent(long OrderId) : DomainEvent;
```

Handling:

```csharp
public class OrderConfirmedHandler : IDomainEventHandler<OrderConfirmedEvent>
{
    public async Task Handle(OrderConfirmedEvent @event, CancellationToken cancellationToken)
    {
        // Handle the event
    }
}
```

Dispatch:
- Events added via `AddDomainEvent()` in entity
- Dispatched automatically on `SaveChangesAsync()`

## Repository Pattern

```csharp
public interface IOrderRepository : IRepository<Order, long>
{
    Task<Order?> FindByCustomerAsync(string customerName, CancellationToken cancellationToken);
}
```

Usage:

```csharp
public class OrderService
{
    private readonly IRepository<Order, long> _repository;
    
    public async Task ConfirmOrderAsync(long orderId)
    {
        var order = await _repository.FindAsync(orderId);
        order.Confirm();
        await _repository.SaveChangesAsync(); // Events dispatched here
    }
}
```

Auto-registration:

```csharp
context.AutoRegisterRepositories(typeof(MyModule).Assembly);
```

## Unit of Work

Automatic with repository operations:

```csharp
// All operations in same scope share UoW
var order = await _orderRepo.FindAsync(orderId);
order.Confirm();
await _orderRepo.SaveChangesAsync(); // Commits UoW
```

## Best Practices

1. **Small aggregates**: Prefer smaller aggregates
2. **Reference by ID**: Cross-aggregate references use IDs
3. **Immutable value objects**: No setters on value objects
4. **Domain validation**: Validate in domain, not application
5. **Rich domain model**: Behavior in entities, not services
6. **Eventual consistency**: Use domain events between aggregates
