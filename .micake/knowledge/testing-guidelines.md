# MiCake Testing Guidelines

Based on `principles/development_principle.md`.

## Test Project Structure

```
src/tests/
├── MiCake.Core.Tests/
├── MiCake.Tests/                 # DDD tests
├── MiCake.AspNetCore.Tests/
└── MiCake.EntityFrameworkCore.Tests/
```

## Test Naming

Pattern: `{Method}_{Scenario}_{ExpectedResult}`

Examples:
- `FindAsync_ExistingEntity_ReturnsEntity`
- `FindAsync_NonExistentId_ReturnsNull`
- `Add_NullEntity_ThrowsArgumentNullException`
- `Save_ValidAggregate_DispatchesDomainEvents`

## AAA Pattern

```csharp
[Fact]
public void MethodName_Scenario_ExpectedResult()
{
    // Arrange
    var input = CreateTestInput();
    var sut = CreateSystemUnderTest();
    
    // Act
    var result = sut.Method(input);
    
    // Assert
    Assert.Equal(expected, result);
}
```

## Test Categories

### Unit Tests

Test individual classes in isolation.

```csharp
public class OrderServiceTests
{
    private readonly Mock<IOrderRepository> _repositoryMock;
    private readonly OrderService _sut;
    
    public OrderServiceTests()
    {
        _repositoryMock = new Mock<IOrderRepository>();
        _sut = new OrderService(_repositoryMock.Object);
    }
    
    [Fact]
    public async Task GetOrderAsync_ExistingOrder_ReturnsOrder()
    {
        // Arrange
        var order = new Order("Customer");
        _repositoryMock
            .Setup(r => r.FindAsync(1L, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        
        // Act
        var result = await _sut.GetOrderAsync(1L);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal("Customer", result.CustomerName);
    }
}
```

### Integration Tests

Test component interactions with real infrastructure.

```csharp
public class RepositoryIntegrationTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;
    
    public RepositoryIntegrationTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }
    
    [Fact]
    public async Task SaveChangesAsync_NewAggregate_PersistsToDatabase()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var repository = new EfRepository<Order, long>(context);
        var order = new Order("Test Customer");
        
        // Act
        repository.Add(order);
        await repository.SaveChangesAsync();
        
        // Assert
        var saved = await context.Orders.FindAsync(order.Id);
        Assert.NotNull(saved);
    }
}
```

## Test Scenarios

| Category | Scenarios |
|----------|-----------|
| Happy Path | Valid input, normal execution |
| Null Input | Null parameters, null return |
| Empty | Empty collections, empty strings |
| Boundary | Min/max values, limits |
| Error | Exceptions, failure paths |
| Async | Cancellation, timeouts |
| Dispose | Resource cleanup |
| Concurrency | Thread safety |

## Mocking Guidelines

### Mock External Dependencies Only

```csharp
// DO: Mock infrastructure
var repositoryMock = new Mock<IOrderRepository>();

// DON'T: Mock domain objects
var orderMock = new Mock<Order>(); // Anti-pattern
```

### Setup Expectations Clearly

```csharp
_repositoryMock
    .Setup(r => r.FindAsync(
        It.Is<long>(id => id > 0), 
        It.IsAny<CancellationToken>()))
    .ReturnsAsync(order);
```

### Verify Interactions When Needed

```csharp
// After Act
_repositoryMock.Verify(
    r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), 
    Times.Once);
```

## Assertion Best Practices

```csharp
// Prefer specific assertions
Assert.Equal(expected, actual);
Assert.NotNull(result);
Assert.True(result.IsValid);
Assert.Contains(item, collection);
Assert.Throws<ArgumentNullException>(() => sut.Method(null));

// Async exception testing
await Assert.ThrowsAsync<InvalidOperationException>(
    () => sut.ProcessAsync());
```

## Test Data

### Factory Methods

```csharp
private static Order CreateValidOrder(string customerName = "Test Customer")
{
    return new Order(customerName);
}

private static OrderLine CreateOrderLine(decimal price = 10.0m)
{
    return new OrderLine(productId: 1, quantity: 1, price);
}
```

### Builder Pattern

```csharp
public class OrderBuilder
{
    private string _customerName = "Default";
    private List<OrderLine> _lines = new();
    
    public OrderBuilder WithCustomer(string name)
    {
        _customerName = name;
        return this;
    }
    
    public OrderBuilder WithLine(decimal price)
    {
        _lines.Add(new OrderLine(1, 1, price));
        return this;
    }
    
    public Order Build()
    {
        var order = new Order(_customerName);
        foreach (var line in _lines)
            order.AddLine(line);
        return order;
    }
}
```

## Regression Tests

For every bug fix, add a test that:

1. Reproduces the bug (fails before fix)
2. Passes after fix
3. Documents the issue

```csharp
[Fact]
public void Issue123_OrderTotalCalculation_IncludesDiscount()
{
    // This test was added for issue #123
    // Arrange
    var order = new Order("Customer");
    order.AddLine(new OrderLine(1, 1, 100m));
    order.ApplyDiscount(10m);
    
    // Act
    var total = order.CalculateTotal();
    
    // Assert - Was incorrectly returning 100 before fix
    Assert.Equal(90m, total);
}
```

## Running Tests

```bash
# All tests
dotnet test

# Specific project
dotnet test src/tests/MiCake.Core.Tests

# With coverage
dotnet test --collect:"XPlat Code Coverage"

# Using build script
.\build.cmd
```
