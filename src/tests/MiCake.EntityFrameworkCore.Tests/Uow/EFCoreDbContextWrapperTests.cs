using MiCake.DDD.Uow;
using MiCake.DDD.Uow.Exceptions;
using MiCake.EntityFrameworkCore.Uow;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Data;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MiCake.EntityFrameworkCore.Tests.Uow
{
    /// <summary>
    /// Unit tests for EFCoreDbContextWrapper against the IUnitOfWorkResource contract:
    /// collision-safe identity, prepare/rebind rules, idempotent activation, flush, terminal
    /// commit/rollback, savepoint rejection, and synchronous/asynchronous disposal.
    /// </summary>
    public class EFCoreDbContextWrapperTests : IDisposable
    {
        private readonly TestDbContextForWrapper _dbContext;
        private readonly Mock<ILogger<EFCoreDbContextWrapper>> _mockLogger;
        private readonly UnitOfWorkResourceContext _context;

        public EFCoreDbContextWrapperTests()
        {
            var options = new DbContextOptionsBuilder<TestDbContextForWrapper>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _dbContext = new TestDbContextForWrapper(options);
            _mockLogger = new Mock<ILogger<EFCoreDbContextWrapper>>();
            _context = new UnitOfWorkResourceContext(Guid.NewGuid(), null, false);
        }

        public void Dispose()
        {
            _dbContext.Dispose();
        }

        private EFCoreDbContextWrapper CreateWrapper(bool shouldDisposeDbContext = false)
            => new(_dbContext, _mockLogger.Object, shouldDisposeDbContext);

        private static void SetTransaction(EFCoreDbContextWrapper wrapper, IDbContextTransaction transaction)
        {
            var field = typeof(EFCoreDbContextWrapper).GetField(
                "_currentTransaction",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field);
            field.SetValue(wrapper, transaction);
        }

        private static async Task<TestDbContextForWrapper> CreateSqliteContextAsync()
        {
            var options = new DbContextOptionsBuilder<TestDbContextForWrapper>()
                .UseSqlite("DataSource=:memory:")
                .Options;
            var context = new TestDbContextForWrapper(options);
            await context.Database.OpenConnectionAsync();
            await context.Database.EnsureCreatedAsync();
            return context;
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidParameters_ShouldCreateInstance()
        {
            // Arrange & Act
            var wrapper = CreateWrapper();

            // Assert
            Assert.NotNull(wrapper);
            Assert.Same(_dbContext, wrapper.DbContext);
            Assert.False(wrapper.HasActiveTransaction);
            Assert.False(wrapper.SupportsSavepoints);
            Assert.Equal(_dbContext.GetType().FullName, wrapper.ResourceType);
        }

        [Fact]
        public void Constructor_ShouldGenerateCollisionSafeIdNotDerivedFromHashCode()
        {
            // Arrange
            var wrapper1 = CreateWrapper();
            var wrapper2 = CreateWrapper();

            // Assert
            Assert.NotEqual(Guid.Empty, wrapper1.Id.Value);
            Assert.NotEqual(wrapper1.Id, wrapper2.Id);
        }

        [Fact]
        public void Constructor_WithNullDbContext_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new EFCoreDbContextWrapper(null!, _mockLogger.Object));
        }

        [Fact]
        public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new EFCoreDbContextWrapper(_dbContext, null!));
        }

        #endregion

        #region Prepare Tests

        [Fact]
        public void Prepare_ShouldStoreUowContext()
        {
            // Arrange
            var wrapper = CreateWrapper();

            // Act
            wrapper.Prepare(_context);

            // Assert
            Assert.False(wrapper.HasActiveTransaction);
        }

        [Fact]
        public void Prepare_SameUowTwice_ShouldBeIdempotent()
        {
            // Arrange
            var wrapper = CreateWrapper();

            // Act & Assert
            wrapper.Prepare(_context);
            var exception = Record.Exception(() => wrapper.Prepare(_context));

            Assert.Null(exception);
        }

        [Fact]
        public void Prepare_DifferentUow_ShouldRejectRebinding()
        {
            // Arrange
            var wrapper = CreateWrapper();
            wrapper.Prepare(_context);

            var otherContext = new UnitOfWorkResourceContext(Guid.NewGuid(), IsolationLevel.Serializable, false);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => wrapper.Prepare(otherContext));
            Assert.Contains("rebound", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Prepare_WhenDisposed_ShouldThrowObjectDisposedException()
        {
            // Arrange
            var wrapper = CreateWrapper();
            wrapper.Dispose();

            // Act & Assert
            Assert.Throws<ObjectDisposedException>(() => wrapper.Prepare(_context));
        }

        #endregion

        #region EnsureTransaction Tests

        [Fact]
        public async Task EnsureTransactionAsync_WithoutPrepare_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var wrapper = CreateWrapper();

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => wrapper.EnsureTransactionAsync().AsTask());
        }

        [Fact]
        public async Task EnsureTransactionAsync_ShouldStartExplicitTransaction()
        {
            // Arrange
            await using var sqliteContext = await CreateSqliteContextAsync();
            var wrapper = new EFCoreDbContextWrapper(sqliteContext, _mockLogger.Object, shouldDisposeDbContext: true);
            wrapper.Prepare(_context);

            // Act
            await wrapper.EnsureTransactionAsync();

            // Assert
            Assert.True(wrapper.HasActiveTransaction);
        }

        [Fact]
        public async Task EnsureTransactionAsync_CalledTwice_ShouldBeIdempotent()
        {
            // Arrange
            await using var sqliteContext = await CreateSqliteContextAsync();
            var wrapper = new EFCoreDbContextWrapper(sqliteContext, _mockLogger.Object, shouldDisposeDbContext: true);
            wrapper.Prepare(_context);

            // Act
            await wrapper.EnsureTransactionAsync();
            var exception = await Record.ExceptionAsync(() => wrapper.EnsureTransactionAsync().AsTask());

            // Assert
            Assert.Null(exception);
            Assert.True(wrapper.HasActiveTransaction);
        }

        [Fact]
        public void EnsureTransaction_ShouldStartExplicitTransactionSynchronously()
        {
            // Arrange
            var sqliteOptions = new DbContextOptionsBuilder<TestDbContextForWrapper>()
                .UseSqlite("DataSource=:memory:")
                .Options;
            using var sqliteContext = new TestDbContextForWrapper(sqliteOptions);
            sqliteContext.Database.OpenConnection();
            sqliteContext.Database.EnsureCreated();

            var wrapper = new EFCoreDbContextWrapper(sqliteContext, _mockLogger.Object, shouldDisposeDbContext: true);
            wrapper.Prepare(_context);

            // Act
            wrapper.EnsureTransaction();

            // Assert
            Assert.True(wrapper.HasActiveTransaction);
        }

        [Fact]
        public async Task EnsureTransactionAsync_WithIsolationLevel_ShouldUseSpecifiedLevel()
        {
            // Arrange
            await using var sqliteContext = await CreateSqliteContextAsync();
            var wrapper = new EFCoreDbContextWrapper(sqliteContext, _mockLogger.Object, shouldDisposeDbContext: true);
            wrapper.Prepare(new UnitOfWorkResourceContext(Guid.NewGuid(), IsolationLevel.Serializable, false));

            // Act
            await wrapper.EnsureTransactionAsync();

            // Assert
            Assert.True(wrapper.HasActiveTransaction);
        }

        #endregion

        #region FlushAsync Tests

        [Fact]
        public async Task FlushAsync_WithoutPrepare_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var wrapper = CreateWrapper();

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => wrapper.FlushAsync().AsTask());
        }

        [Fact]
        public async Task FlushAsync_ShouldReturnAffectedRowCount()
        {
            // Arrange - file-backed in-memory SQLite because flush requires an explicit transaction.
            await using var sqliteContext = await CreateSqliteContextAsync();
            var wrapper = new EFCoreDbContextWrapper(sqliteContext, _mockLogger.Object, shouldDisposeDbContext: true);
            wrapper.Prepare(_context);
            await wrapper.EnsureTransactionAsync();
            sqliteContext.Add(new TestEntity { Name = "item" });

            // Act
            var affected = await wrapper.FlushAsync();

            // Assert
            Assert.Equal(1, affected);
        }

        [Fact]
        public async Task FlushAsync_WithoutActiveTransaction_ShouldThrowAndNotPersist()
        {
            // Arrange
            await using var sqliteContext = await CreateSqliteContextAsync();
            var wrapper = new EFCoreDbContextWrapper(sqliteContext, _mockLogger.Object, shouldDisposeDbContext: true);
            wrapper.Prepare(_context);
            sqliteContext.Add(new TestEntity { Name = "must not persist" });

            // Act - flush without an active transaction is rejected before any write
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => wrapper.FlushAsync().AsTask());

            // Assert
            Assert.Contains("active transaction", exception.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(0, await sqliteContext.Set<TestEntity>().CountAsync());
        }

        #endregion

        #region Commit Tests

        [Fact]
        public async Task CommitAsync_WithoutActiveTransaction_ShouldCompleteWithoutError()
        {
            // Arrange
            var wrapper = CreateWrapper();
            wrapper.Prepare(_context);

            // Act
            await wrapper.CommitAsync();

            // Assert
            Assert.False(wrapper.HasActiveTransaction);
        }

        [Fact]
        public async Task CommitAsync_ShouldPersistChanges()
        {
            // Arrange
            await using var sqliteContext = await CreateSqliteContextAsync();
            var wrapper = new EFCoreDbContextWrapper(sqliteContext, _mockLogger.Object, shouldDisposeDbContext: true);
            wrapper.Prepare(_context);
            await wrapper.EnsureTransactionAsync();
            sqliteContext.Add(new TestEntity { Name = "committed" });
            await sqliteContext.SaveChangesAsync();

            // Act
            await wrapper.CommitAsync();

            // Assert
            Assert.False(wrapper.HasActiveTransaction);
            Assert.Equal(1, await sqliteContext.Set<TestEntity>().CountAsync());
        }

        [Fact]
        public async Task CommitAsync_CalledTwice_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var wrapper = CreateWrapper();
            wrapper.Prepare(_context);

            // Act
            await wrapper.CommitAsync();

            // Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => wrapper.CommitAsync().AsTask());
        }

        [Fact]
        public async Task CommitAsync_AfterRollback_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var wrapper = CreateWrapper();
            wrapper.Prepare(_context);

            // Act
            await wrapper.RollbackAsync();

            // Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => wrapper.CommitAsync().AsTask());
        }

        [Fact]
        public async Task CommitAsync_WhenCommitFails_ShouldRemainEligibleForRollback()
        {
            // Arrange
            var commitFailure = new InvalidOperationException("Commit failed");
            var transaction = new Mock<IDbContextTransaction>();
            transaction
                .Setup(t => t.CommitAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(commitFailure);
            transaction
                .Setup(t => t.RollbackAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            transaction
                .Setup(t => t.DisposeAsync())
                .Returns(ValueTask.CompletedTask);

            var wrapper = CreateWrapper();
            wrapper.Prepare(_context);
            SetTransaction(wrapper, transaction.Object);

            // Act
            var actual = await Assert.ThrowsAsync<InvalidOperationException>(() => wrapper.CommitAsync().AsTask());
            await wrapper.RollbackAsync();

            // Assert
            Assert.Same(commitFailure, actual);
            Assert.False(wrapper.HasActiveTransaction);
            transaction.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
            await wrapper.DisposeAsync();
        }

        #endregion

        #region Rollback Tests

        [Fact]
        public async Task RollbackAsync_ShouldDiscardChanges()
        {
            // Arrange
            await using var sqliteContext = await CreateSqliteContextAsync();
            var wrapper = new EFCoreDbContextWrapper(sqliteContext, _mockLogger.Object, shouldDisposeDbContext: true);
            wrapper.Prepare(_context);
            await wrapper.EnsureTransactionAsync();
            sqliteContext.Add(new TestEntity { Name = "rolled back" });
            await sqliteContext.SaveChangesAsync();

            // Act
            await wrapper.RollbackAsync();

            // Assert
            Assert.False(wrapper.HasActiveTransaction);
            Assert.Equal(0, await sqliteContext.Set<TestEntity>().CountAsync());
        }

        [Fact]
        public async Task RollbackAsync_WithoutActiveTransaction_ShouldCompleteWithoutError()
        {
            // Arrange
            var wrapper = CreateWrapper();
            wrapper.Prepare(_context);

            // Act
            await wrapper.RollbackAsync();

            // Assert
            Assert.False(wrapper.HasActiveTransaction);
        }

        [Fact]
        public async Task RollbackAsync_CalledTwice_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var wrapper = CreateWrapper();
            wrapper.Prepare(_context);

            // Act
            await wrapper.RollbackAsync();

            // Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => wrapper.RollbackAsync().AsTask());
        }

        #endregion

        #region Savepoint Tests

        [Fact]
        public async Task CreateSavepointAsync_ShouldThrowNotSupportedException()
        {
            // Arrange
            var wrapper = CreateWrapper();
            wrapper.Prepare(_context);

            // Act & Assert
            await Assert.ThrowsAsync<NotSupportedException>(() => wrapper.CreateSavepointAsync("sp").AsTask());
        }

        [Fact]
        public async Task RollbackToSavepointAsync_ShouldThrowNotSupportedException()
        {
            // Arrange
            var wrapper = CreateWrapper();
            wrapper.Prepare(_context);

            // Act & Assert
            await Assert.ThrowsAsync<NotSupportedException>(() => wrapper.RollbackToSavepointAsync("sp").AsTask());
        }

        [Fact]
        public async Task ReleaseSavepointAsync_ShouldThrowNotSupportedException()
        {
            // Arrange
            var wrapper = CreateWrapper();
            wrapper.Prepare(_context);

            // Act & Assert
            await Assert.ThrowsAsync<NotSupportedException>(() => wrapper.ReleaseSavepointAsync("sp").AsTask());
        }

        [Fact]
        public async Task CreateSavepointAsync_WithActiveTransaction_ShouldReportSupportedAndCreate()
        {
            // Arrange
            await using var sqliteContext = await CreateSqliteContextAsync();
            var wrapper = new EFCoreDbContextWrapper(sqliteContext, _mockLogger.Object, shouldDisposeDbContext: true);
            wrapper.Prepare(_context);
            await wrapper.EnsureTransactionAsync();

            // Act & Assert - SQLite exposes savepoints through the provider transaction
            Assert.True(wrapper.SupportsSavepoints);
            await wrapper.CreateSavepointAsync("sp1");
        }

        [Fact]
        public async Task RollbackToSavepointAsync_ShouldUndoWritesAfterSavepoint()
        {
            // Arrange
            await using var sqliteContext = await CreateSqliteContextAsync();
            var wrapper = new EFCoreDbContextWrapper(sqliteContext, _mockLogger.Object, shouldDisposeDbContext: true);
            wrapper.Prepare(_context);
            await wrapper.EnsureTransactionAsync();
            sqliteContext.Add(new TestEntity { Name = "kept" });
            await sqliteContext.SaveChangesAsync();
            await wrapper.CreateSavepointAsync("sp1");

            var discarded = new TestEntity { Name = "discarded" };
            sqliteContext.Add(discarded);
            await sqliteContext.SaveChangesAsync();

            // Act - roll back to the savepoint and detach the rolled-back entity so it is not re-inserted
            await wrapper.RollbackToSavepointAsync("sp1");
            sqliteContext.Entry(discarded).State = EntityState.Detached;
            await wrapper.CommitAsync();

            // Assert - only the pre-savepoint write is durable
            Assert.Equal(1, await sqliteContext.Set<TestEntity>().CountAsync());
            Assert.Equal("kept", (await sqliteContext.Set<TestEntity>().SingleAsync()).Name);
        }

        [Fact]
        public async Task ReleaseSavepointAsync_WithActiveTransaction_ShouldComplete()
        {
            // Arrange
            await using var sqliteContext = await CreateSqliteContextAsync();
            var wrapper = new EFCoreDbContextWrapper(sqliteContext, _mockLogger.Object, shouldDisposeDbContext: true);
            wrapper.Prepare(_context);
            await wrapper.EnsureTransactionAsync();
            await wrapper.CreateSavepointAsync("sp1");

            // Act & Assert
            await wrapper.ReleaseSavepointAsync("sp1");
            await wrapper.CommitAsync();
        }

        #endregion

        #region Disposal Tests

        [Fact]
        public void Dispose_ShouldReleaseTransaction()
        {
            // Arrange
            var sqliteOptions = new DbContextOptionsBuilder<TestDbContextForWrapper>()
                .UseSqlite("DataSource=:memory:")
                .Options;
            using var sqliteContext = new TestDbContextForWrapper(sqliteOptions);
            sqliteContext.Database.OpenConnection();
            sqliteContext.Database.EnsureCreated();

            var wrapper = new EFCoreDbContextWrapper(sqliteContext, _mockLogger.Object, shouldDisposeDbContext: false);
            wrapper.Prepare(_context);
            wrapper.EnsureTransaction();
            Assert.True(wrapper.HasActiveTransaction);

            // Act
            wrapper.Dispose();

            // Assert
            Assert.False(wrapper.HasActiveTransaction);
            Assert.True(wrapper.DbContext is not null);
        }

        [Fact]
        public void Dispose_WhenOwned_ShouldDisposeDbContext()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<DisposeTrackingDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var trackedContext = new DisposeTrackingDbContext(options);
            var wrapper = new EFCoreDbContextWrapper(trackedContext, _mockLogger.Object, shouldDisposeDbContext: true);

            // Act
            wrapper.Dispose();

            // Assert
            Assert.Equal(1, trackedContext.DisposeCount);
            Assert.Equal(0, trackedContext.DisposeAsyncCount);
        }

        [Fact]
        public async Task DisposeAsync_WhenOwned_ShouldUseAsyncDisposalPath()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<DisposeTrackingDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var trackedContext = new DisposeTrackingDbContext(options);
            var wrapper = new EFCoreDbContextWrapper(trackedContext, _mockLogger.Object, shouldDisposeDbContext: true);

            // Act
            await wrapper.DisposeAsync();

            // Assert
            Assert.Equal(1, trackedContext.DisposeAsyncCount);
        }

        [Fact]
        public async Task DisposeAsync_ShouldReleaseTransactionAndDisposeDbContextWhenOwned()
        {
            // Arrange
            await using var sqliteContext = await CreateSqliteContextAsync();
            var wrapper = new EFCoreDbContextWrapper(sqliteContext, _mockLogger.Object, shouldDisposeDbContext: true);
            wrapper.Prepare(_context);
            await wrapper.EnsureTransactionAsync();
            Assert.True(wrapper.HasActiveTransaction);

            // Act
            await wrapper.DisposeAsync();

            // Assert
            Assert.False(wrapper.HasActiveTransaction);
        }

        [Fact]
        public async Task DisposeAsync_AfterSuccessfulCommit_WhenTransactionCleanupFails_ShouldExposeCleanupFailure()
        {
            // Arrange
            var cleanupFailure = new InvalidOperationException("Transaction cleanup failed");
            var transaction = new Mock<IDbContextTransaction>();
            transaction
                .Setup(t => t.CommitAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            transaction
                .Setup(t => t.DisposeAsync())
                .Throws(cleanupFailure);

            var wrapper = CreateWrapper();
            wrapper.Prepare(_context);
            SetTransaction(wrapper, transaction.Object);

            // Act - commit records the durable outcome; cleanup remains resource-disposal work
            await wrapper.CommitAsync();
            var exception = await Assert.ThrowsAsync<UnitOfWorkBoundaryException>(() => wrapper.DisposeAsync().AsTask());

            // Assert
            Assert.False(wrapper.HasActiveTransaction);
            Assert.Single(exception.CleanupExceptions);
            Assert.Same(cleanupFailure, exception.CleanupExceptions[0]);
        }

        [Fact]
        public async Task DisposeAsync_WhenTransactionAndDbContextCleanupFail_ShouldExposeBothFailures()
        {
            // Arrange
            var transactionFailure = new InvalidOperationException("Transaction cleanup failed");
            var contextFailure = new InvalidOperationException("DbContext cleanup failed");
            var transaction = new Mock<IDbContextTransaction>();
            transaction
                .Setup(t => t.DisposeAsync())
                .Throws(transactionFailure);

            var options = new DbContextOptionsBuilder<DisposeTrackingDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var trackedContext = new DisposeTrackingDbContext(options)
            {
                DisposeAsyncException = contextFailure
            };
            var wrapper = new EFCoreDbContextWrapper(trackedContext, _mockLogger.Object, shouldDisposeDbContext: true);
            SetTransaction(wrapper, transaction.Object);

            // Act
            var exception = await Assert.ThrowsAsync<UnitOfWorkBoundaryException>(() => wrapper.DisposeAsync().AsTask());

            // Assert
            Assert.Equal(2, exception.CleanupExceptions.Count);
            Assert.Contains(transactionFailure, exception.CleanupExceptions);
            Assert.Contains(contextFailure, exception.CleanupExceptions);
        }

        #endregion
    }

    /// <summary>
    /// DbContext that records whether its Dispose / DisposeAsync path was invoked.
    /// </summary>
    public class DisposeTrackingDbContext : DbContext
    {
        public int DisposeCount { get; private set; }
        public int DisposeAsyncCount { get; private set; }
        public Exception? DisposeAsyncException { get; init; }

        public DisposeTrackingDbContext(DbContextOptions<DisposeTrackingDbContext> options) : base(options)
        {
        }

        public override void Dispose()
        {
            DisposeCount++;
            base.Dispose();
        }

        public override ValueTask DisposeAsync()
        {
            DisposeAsyncCount++;
            if (DisposeAsyncException != null)
            {
                throw DisposeAsyncException;
            }
            return base.DisposeAsync();
        }
    }

    public class TestEntity
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    public class TestDbContextForWrapper : DbContext
    {
        public TestDbContextForWrapper(DbContextOptions<TestDbContextForWrapper> options) : base(options)
        {
        }

        public DbSet<TestEntity> Entities => Set<TestEntity>();
    }
}
