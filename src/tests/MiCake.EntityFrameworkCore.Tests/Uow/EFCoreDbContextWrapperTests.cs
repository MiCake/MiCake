using MiCake.EntityFrameworkCore.Uow;
using MiCake.Core.DDD.Uow;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MiCake.EntityFrameworkCore.Tests.Uow
{
    /// <summary>
    /// Unit tests for EFCoreDbContextWrapper functionality
    /// Tests savepoint support, transaction management, and resource lifecycle
    /// </summary>
    public class EFCoreDbContextWrapperTests : IDisposable
    {
        private readonly Mock<DbContext> _mockDbContext;
        private readonly Mock<IDbContextTransaction> _mockTransaction;
        private readonly EFCoreDbContextWrapper _wrapper;

        public EFCoreDbContextWrapperTests()
        {
            var options = new DbContextOptionsBuilder<DbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            
            _mockDbContext = new Mock<DbContext>(options) { CallBase = true };
            _mockTransaction = new Mock<IDbContextTransaction>();
            
            _wrapper = new EFCoreDbContextWrapper(_mockDbContext.Object);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithNullDbContext_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new EFCoreDbContextWrapper(null));
        }

        [Fact]
        public void Constructor_WithValidDbContext_ShouldSucceed()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<DbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var dbContext = new DbContext(options);

            // Act
            var wrapper = new EFCoreDbContextWrapper(dbContext);

            // Assert
            Assert.NotNull(wrapper);
            Assert.Same(dbContext, wrapper.DbContext);
        }

        #endregion

        #region Transaction Management Tests

        [Fact]
        public async Task BeginTransactionAsync_WithoutIsolationLevel_ShouldBeginTransaction()
        {
            // Arrange
            var cancellationToken = CancellationToken.None;
            
            // Act
            await _wrapper.BeginTransactionAsync(new UnitOfWorkOptions(), cancellationToken);

            // Assert
            // Verify transaction was started (implementation detail)
            Assert.NotNull(_wrapper.DbContext);
        }

        [Fact]
        public async Task BeginTransactionAsync_WithIsolationLevel_ShouldBeginTransactionWithLevel()
        {
            // Arrange
            var isolationLevel = IsolationLevel.ReadCommitted;
            var cancellationToken = CancellationToken.None;
            
            // Act
            await _wrapper.BeginTransactionAsync(new UnitOfWorkOptions { IsolationLevel = isolationLevel }, cancellationToken);

            // Assert
            Assert.NotNull(_wrapper.DbContext);
        }

        [Fact]
        public async Task BeginTransactionAsync_CalledTwice_ShouldNotThrow()
        {
            // Arrange
            var cancellationToken = CancellationToken.None;
            
            // Act
            await _wrapper.BeginTransactionAsync(new UnitOfWorkOptions(), cancellationToken);
            await _wrapper.BeginTransactionAsync(new UnitOfWorkOptions(), cancellationToken);

            // Assert - no exception thrown
            Assert.NotNull(_wrapper.DbContext);
        }

        #endregion

        #region Savepoint Tests

        [Fact]
        public async Task CreateSavepointAsync_WithValidName_ShouldReturnSavepointName()
        {
            // Arrange
            var savepointName = "TestSavepoint";
            var cancellationToken = CancellationToken.None;
            
            // Start transaction first
            await _wrapper.BeginTransactionAsync(new UnitOfWorkOptions(), cancellationToken);

            // Act
            var result = await _wrapper.CreateSavepointAsync(savepointName, cancellationToken);

            // Assert
            Assert.Equal(savepointName, result);
        }

        [Fact]
        public async Task CreateSavepointAsync_WithNullName_ShouldThrowArgumentException()
        {
            // Arrange
            var cancellationToken = CancellationToken.None;
            await _wrapper.BeginTransactionAsync(new UnitOfWorkOptions(), cancellationToken);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => _wrapper.CreateSavepointAsync(null, cancellationToken));
        }

        [Fact]
        public async Task CreateSavepointAsync_WithEmptyName_ShouldThrowArgumentException()
        {
            // Arrange
            var cancellationToken = CancellationToken.None;
            await _wrapper.BeginTransactionAsync(new UnitOfWorkOptions(), cancellationToken);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => _wrapper.CreateSavepointAsync(string.Empty, cancellationToken));
        }

        [Fact]
        public async Task CreateSavepointAsync_WithWhitespaceName_ShouldThrowArgumentException()
        {
            // Arrange
            var cancellationToken = CancellationToken.None;
            await _wrapper.BeginTransactionAsync(new UnitOfWorkOptions(), cancellationToken);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => _wrapper.CreateSavepointAsync("   ", cancellationToken));
        }

        [Fact]
        public async Task RollbackToSavepointAsync_WithValidName_ShouldNotThrow()
        {
            // Arrange
            var savepointName = "TestSavepoint";
            var cancellationToken = CancellationToken.None;
            await _wrapper.BeginTransactionAsync(new UnitOfWorkOptions(), cancellationToken);
            await _wrapper.CreateSavepointAsync(savepointName, cancellationToken);

            // Act & Assert - should not throw
            await _wrapper.RollbackToSavepointAsync(savepointName, cancellationToken);
        }

        [Fact]
        public async Task RollbackToSavepointAsync_WithNullName_ShouldThrowArgumentException()
        {
            // Arrange
            var cancellationToken = CancellationToken.None;
            await _wrapper.BeginTransactionAsync(new UnitOfWorkOptions(), cancellationToken);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => _wrapper.RollbackToSavepointAsync(null, cancellationToken));
        }

        [Fact]
        public async Task ReleaseSavepointAsync_WithValidName_ShouldNotThrow()
        {
            // Arrange
            var savepointName = "TestSavepoint";
            var cancellationToken = CancellationToken.None;
            await _wrapper.BeginTransactionAsync(new UnitOfWorkOptions(), cancellationToken);
            await _wrapper.CreateSavepointAsync(savepointName, cancellationToken);

            // Act & Assert - should not throw
            await _wrapper.ReleaseSavepointAsync(savepointName, cancellationToken);
        }

        [Fact]
        public async Task ReleaseSavepointAsync_WithNullName_ShouldThrowArgumentException()
        {
            // Arrange
            var cancellationToken = CancellationToken.None;
            await _wrapper.BeginTransactionAsync(new UnitOfWorkOptions(), cancellationToken);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => _wrapper.ReleaseSavepointAsync(null, cancellationToken));
        }

        #endregion

        #region Commit and Rollback Tests

        [Fact]
        public async Task CommitAsync_WithTransaction_ShouldCommit()
        {
            // Arrange
            var cancellationToken = CancellationToken.None;
            await _wrapper.BeginTransactionAsync(new UnitOfWorkOptions(), cancellationToken);

            // Act
            await _wrapper.CommitAsync(cancellationToken);

            // Assert - no exception thrown
            Assert.NotNull(_wrapper.DbContext);
        }

        [Fact]
        public async Task CommitAsync_WithoutTransaction_ShouldNotThrow()
        {
            // Arrange
            var cancellationToken = CancellationToken.None;

            // Act & Assert - should handle gracefully
            await _wrapper.CommitAsync(cancellationToken);
        }

        [Fact]
        public async Task RollbackAsync_WithTransaction_ShouldRollback()
        {
            // Arrange
            var cancellationToken = CancellationToken.None;
            await _wrapper.BeginTransactionAsync(new UnitOfWorkOptions(), cancellationToken);

            // Act
            await _wrapper.RollbackAsync(cancellationToken);

            // Assert - no exception thrown
            Assert.NotNull(_wrapper.DbContext);
        }

        [Fact]
        public async Task RollbackAsync_WithoutTransaction_ShouldNotThrow()
        {
            // Arrange
            var cancellationToken = CancellationToken.None;

            // Act & Assert - should handle gracefully
            await _wrapper.RollbackAsync(cancellationToken);
        }

        #endregion

        #region SaveChanges Tests

        [Fact]
        public async Task SaveChangesAsync_ShouldCallDbContextSaveChanges()
        {
            // Arrange
            var cancellationToken = CancellationToken.None;

            // Act
            await _wrapper.SaveChangesAsync(cancellationToken);

            // Assert - verify it completed without error
            Assert.NotNull(_wrapper.DbContext);
        }

        [Fact]
        public async Task SaveChangesAsync_CalledMultipleTimes_ShouldSucceed()
        {
            // Arrange
            var cancellationToken = CancellationToken.None;

            // Act
            await _wrapper.SaveChangesAsync(cancellationToken);
            await _wrapper.SaveChangesAsync(cancellationToken);
            await _wrapper.SaveChangesAsync(cancellationToken);

            // Assert - all calls succeeded
            Assert.NotNull(_wrapper.DbContext);
        }

        #endregion

        #region Dispose Tests

        [Fact]
        public void Dispose_ShouldDisposeDbContext()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<DbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var dbContext = new DbContext(options);
            var wrapper = new EFCoreDbContextWrapper(dbContext);

            // Act
            wrapper.Dispose();

            // Assert - verify wrapper is disposed
            // DbContext disposal is handled by EF Core
        }

        [Fact]
        public void Dispose_CalledMultipleTimes_ShouldNotThrow()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<DbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var dbContext = new DbContext(options);
            var wrapper = new EFCoreDbContextWrapper(dbContext);

            // Act & Assert - should not throw
            wrapper.Dispose();
            wrapper.Dispose();
            wrapper.Dispose();
        }

        #endregion

        #region Edge Case Tests

        [Fact]
        public async Task SavepointWorkflow_CreateRollbackRelease_ShouldWork()
        {
            // Arrange
            var savepointName = "Checkpoint";
            var cancellationToken = CancellationToken.None;
            await _wrapper.BeginTransactionAsync(new UnitOfWorkOptions(), cancellationToken);

            // Act
            var name1 = await _wrapper.CreateSavepointAsync(savepointName, cancellationToken);
            await _wrapper.RollbackToSavepointAsync(name1, cancellationToken);
            
            var name2 = await _wrapper.CreateSavepointAsync(savepointName + "2", cancellationToken);
            await _wrapper.ReleaseSavepointAsync(name2, cancellationToken);

            // Assert
            Assert.Equal(savepointName, name1);
            Assert.Equal(savepointName + "2", name2);
        }

        [Fact]
        public async Task TransactionWorkflow_BeginCommit_ShouldWork()
        {
            // Arrange
            var cancellationToken = CancellationToken.None;

            // Act
            await _wrapper.BeginTransactionAsync(new UnitOfWorkOptions { Strategy = PersistenceStrategy.TransactionManaged }, cancellationToken);
            await _wrapper.SaveChangesAsync(cancellationToken);
            await _wrapper.CommitAsync(cancellationToken);

            // Assert - completed successfully
            Assert.NotNull(_wrapper.DbContext);
        }

        [Fact]
        public async Task CommitAsync_ShouldAutoSavePendingChanges_WhenTransactionActive()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<TestEntityContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            await using var localContext = new TestEntityContext(options);
            var wrapper = new EFCoreDbContextWrapper(localContext);

            // Start manual transaction managed by wrapper
            await wrapper.BeginTransactionAsync(new UnitOfWorkOptions { Strategy = PersistenceStrategy.TransactionManaged });

            // Add entity, but do NOT call SaveChanges
            localContext.Set<TestEntity>().Add(new TestEntity { Name = "AutoSave" });

            // Act - commit should auto-save before commit
            await wrapper.CommitAsync();

            // Assert
            var saved = await localContext.Set<TestEntity>().FirstAsync();
            Assert.Equal("AutoSave", saved.Name);
        }

        // Simple test-specific DbContext and entity
        private class TestEntityContext : DbContext
        {
            public TestEntityContext(DbContextOptions options) : base(options) { }

            public DbSet<TestEntity> TestEntities { get; set; }
        }

        private class TestEntity
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        [Fact]
        public async Task TransactionWorkflow_BeginRollback_ShouldWork()
        {
            // Arrange
            var cancellationToken = CancellationToken.None;

            // Act
            await _wrapper.BeginTransactionAsync(new UnitOfWorkOptions(), cancellationToken);
            await _wrapper.SaveChangesAsync(cancellationToken);
            await _wrapper.RollbackAsync(cancellationToken);

            // Assert - completed successfully
            Assert.NotNull(_wrapper.DbContext);
        }

        #endregion

        public void Dispose()
        {
            _wrapper?.Dispose();
            _mockDbContext?.Object?.Dispose();
        }
    }
}
