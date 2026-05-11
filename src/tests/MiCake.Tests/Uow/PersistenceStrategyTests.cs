using MiCake.DDD.Uow;
using MiCake.DDD.Uow.Internal;
using Microsoft.Extensions.Logging;
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MiCake.Tests.Uow
{
    /// <summary>
    /// Unit tests for PersistenceStrategy and strategy-based behavior
    /// Tests verify that different persistence strategies correctly control transaction initialization and activation
    /// </summary>
    public class PersistenceStrategyTests
    {
        private readonly ILogger<UnitOfWork> _logger;
        private readonly ILogger<MockResource> _mockLogger;

        public PersistenceStrategyTests()
        {
            var loggerFactory = LoggerFactory.Create(builder => { });
            _logger = loggerFactory.CreateLogger<UnitOfWork>();
            _mockLogger = loggerFactory.CreateLogger<MockResource>();
        }

        #region TransactionManaged Strategy Tests

        [Fact]
        public void PrepareForTransaction_WithTransactionManagedStrategy_ShouldStorePersistenceStrategy()
        {
            // Arrange
            var options = new UnitOfWorkOptions
            {
                Strategy = PersistenceStrategy.TransactionManaged,
                IsolationLevel = IsolationLevel.ReadCommitted
            };
            var mockResource = new MockResource(_mockLogger);

            // Act
            mockResource.PrepareForTransaction(options);

            // Assert
            Assert.Equal(PersistenceStrategy.TransactionManaged, mockResource.StoredStrategy);
            Assert.Equal(IsolationLevel.ReadCommitted, mockResource.StoredIsolationLevel);
        }

        [Fact]
        public async Task ActivateTransactionAsync_WithTransactionManagedStrategy_ShouldStartExplicitTransaction()
        {
            // Arrange
            var options = new UnitOfWorkOptions
            {
                Strategy = PersistenceStrategy.TransactionManaged
            };
            var mockResource = new MockResource(_mockLogger);
            mockResource.PrepareForTransaction(options);

            // Act
            await mockResource.ActivateTransactionAsync();

            // Assert
            Assert.True(mockResource.TransactionStarted, "Explicit transaction should be started for TransactionManaged strategy");
        }

        [Fact]
        public async Task TransactionManaged_WithDifferentIsolationLevels_ShouldPropagateCorrectly()
        {
            // Arrange
            var isolationLevels = new[] 
            { 
                IsolationLevel.ReadUncommitted,
                IsolationLevel.ReadCommitted,
                IsolationLevel.RepeatableRead,
                IsolationLevel.Serializable
            };

            foreach (var isolationLevel in isolationLevels)
            {
                var options = new UnitOfWorkOptions
                {
                    Strategy = PersistenceStrategy.TransactionManaged,
                    IsolationLevel = isolationLevel
                };
                var mockResource = new MockResource(_mockLogger);

                // Act
                mockResource.PrepareForTransaction(options);
                await mockResource.ActivateTransactionAsync();

                // Assert
                Assert.Equal(isolationLevel, mockResource.StoredIsolationLevel);
            }
        }

        #endregion

        #region OptimizeForSingleWrite Strategy Tests

        [Fact]
        public void PrepareForTransaction_WithOptimizeForSingleWriteStrategy_ShouldStorePersistenceStrategy()
        {
            // Arrange
            var options = new UnitOfWorkOptions
            {
                Strategy = PersistenceStrategy.OptimizeForSingleWrite
            };
            var mockResource = new MockResource(_mockLogger);

            // Act
            mockResource.PrepareForTransaction(options);

            // Assert
            Assert.Equal(PersistenceStrategy.OptimizeForSingleWrite, mockResource.StoredStrategy);
        }

        [Fact]
        public async Task ActivateTransactionAsync_WithOptimizeForSingleWriteStrategy_ShouldSkipExplicitTransaction()
        {
            // Arrange
            var options = new UnitOfWorkOptions
            {
                Strategy = PersistenceStrategy.OptimizeForSingleWrite
            };
            var mockResource = new MockResource(_mockLogger);
            mockResource.PrepareForTransaction(options);

            // Act
            await mockResource.ActivateTransactionAsync();

            // Assert
            Assert.False(mockResource.TransactionStarted, "Explicit transaction should NOT be started for OptimizeForSingleWrite strategy");
        }

        #endregion

        #region Default Options Tests

        [Fact]
        public void UnitOfWorkOptions_Default_ShouldUseOptimizeForSingleWriteStrategy()
        {
            // Act & Assert
            Assert.Equal(PersistenceStrategy.OptimizeForSingleWrite, UnitOfWorkOptions.Default.Strategy);
            Assert.Equal(TransactionInitializationMode.Lazy, UnitOfWorkOptions.Default.InitializationMode);
        }

        [Fact]
        public void UnitOfWorkOptions_Immediate_ShouldUseTransactionManagedStrategy()
        {
            // Act & Assert
            Assert.Equal(PersistenceStrategy.TransactionManaged, UnitOfWorkOptions.Immediate.Strategy);
            Assert.Equal(TransactionInitializationMode.Immediate, UnitOfWorkOptions.Immediate.InitializationMode);
        }

        [Fact]
        public void UnitOfWorkOptions_ReadOnly_ShouldSetIsReadOnlyFlag()
        {
            // Act & Assert
            Assert.True(UnitOfWorkOptions.ReadOnly.IsReadOnly);
        }

        #endregion

        #region Strategy Switching Tests

        [Fact]
        public async Task SwitchingStrategy_BetweenDifferentResources_ShouldWorkCorrectly()
        {
            // Arrange
            var optionsManaged = new UnitOfWorkOptions { Strategy = PersistenceStrategy.TransactionManaged };
            var optionsOptimized = new UnitOfWorkOptions { Strategy = PersistenceStrategy.OptimizeForSingleWrite };

            var resource1 = new MockResource(_mockLogger);
            var resource2 = new MockResource(_mockLogger);

            // Act
            resource1.PrepareForTransaction(optionsManaged);
            resource2.PrepareForTransaction(optionsOptimized);

            await resource1.ActivateTransactionAsync();
            await resource2.ActivateTransactionAsync();

            // Assert
            Assert.True(resource1.TransactionStarted);
            Assert.False(resource2.TransactionStarted);
        }

        #endregion

        #region UnitOfWork with Strategy Tests

        [Fact]
        public void UnitOfWork_RegisterResource_ShouldPropagateStrategyToResource()
        {
            // Arrange
            var options = new UnitOfWorkOptions { Strategy = PersistenceStrategy.TransactionManaged };
            var uow = new UnitOfWork(_logger, options, null);
            var mockResource = new MockResource(_mockLogger);

            // Act
            uow.RegisterResource(mockResource);

            // Assert
            Assert.Equal(PersistenceStrategy.TransactionManaged, mockResource.StoredStrategy);
        }

        #endregion

        #region Helper Class

        /// <summary>
        /// Mock resource for testing strategy propagation without EF Core dependencies
        /// </summary>
        private class MockResource : IUnitOfWorkResource
        {
            private readonly ILogger<MockResource> _logger;
            public string ResourceIdentifier => "MockResource";
            public PersistenceStrategy StoredStrategy { get; private set; }
            public IsolationLevel? StoredIsolationLevel { get; private set; }
            public bool TransactionStarted { get; private set; }
            public bool HasActiveTransaction { get; private set; }
            public bool IsInitialized { get; private set; }

            public MockResource(ILogger<MockResource> logger)
            {
                _logger = logger;
            }

            public void PrepareForTransaction(UnitOfWorkOptions options)
            {
                ArgumentNullException.ThrowIfNull(options);
                StoredStrategy = options.Strategy;
                StoredIsolationLevel = options.IsolationLevel;
                _logger.LogDebug("Resource prepared with Strategy={Strategy}, IsolationLevel={IsolationLevel}",
                    options.Strategy, options.IsolationLevel);
            }

            public async Task ActivateTransactionAsync(CancellationToken cancellationToken = default)
            {
                await Task.Delay(10, cancellationToken).ConfigureAwait(false);

                switch (StoredStrategy)
                {
                    case PersistenceStrategy.OptimizeForSingleWrite:
                        TransactionStarted = false;
                        HasActiveTransaction = false;
                        IsInitialized = true;
                        _logger.LogDebug("Skipping explicit transaction for OptimizeForSingleWrite");
                        break;

                    case PersistenceStrategy.TransactionManaged:
                        TransactionStarted = true;
                        HasActiveTransaction = true;
                        IsInitialized = true;
                        _logger.LogDebug("Started explicit transaction with IsolationLevel={IsolationLevel}", StoredIsolationLevel);
                        break;

                    default:
                        throw new InvalidOperationException($"Unknown strategy: {StoredStrategy}");
                }
            }

            public async Task BeginTransactionAsync(UnitOfWorkOptions options, CancellationToken cancellationToken = default)
            {
                PrepareForTransaction(options);
                await ActivateTransactionAsync(cancellationToken).ConfigureAwait(false);
            }

            public Task CommitAsync(CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }

            public Task RollbackAsync(CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }

            public Task SaveChangesAsync(CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }

            public Task<string> CreateSavepointAsync(string name, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(name);
            }

            public Task RollbackToSavepointAsync(string name, CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }

            public Task ReleaseSavepointAsync(string name, CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }

            public void Dispose()
            {
            }
        }

        #endregion
    }
}
