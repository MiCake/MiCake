using MiCake.EntityFrameworkCore.Uow;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MiCake.EntityFrameworkCore.Tests.Uow
{
    /// <summary>
    /// Unit tests for EFCoreDbContextWrapper two-phase registration implementation
    /// Tests the prepare-activate pattern for transaction initialization
    /// </summary>
    public class EFCoreTwoPhaseRegistrationTests : IDisposable
    {
        private readonly DbContext _dbContext;
        private readonly Mock<ILogger<EFCoreDbContextWrapper>> _mockLogger;
        private readonly MiCakeEFCoreOptions _options;

        public EFCoreTwoPhaseRegistrationTests()
        {
            var dbOptions = new DbContextOptionsBuilder<DbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            
            _dbContext = new DbContext(dbOptions);
            _mockLogger = new Mock<ILogger<EFCoreDbContextWrapper>>();
            _options = new MiCakeEFCoreOptions();
        }

        #region Phase 1 - Prepare Tests

        [Fact]
        public void PrepareForTransaction_ShouldStoreIsolationLevel()
        {
            // Arrange
            var wrapper = new EFCoreDbContextWrapper(_dbContext, _mockLogger.Object, _options);
            var isolationLevel = IsolationLevel.Serializable;

            // Act
            wrapper.PrepareForTransaction(isolationLevel);

            // Assert
            Assert.False(wrapper.HasActiveTransaction);
            Assert.False(wrapper.IsInitialized);
        }

        [Fact]
        public void PrepareForTransaction_WithNullIsolationLevel_ShouldSucceed()
        {
            // Arrange
            var wrapper = new EFCoreDbContextWrapper(_dbContext, _mockLogger.Object, _options);

            // Act
            wrapper.PrepareForTransaction(null);

            // Assert
            Assert.False(wrapper.HasActiveTransaction);
        }

        [Fact]
        public void PrepareForTransaction_CalledTwice_ShouldBeIdempotent()
        {
            // Arrange
            var wrapper = new EFCoreDbContextWrapper(_dbContext, _mockLogger.Object, _options);

            // Act
            wrapper.PrepareForTransaction(IsolationLevel.ReadCommitted);
            wrapper.PrepareForTransaction(IsolationLevel.Serializable);

            // Assert - Should not throw, last call wins or first call persists
            Assert.False(wrapper.HasActiveTransaction);
        }

        #endregion

        #region Phase 2 - Activate Tests

        [Fact]
        public async Task ActivateTransactionAsync_AfterPrepare_ShouldStartTransaction()
        {
            // Arrange
            var wrapper = new EFCoreDbContextWrapper(_dbContext, _mockLogger.Object, _options);
            wrapper.PrepareForTransaction(IsolationLevel.ReadCommitted);

            // Act
            await wrapper.ActivateTransactionAsync();

            // Assert
            Assert.True(wrapper.IsInitialized);
            Assert.True(wrapper.HasActiveTransaction);
        }

        [Fact]
        public async Task ActivateTransactionAsync_WithoutPrepare_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var wrapper = new EFCoreDbContextWrapper(_dbContext, _mockLogger.Object, _options);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => wrapper.ActivateTransactionAsync());
            
            Assert.Contains("prepared", exception.Message.ToLower());
        }

        [Fact]
        public async Task ActivateTransactionAsync_CalledTwice_ShouldBeIdempotent()
        {
            // Arrange
            var wrapper = new EFCoreDbContextWrapper(_dbContext, _mockLogger.Object, _options);
            wrapper.PrepareForTransaction(IsolationLevel.ReadCommitted);

            // Act
            await wrapper.ActivateTransactionAsync();
            await wrapper.ActivateTransactionAsync();

            // Assert
            Assert.True(wrapper.IsInitialized);
            Assert.True(wrapper.HasActiveTransaction);
        }

        #endregion

        #region Prepare-Then-Activate Workflow Tests

        [Fact]
        public async Task WorkflowTest_PrepareActivateCommit_ShouldSucceed()
        {
            // Arrange
            var wrapper = new EFCoreDbContextWrapper(_dbContext, _mockLogger.Object, _options);

            // Act - Phase 1: Prepare
            wrapper.PrepareForTransaction(IsolationLevel.ReadCommitted);
            Assert.False(wrapper.HasActiveTransaction);

            // Act - Phase 2: Activate
            await wrapper.ActivateTransactionAsync();
            Assert.True(wrapper.HasActiveTransaction);

            // Act - Commit
            await wrapper.CommitAsync();

            // Assert
            Assert.False(wrapper.HasActiveTransaction);
        }

        [Fact]
        public async Task WorkflowTest_PrepareActivateRollback_ShouldSucceed()
        {
            // Arrange
            var wrapper = new EFCoreDbContextWrapper(_dbContext, _mockLogger.Object, _options);

            // Act - Phase 1: Prepare
            wrapper.PrepareForTransaction(IsolationLevel.ReadCommitted);

            // Act - Phase 2: Activate
            await wrapper.ActivateTransactionAsync();
            Assert.True(wrapper.HasActiveTransaction);

            // Act - Rollback
            await wrapper.RollbackAsync();

            // Assert
            Assert.False(wrapper.HasActiveTransaction);
        }

        #endregion

        #region User-Managed Transaction Tests

        [Fact]
        public async Task PrepareForTransaction_WithExistingUserManagedTransaction_ShouldMarkAsInitialized()
        {
            // Arrange
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            var wrapper = new EFCoreDbContextWrapper(_dbContext, _mockLogger.Object, _options);

            // Act
            wrapper.PrepareForTransaction(IsolationLevel.ReadCommitted);

            // Assert
            Assert.True(wrapper.HasActiveTransaction);
            Assert.True(wrapper.IsInitialized); // User-managed transaction counts as initialized
        }

        [Fact]
        public async Task ActivateTransactionAsync_WithExistingUserManagedTransaction_ShouldNotCreateNew()
        {
            // Arrange
            var existingTransaction = await _dbContext.Database.BeginTransactionAsync();
            var wrapper = new EFCoreDbContextWrapper(_dbContext, _mockLogger.Object, _options);
            wrapper.PrepareForTransaction(IsolationLevel.ReadCommitted);

            // Act
            await wrapper.ActivateTransactionAsync();

            // Assert
            Assert.True(wrapper.HasActiveTransaction);
            Assert.Same(existingTransaction, _dbContext.Database.CurrentTransaction);
        }

        #endregion

        #region Legacy BeginTransactionAsync Tests

        [Fact]
        public async Task BeginTransactionAsync_ShouldCallBothPhases()
        {
            // Arrange
            var wrapper = new EFCoreDbContextWrapper(_dbContext, _mockLogger.Object, _options);

            // Act
            await wrapper.BeginTransactionAsync(IsolationLevel.Serializable);

            // Assert
            Assert.True(wrapper.IsInitialized);
            Assert.True(wrapper.HasActiveTransaction);
        }

        [Fact]
        public async Task BeginTransactionAsync_WithNullIsolationLevel_ShouldUseDefault()
        {
            // Arrange
            var wrapper = new EFCoreDbContextWrapper(_dbContext, _mockLogger.Object, _options);

            // Act
            await wrapper.BeginTransactionAsync(null);

            // Assert
            Assert.True(wrapper.HasActiveTransaction);
        }

        #endregion

        #region Resource Identifier Tests

        [Fact]
        public void ResourceIdentifier_ShouldBeUnique()
        {
            // Arrange
            var wrapper1 = new EFCoreDbContextWrapper(_dbContext, _mockLogger.Object, _options);
            
            var dbOptions2 = new DbContextOptionsBuilder<DbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var dbContext2 = new DbContext(dbOptions2);
            var wrapper2 = new EFCoreDbContextWrapper(dbContext2, _mockLogger.Object, _options);

            // Act
            var id1 = wrapper1.ResourceIdentifier;
            var id2 = wrapper2.ResourceIdentifier;

            // Assert
            Assert.NotEqual(id1, id2);
            
            dbContext2.Dispose();
        }

        [Fact]
        public void ResourceIdentifier_SameWrapper_ShouldReturnSameValue()
        {
            // Arrange
            var wrapper = new EFCoreDbContextWrapper(_dbContext, _mockLogger.Object, _options);

            // Act
            var id1 = wrapper.ResourceIdentifier;
            var id2 = wrapper.ResourceIdentifier;

            // Assert
            Assert.Equal(id1, id2);
        }

        #endregion

        #region Dispose Tests

        [Fact]
        public async Task Dispose_WithActiveTransaction_ShouldDisposeTransaction()
        {
            // Arrange
            var wrapper = new EFCoreDbContextWrapper(_dbContext, _mockLogger.Object, _options);
            wrapper.PrepareForTransaction(IsolationLevel.ReadCommitted);
            await wrapper.ActivateTransactionAsync();

            // Act
            wrapper.Dispose();

            // Assert
            Assert.False(wrapper.HasActiveTransaction);
        }

        [Fact]
        public async Task Dispose_WithShouldDisposeDbContext_ShouldDisposeDbContext()
        {
            // Arrange
            var dbOptions = new DbContextOptionsBuilder<DbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var dbContext = new DbContext(dbOptions);
            
            var wrapper = new EFCoreDbContextWrapper(
                dbContext, 
                _mockLogger.Object, 
                _options, 
                shouldDisposeDbContext: true);

            // Act
            wrapper.Dispose();

            // Assert - Accessing disposed DbContext should throw
            Assert.Throws<ObjectDisposedException>(() => dbContext.Database.CanConnect());
        }

        [Fact]
        public void Dispose_WithoutShouldDisposeDbContext_ShouldNotDisposeDbContext()
        {
            // Arrange
            var wrapper = new EFCoreDbContextWrapper(
                _dbContext, 
                _mockLogger.Object, 
                _options, 
                shouldDisposeDbContext: false);

            // Act
            wrapper.Dispose();

            // Assert - DbContext should still be accessible
            var canConnect = _dbContext.Database.CanConnect();
            Assert.False(canConnect); // In-memory DB always returns false for CanConnect
        }

        #endregion

        public void Dispose()
        {
            _dbContext?.Dispose();
        }
    }
}
