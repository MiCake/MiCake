using MiCake.Core;
using MiCake.Core.DependencyInjection;
using MiCake.Core.Handlers;
using System.Collections.Generic;

namespace MiCake.IntegrationTests.Fakes
{
    /// <summary>
    /// Test services for DI and exception handling tests
    /// </summary>

    // Marker interfaces for DI tests
    public interface ITransientTestService : ITransientService
    {
        string GetId();
    }

    public interface IScopedTestService : IScopedService
    {
        string GetId();
    }

    public interface ISingletonTestService : ISingletonService
    {
        string GetId();
    }

    // Implementations
    public class TransientTestService : ITransientTestService
    {
        private readonly string _id;

        public TransientTestService()
        {
            _id = Guid.NewGuid().ToString();
        }

        public string GetId() => _id;
    }

    public class ScopedTestService : IScopedTestService
    {
        private readonly string _id;

        public ScopedTestService()
        {
            _id = Guid.NewGuid().ToString();
        }

        public string GetId() => _id;
    }

    public class SingletonTestService : ISingletonTestService
    {
        private readonly string _id;

        public SingletonTestService()
        {
            _id = Guid.NewGuid().ToString();
        }

        public string GetId() => _id;
    }

    /// <summary>
    /// Mock repository for query testing
    /// </summary>
    public class MockDataService
    {
        private readonly Dictionary<int, Product> _products = new();
        private int _nextId = 1;

        public void AddProduct(string name, decimal price, int stock)
        {
            var product = new Product
            {
                Id = _nextId++,
                Name = name,
                Price = price,
                Stock = stock
            };
            _products[product.Id] = product;
        }

        public Product? GetProduct(int id)
        {
            return _products.TryGetValue(id, out var product) ? product : null;
        }

        public IEnumerable<Product> GetAllProducts()
        {
            return _products.Values;
        }

        public int Count => _products.Count;

        public void Clear()
        {
            _products.Clear();
            _nextId = 1;
        }
    }

    /// <summary>
    /// Custom exception for testing
    /// </summary>
    public class TestBusinessException : Exception
    {
        public string ErrorCode { get; }

        public TestBusinessException(string message, string errorCode = "TEST_ERROR") 
            : base(message)
        {
            ErrorCode = errorCode;
        }
    }

    /// <summary>
    /// Test exception handler
    /// </summary>
    public class TestExceptionHandler : IMiCakeExceptionHandler
    {
        public static List<Exception> HandledExceptions { get; } = new();

        public Task Handle(MiCakeExceptionContext exceptionContext, CancellationToken cancellationToken = default)
        {
            HandledExceptions.Add(exceptionContext.Exception);
            return Task.CompletedTask;
        }

        public static void Clear()
        {
            HandledExceptions.Clear();
        }
    }
}
