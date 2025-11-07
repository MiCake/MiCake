using MiCake.Audit;
using MiCake.Audit.SoftDeletion;
using MiCake.DDD.Domain;

namespace MiCake.IntegrationTests.Fakes
{
    // Test Entities
    public class Product : AggregateRoot<int>, IHasCreationTime, IHasModificationTime
    {
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public DateTime CreationTime { get; set; }
        public DateTime? ModificationTime { get; set; }

        public void UpdatePrice(decimal newPrice)
        {
            var oldPrice = Price;
            Price = newPrice;
            AddDomainEvent(new ProductPriceChangedEvent(Id, oldPrice, newPrice));
        }

        public void DecreaseStock(int quantity)
        {
            if (Stock < quantity)
                throw new InvalidOperationException("Insufficient stock");

            Stock -= quantity;
            AddDomainEvent(new ProductStockDecreasedEvent(Id, quantity));
        }
    }

    public class Order : AggregateRoot<int>, IHasCreationTime, IHasModificationTime, ISoftDeletion, IHasDeletionTime
    {
        public string OrderNumber { get; set; } = string.Empty;
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal TotalAmount { get; set; }
        public OrderStatus Status { get; set; }
        public DateTime CreationTime { get; set; }
        public DateTime? ModificationTime { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletionTime { get; set; }

        public void Complete()
        {
            Status = OrderStatus.Completed;
            AddDomainEvent(new OrderCompletedEvent(Id, OrderNumber));
        }

        public void Cancel()
        {
            Status = OrderStatus.Cancelled;
            AddDomainEvent(new OrderCancelledEvent(Id, OrderNumber));
        }
    }

    public enum OrderStatus
    {
        Pending = 0,
        Completed = 1,
        Cancelled = 2
    }

    // Domain Events
    public class ProductPriceChangedEvent : DomainEvent
    {
        public int ProductId { get; }
        public decimal OldPrice { get; }
        public decimal NewPrice { get; }

        public ProductPriceChangedEvent(int productId, decimal oldPrice, decimal newPrice)
        {
            ProductId = productId;
            OldPrice = oldPrice;
            NewPrice = newPrice;
        }
    }

    public class ProductStockDecreasedEvent : DomainEvent
    {
        public int ProductId { get; }
        public int Quantity { get; }

        public ProductStockDecreasedEvent(int productId, int quantity)
        {
            ProductId = productId;
            Quantity = quantity;
        }
    }

    public class OrderCompletedEvent : DomainEvent
    {
        public int OrderId { get; }
        public string OrderNumber { get; }

        public OrderCompletedEvent(int orderId, string orderNumber)
        {
            OrderId = orderId;
            OrderNumber = orderNumber;
        }
    }

    public class OrderCancelledEvent : DomainEvent
    {
        public int OrderId { get; }
        public string OrderNumber { get; }

        public OrderCancelledEvent(int orderId, string orderNumber)
        {
            OrderId = orderId;
            OrderNumber = orderNumber;
        }
    }
}
