# MiCake Coding Standards

Based on `principles/development_principle.md`.

## Naming Conventions

| Element | Convention | Example |
|---------|------------|---------|
| Public types | PascalCase | `OrderService` |
| Public members | PascalCase | `ProcessOrder()` |
| Private fields | _camelCase | `_orderRepository` |
| Parameters | camelCase | `orderId` |
| Local variables | camelCase | `currentOrder` |
| Constants | PascalCase | `MaxRetryCount` |
| Async methods | Async suffix | `ProcessOrderAsync()` |
| Interfaces | I prefix | `IOrderService` |

## Dependency Injection

### Do

```csharp
public class OrderService
{
    private readonly IOrderRepository _repository;
    private readonly ILogger<OrderService> _logger;
    
    public OrderService(IOrderRepository repository, ILogger<OrderService> logger)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(logger);
        _repository = repository;
        _logger = logger;
    }
}
```

### Don't

```csharp
// DON'T: Service locator pattern
public class OrderService
{
    private readonly IServiceProvider _provider;
    
    public OrderService(IServiceProvider provider)
    {
        _provider = provider; // Anti-pattern
    }
}
```

### Dependency Wrapper (2+ dependencies)

```csharp
public class OrderServiceDependencies
{
    public IOrderRepository Repository { get; init; }
    public ILogger<OrderService> Logger { get; init; }
    public IEventDispatcher EventDispatcher { get; init; }
}

public class OrderService
{
    private readonly OrderServiceDependencies _deps;
    
    public OrderService(OrderServiceDependencies deps)
    {
        ArgumentNullException.ThrowIfNull(deps);
        _deps = deps;
    }
}
```

## Async Programming

### Rules

1. Always use `ConfigureAwait(false)` in library code
2. Accept `CancellationToken` parameter
3. Use `Async` suffix
4. Never block with `.Result`, `.Wait()`, or `GetAwaiter().GetResult()`

### Example

```csharp
public async Task<Order?> GetOrderAsync(
    long orderId, 
    CancellationToken cancellationToken = default)
{
    var order = await _repository
        .FindAsync(orderId, cancellationToken)
        .ConfigureAwait(false);
    
    if (order == null)
    {
        _logger.LogWarning("Order {OrderId} not found", orderId);
        return null;
    }
    
    return order;
}
```

## Input Validation

```csharp
public void ProcessOrder(Order order, string userId)
{
    ArgumentNullException.ThrowIfNull(order);
    ArgumentException.ThrowIfNullOrWhiteSpace(userId);
    
    // Process...
}
```

## Exception Handling

```csharp
public async Task<Order> GetRequiredOrderAsync(long orderId)
{
    var order = await _repository.FindAsync(orderId).ConfigureAwait(false);
    
    if (order == null)
    {
        _logger.LogError("Order {OrderId} not found", orderId);
        throw new EntityNotFoundException(typeof(Order), orderId);
    }
    
    return order;
}
```

## Dispose Pattern

```csharp
public class ResourceHolder : IDisposable
{
    private bool _disposed;
    private readonly Stream _stream;
    
    public void DoWork()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        // Use _stream...
    }
    
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
    
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;
        
        if (disposing)
        {
            try { _stream?.Dispose(); }
            catch { /* Swallow during dispose */ }
        }
        
        _disposed = true;
    }
}
```

## XML Documentation

```csharp
/// <summary>
/// Processes an order and dispatches relevant domain events.
/// </summary>
/// <param name="orderId">The unique identifier of the order to process.</param>
/// <param name="cancellationToken">Cancellation token for the operation.</param>
/// <returns>The processed order, or null if not found.</returns>
/// <exception cref="InvalidOperationException">
/// Thrown when the order is in an invalid state for processing.
/// </exception>
public async Task<Order?> ProcessOrderAsync(
    long orderId, 
    CancellationToken cancellationToken = default)
```

## Performance

### Caching

```csharp
private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _propertyCache = new();

public PropertyInfo[] GetProperties(Type type)
{
    return _propertyCache.GetOrAdd(type, t => t.GetProperties());
}
```

### Compiled Activators

```csharp
// DON'T
var instance = Activator.CreateInstance(type);

// DO - Use compiled expression trees
var factory = CompiledActivator.GetFactory(type);
var instance = factory();
```

### Avoid O(n) on Hot Paths

```csharp
// DON'T
var item = list.FirstOrDefault(x => x.Id == id); // O(n)

// DO
var item = dictionary[id]; // O(1)
```

## Logging

```csharp
// Include context in logs
_logger.LogInformation(
    "Processing order {OrderId} for customer {CustomerId}", 
    order.Id, 
    order.CustomerId);

// Use appropriate levels
_logger.LogDebug("Detailed operation info");
_logger.LogInformation("Normal operations");
_logger.LogWarning("Unexpected but handled");
_logger.LogError(ex, "Operation failed for order {OrderId}", orderId);
```
