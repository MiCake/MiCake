using MiCake.DDD.Domain;

namespace MiCake.IntegrationTests.Fakes
{
    // Event Handlers for tracking (using static thread-safe collections)
    public class ProductPriceChangedHandler : IDomainEventHandler<ProductPriceChangedEvent>
    {
        private static readonly object _lock = new();
        private static readonly List<ProductPriceChangedEvent> _handledEvents = new();
        
        public static List<ProductPriceChangedEvent> HandledEvents 
        { 
            get
            {
                lock (_lock)
                {
                    return new List<ProductPriceChangedEvent>(_handledEvents);
                }
            }
        }

        public Task HandleAysnc(ProductPriceChangedEvent domainEvent, CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                System.Diagnostics.Debug.WriteLine($"[ProductPriceChangedHandler] Handling event: OldPrice={domainEvent.OldPrice}, NewPrice={domainEvent.NewPrice}");
                _handledEvents.Add(domainEvent);
            }
            return Task.CompletedTask;
        }

        public static void Clear()
        {
            lock (_lock)
            {
                _handledEvents.Clear();
            }
        }
    }

    public class ProductStockDecreasedHandler : IDomainEventHandler<ProductStockDecreasedEvent>
    {
        private static readonly object _lock = new();
        private static readonly List<ProductStockDecreasedEvent> _handledEvents = new();
        
        public static List<ProductStockDecreasedEvent> HandledEvents 
        { 
            get
            {
                lock (_lock)
                {
                    return new List<ProductStockDecreasedEvent>(_handledEvents);
                }
            }
        }

        public Task HandleAysnc(ProductStockDecreasedEvent domainEvent, CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                _handledEvents.Add(domainEvent);
            }
            return Task.CompletedTask;
        }

        public static void Clear()
        {
            lock (_lock)
            {
                _handledEvents.Clear();
            }
        }
    }

    public class OrderCompletedHandler : IDomainEventHandler<OrderCompletedEvent>
    {
        private static readonly object _lock = new();
        private static readonly List<OrderCompletedEvent> _handledEvents = new();
        
        public static List<OrderCompletedEvent> HandledEvents 
        { 
            get
            {
                lock (_lock)
                {
                    return new List<OrderCompletedEvent>(_handledEvents);
                }
            }
        }

        public Task HandleAysnc(OrderCompletedEvent domainEvent, CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                _handledEvents.Add(domainEvent);
            }
            return Task.CompletedTask;
        }

        public static void Clear()
        {
            lock (_lock)
            {
                _handledEvents.Clear();
            }
        }
    }
}
