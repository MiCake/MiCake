using MiCake.DDD.Domain;

namespace MiCake.IntegrationTests.EntityFrameworkCore
{
    /// <summary>
    /// Test order aggregate root for integration tests
    /// </summary>
    public class Order : AggregateRoot<int>
    {
        public string OrderNumber { get; init; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public OrderStatus Status { get; set; }
        public DateTime CreatedDate { get; set; }

        public void Ship()
        {
            Status = OrderStatus.Shipped;
            RaiseDomainEvent(new OrderShippedEvent(Id));
        }

        public void Complete()
        {
            Status = OrderStatus.Completed;
            RaiseDomainEvent(new OrderCompletedEvent(Id));
        }
    }

    public enum OrderStatus
    {
        Pending = 0,
        Shipped = 1,
        Completed = 2,
        Cancelled = 3
    }

    /// <summary>
    /// Test product aggregate root for integration tests
    /// </summary>
    public class Product : AggregateRoot<int>
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public bool IsActive { get; set; } = true;

        public void UpdateStock(int quantity)
        {
            StockQuantity = quantity;
        }
    }

    /// <summary>
    /// Domain event fired when an order is shipped
    /// </summary>
    public class OrderShippedEvent : IDomainEvent
    {
        public int OrderId { get; }

        public OrderShippedEvent(int orderId)
        {
            OrderId = orderId;
        }
    }

    /// <summary>
    /// Domain event fired when an order is completed
    /// </summary>
    public class OrderCompletedEvent : IDomainEvent
    {
        public int OrderId { get; }

        public OrderCompletedEvent(int orderId)
        {
            OrderId = orderId;
        }
    }
}
