using MiCake.DDD.Uow;
using MiCake.EntityFrameworkCore.Uow;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Data;
using System.Threading.Tasks;
using Xunit;

namespace MiCake.EntityFrameworkCore.Tests.Uow
{
    /// <summary>
    /// Unit tests for EFCoreDbContextWrapper.
    /// Tests the two-phase transaction pattern, commit, rollback, and savepoint operations.
    /// </summary>
    public class EFCoreDbContextWrapperTests : IDisposable
    {
        private readonly TestDbContextForWrapper _dbContext;
        private readonly Mock<ILogger<EFCoreDbContextWrapper>> _mockLogger;
        private readonly MiCakeEFCoreOptions _efCoreOptions;

        public EFCoreDbContextWrapperTests()
        {
            var options = new DbContextOptionsBuilder<TestDbContextForWrapper>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _dbContext = new TestDbContextForWrapper(options);
            _mockLogger = new Mock<ILogger<EFCoreDbContextWrapper>>();
            _efCoreOptions = new MiCakeEFCoreOptions(typeof(TestDbContextForWrapper));
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidParameters_ShouldCreateInstance()
        {
            // Arrange & Act
            var wrapper = new EFCoreDbContextWrapper(_dbContext, _mockLogger.Object, _efCoreOptions);

            // Assert
            Assert.NotNull(wrapper);
            Assert.Same(_dbContext, wrapper.DbContext);
            Assert.False(wrapper.HasActiveTransaction);
            Assert.False(wrapper.IsInitialized);
        }

        [Fact]
        public void Constructor_WithNullDbContext_ShouldThrowArgumentNullException()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new EFCoreDbContextWrapper(null!, _mockLogger.Object, _efCoreOptions));
        }

        [Fact]
        public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new EFCoreDbContextWrapper(_dbContext, null!, _efCoreOptions));
        }

        [Fact]
        public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new EFCoreDbContextWrapper(_dbContext, _mockLogger.Object, null!));
        }

        [Fact]
        public void ResourceIdentifier_ShouldContainDbContextTypeAndHashCode()
        {
            // Arrange
            var wrapper = new EFCoreDbContextWrapper(_dbContext, _mockLogger.Object, _efCoreOptions);

            // Act
            var identifier = wrapper.ResourceIdentifier;

            // Assert
            Assert.Contains(_dbContext.GetType().FullName!, identifier);
            Assert.Contains(_dbContext.GetHashCode().ToString(), identifier);
        }

        #endregion

        #region PrepareForTransaction Tests

        [Fact]
        public void PrepareForTransaction_WithValidOptions_ShouldSetIsPrepared()
        {
            // Arrange
            var wrapper = new EFCoreDbContextWrapper(_dbContext, _mockLogger.Object, _efCoreOptions);
            var options = UnitOfWorkOptions.Default;

            // Act
            wrapper.PrepareForTransaction(options);

            // Assert
            Assert.False(wrapper.IsInitialized); // Not initialized until ActivateTransactionAsync
            Assert.False(wrapper.HasActiveTransaction);
        }

        [Fact]
        public void PrepareForTransaction_WithNullOptions_ShouldThrowArgumentNullException()
        {
            // Arrange
            var wrapper = new EFCoreDbContextWrapper(_dbContext, _mockLogger.Object, _efCoreOptions);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => wrapper.PrepareForTransaction(null!));
        }

        [Fact]
        public void PrepareForTransaction_CalledTwice_ShouldNotThrow()
        {
            // Arrange
            var wrapper = new EFCoreDbContextWrapper(_dbContext, _mockLogger.Object, _efCoreOptions);
            var options = UnitOfWorkOptions.Default;

            // Act - Call twice
            wrapper.PrepareForTransaction(options);
            var exception = Record.Exception(() => wrapper.PrepareForTransaction(options));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void PrepareForTransaction_WhenDisposed_ShouldThrowObjectDisposedException()
        {
            // Arrange
            var wrapper = new EFCoreDbContextWrapper(_dbContext, _mockLogger.Object, _efCoreOptions);
            wrapper.Dispose();

            // Act & Assert
            Assert.Throws<ObjectDisposedException>(() => wrapper.PrepareForTransaction(UnitOfWorkOptions.Default));
        }

        #endregion

        #region ActivateTransactionAsync Tests

        [Fact]
        public async Task ActivateTransactionAsync_WithoutPrepare_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var wrapper = new EFCoreDbContextWrapper(_dbContext, _mockLogger.Object, _efCoreOptions);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => wrapper.ActivateTransactionAsync());
        }

        [Fact]
        public async Task ActivateTransactionAsync_WithOptimizeForSingleWrite_ShouldNotStartExplicitTransaction()
        {
            // Arrange
            var wrapper = new EFCoreDbContextWrapper(_dbContext, _mockLogger.Object, _efCoreOptions);
            var options = new UnitOfWorkOptions { Strategy = PersistenceStrategy.OptimizeForSingleWrite };
            wrapper.PrepareForTransaction(options);

            // Act
            await wrapper.ActivateTransactionAsync();

            // Assert
            Assert.True(wrapper.IsInitialized);
            Assert.False(wrapper.HasActiveTransaction); // No explicit transaction for OptimizeForSingleWrite
        }

        [Fact]
        public async Task ActivateTransactionAsync_WithTransactionManaged_ShouldStartExplicitTransaction()
        {
            // Arrange
            // Use SQLite in-memory for real transaction support
            var sqliteOptions = new DbContextOptionsBuilder<TestDbContextForWrapper>()
                .UseSqlite("DataSource=:memory:")
                .Options;
            using var sqliteContext = new TestDbContextForWrapper(sqliteOptions);
            await sqliteContext.Database.OpenConnectionAsync();
            await sqliteContext.Database.EnsureCreatedAsync();

            var wrapper = new EFCoreDbContextWrapper(sqliteContext, _mockLogger.Object, _efCoreOptions, shouldDisposeDbContext: true);
            var options = new UnitOfWorkOptions { Strategy = PersistenceStrategy.TransactionManaged };
            wrapper.PrepareForTransaction(options);

            // Act
            await wrapper.ActivateTransactionAsync();

            // Assert
            Assert.True(wrapper.IsInitialized);
            Assert.True(wrapper.HasActiveTransaction);

            // Cleanup
            wrapper.Dispose();
        }

        [Fact]
        public async Task ActivateTransactionAsync_CalledTwice_ShouldBeIdempotent()
        {
            // Arrange
            var wrapper = new EFCoreDbContextWrapper(_dbContext, _mockLogger.Object, _efCoreOptions);
            var options = UnitOfWorkOptions.Default;
            wrapper.PrepareForTransaction(options);
            await wrapper.ActivateTransactionAsync();

            // Act - Call again
            var exception = await Record.ExceptionAsync(() => wrapper.ActivateTransactionAsync());

            // Assert
            Assert.Null(exception);
            Assert.True(wrapper.IsInitialized);
        }

        [Fact]
        public async Task ActivateTransactionAsync_WhenDisposed_ShouldThrowObjectDisposedException()
        {
            // Arrange
            var wrapper = new EFCoreDbContextWrapper(_dbContext, _mockLogger.Object, _efCoreOptions);
            wrapper.PrepareForTransaction(UnitOfWorkOptions.Default);
            wrapper.Dispose();

            // Act & Assert
            await Assert.ThrowsAsync<ObjectDisposedException>(() => wrapper.ActivateTransactionAsync());
        }

        [Fact]
        public async Task ActivateTransactionAsync_WithIsolationLevel_ShouldUseSpecifiedLevel()
        {
            // Arrange
            var sqliteOptions = new DbContextOptionsBuilder<TestDbContextForWrapper>()
                .UseSqlite("DataSource=:memory:")
                .Options;
            using var sqliteContext = new TestDbContextForWrapper(sqliteOptions);
            await sqliteContext.Database.OpenConnectionAsync();
            await sqliteContext.Database.EnsureCreatedAsync();

            var wrapper = new EFCoreDbContextWrapper(sqliteContext, _mockLogger.Object, _efCoreOptions, shouldDisposeDbContext: true);
            var options = new UnitOfWorkOptions
            {
                Strategy = PersistenceStrategy.TransactionManaged,
                IsolationLevel = IsolationLevel.Serializable
            };
            wrapper.PrepareForTransaction(options);

            // Act
            await wrapper.ActivateTransactionAsync();

            // Assert
            Assert.True(wrapper.HasActiveTransaction);

            // Cleanup
            wrapper.Dispose();
        }

        #endregion

        #region BeginTransactionAsync Tests

        [Fact]
        public async Task BeginTransactionAsync_ShouldCombinePrepareAndActivate()
        {
            // Arrange
            var wrapper = new EFCoreDbContextWrapper(_dbContext, _mockLogger.Object, _efCoreOptions);
            var options = UnitOfWorkOptions.Default;

            // Act
            await wrapper.BeginTransactionAsync(options);

            // Assert
            Assert.True(wrapper.IsInitialized);
        }

        [Fact]
        public async Task BeginTransactionAsync_WithNullOptions_ShouldThrowArgumentNullException()
        {
            // Arrange
            var wrapper = new EFCoreDbContextWrapper(_dbContext, _mockLogger.Object, _efCoreOptions);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => wrapper.BeginTransactionAsync(null!));
        }

        #endregion

        #region CommitAsync Tests

        [Fact]
        public async Task CommitAsync_WithActiveTransaction_ShouldCommitSuccessfully()
        {
            // Arrange
            var sqliteOptions = new DbContextOptionsBuilder<TestDbContextForWrapper>()
                .UseSqlite("DataSource=:memory:")
                .Options;
            using var sqliteContext = new TestDbContextForWrapper(sqliteOptions);
            await sqliteContext.Database.OpenConnectionAsync();
            await sqliteContext.Database.EnsureCreatedAsync();

            var wrapper = new EFCoreDbContextWrapper(sqliteContext, _mockLogger.Object, _efCoreOptions, shouldDisposeDbContext: true);
            await wrapper.BeginTransactionAsync(new UnitOfWorkOptions { Strategy = PersistenceStrategy.TransactionManaged });

            // Act
            await wrapper.CommitAsync();

            // Assert
            Assert.False(wrapper.HasActiveTransaction); // Transaction should be disposed after commit
        }

        [Fact]
        public async Task CommitAsync_WithoutActiveTransaction_ShouldNotThrow()
        {
            // Arrange
            var wrapper = new EFCoreDbContextWrapper(_dbContext, _mockLogger.Object, _efCoreOptions);
            await wrapper.BeginTransactionAsync(UnitOfWorkOptions.Default); // OptimizeForSingleWrite - no explicit transaction

            // Act
            var exception = await Record.ExceptionAsync(() => wrapper.CommitAsync());

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public async Task CommitAsync_WhenDisposed_ShouldThrowObjectDisposedException()
        {
            // Arrange
            var wrapper = new EFCoreDbContextWrapper(_dbContext, _mockLogger.Object, _efCoreOptions);
            wrapper.Dispose();

            // Act & Assert
            await Assert.ThrowsAsync<ObjectDisposedException>(() => wrapper.CommitAsync());
        }

        #endregion

        #region RollbackAsync Tests

        [Fact]
        public async Task RollbackAsync_WithActiveTransaction_ShouldRollbackSuccessfully()
        {
            // Arrange
            var sqliteOptions = new DbContextOptionsBuilder<TestDbContextForWrapper>()
                .UseSqlite("DataSource=:memory:")
                .Options;
            using var sqliteContext = new TestDbContextForWrapper(sqliteOptions);
            await sqliteContext.Database.OpenConnectionAsync();
            await sqliteContext.Database.EnsureCreatedAsync();

            var wrapper = new EFCoreDbContextWrapper(sqliteContext, _mockLogger.Object, _efCoreOptions, shouldDisposeDbContext: true);
            await wrapper.BeginTransactionAsync(new UnitOfWorkOptions { Strategy = PersistenceStrategy.TransactionManaged });

            // Act
            await wrapper.RollbackAsync();

            // Assert
            Assert.False(wrapper.HasActiveTransaction);
        }

        [Fact]
        public async Task RollbackAsync_WithoutActiveTransaction_ShouldNotThrow()
        {
            // Arrange
            var wrapper = new EFCoreDbContextWrapper(_dbContext, _mockLogger.Object, _efCoreOptions);
            await wrapper.BeginTransactionAsync(UnitOfWorkOptions.Default);

            // Act
            var exception = await Record.ExceptionAsync(() => wrapper.RollbackAsync());

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public async Task RollbackAsync_WhenDisposed_ShouldNotThrow()
        {
            // Arrange
            var wrapper = new EFCoreDbContextWrapper(_dbContext, _mockLogger.Object, _efCoreOptions);
            wrapper.Dispose();

            // Act - Rollback should be safe even when disposed
            var exception = await Record.ExceptionAsync(() => wrapper.RollbackAsync());

            // Assert
            Assert.Null(exception);
        }

        #endregion

        #region SaveChangesAsync Tests

        [Fact]
        public async Task SaveChangesAsync_ShouldCallDbContextSaveChanges()
        {
            // Arrange
            var wrapper = new EFCoreDbContextWrapper(_dbContext, _mockLogger.Object, _efCoreOptions);
            _dbContext.TestEntities.Add(new TestEntityForWrapper { Name = "Test" });

            // Act
            await wrapper.SaveChangesAsync();

            // Assert
            var count = await _dbContext.TestEntities.CountAsync();
            Assert.Equal(1, count);
        }

        [Fact]
        public async Task SaveChangesAsync_WhenDisposed_ShouldThrowObjectDisposedException()
        {
            // Arrange
            var wrapper = new EFCoreDbContextWrapper(_dbContext, _mockLogger.Object, _efCoreOptions);
            wrapper.Dispose();

            // Act & Assert
            await Assert.ThrowsAsync<ObjectDisposedException>(() => wrapper.SaveChangesAsync());
        }

        #endregion

        #region Savepoint Tests

        [Fact]
        public async Task CreateSavepointAsync_WithoutActiveTransaction_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var wrapper = new EFCoreDbContextWrapper(_dbContext, _mockLogger.Object, _efCoreOptions);
            await wrapper.BeginTransactionAsync(UnitOfWorkOptions.Default); // No explicit transaction

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => wrapper.CreateSavepointAsync("sp1"));
        }

        [Fact]
        public async Task RollbackToSavepointAsync_WithoutActiveTransaction_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var wrapper = new EFCoreDbContextWrapper(_dbContext, _mockLogger.Object, _efCoreOptions);
            await wrapper.BeginTransactionAsync(UnitOfWorkOptions.Default);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => wrapper.RollbackToSavepointAsync("sp1"));
        }

        [Fact]
        public async Task ReleaseSavepointAsync_WithoutActiveTransaction_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var wrapper = new EFCoreDbContextWrapper(_dbContext, _mockLogger.Object, _efCoreOptions);
            await wrapper.BeginTransactionAsync(UnitOfWorkOptions.Default);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => wrapper.ReleaseSavepointAsync("sp1"));
        }

        [Fact]
        public async Task CreateSavepointAsync_WhenDisposed_ShouldThrowObjectDisposedException()
        {
            // Arrange
            var wrapper = new EFCoreDbContextWrapper(_dbContext, _mockLogger.Object, _efCoreOptions);
            wrapper.Dispose();

            // Act & Assert
            await Assert.ThrowsAsync<ObjectDisposedException>(() => wrapper.CreateSavepointAsync("sp1"));
        }

        #endregion

        #region Dispose Tests

        [Fact]
        public void Dispose_ShouldSetDisposedFlag()
        {
            // Arrange
            var wrapper = new EFCoreDbContextWrapper(_dbContext, _mockLogger.Object, _efCoreOptions);

            // Act
            wrapper.Dispose();

            // Assert - Verify disposed by checking subsequent operations throw
            Assert.Throws<ObjectDisposedException>(() => wrapper.PrepareForTransaction(UnitOfWorkOptions.Default));
        }

        [Fact]
        public void Dispose_CalledMultipleTimes_ShouldNotThrow()
        {
            // Arrange
            var wrapper = new EFCoreDbContextWrapper(_dbContext, _mockLogger.Object, _efCoreOptions);

            // Act
            wrapper.Dispose();
            var exception = Record.Exception(() => wrapper.Dispose());

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void Dispose_WithShouldDisposeDbContext_ShouldDisposeDbContext()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<TestDbContextForWrapper>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var dbContext = new TestDbContextForWrapper(options);
            var wrapper = new EFCoreDbContextWrapper(dbContext, _mockLogger.Object, _efCoreOptions, shouldDisposeDbContext: true);

            // Act
            wrapper.Dispose();

            // Assert - DbContext should be disposed
            Assert.Throws<ObjectDisposedException>(() => dbContext.TestEntities.Add(new TestEntityForWrapper()));
        }

        [Fact]
        public void Dispose_WithoutShouldDisposeDbContext_ShouldNotDisposeDbContext()
        {
            // Arrange
            var wrapper = new EFCoreDbContextWrapper(_dbContext, _mockLogger.Object, _efCoreOptions, shouldDisposeDbContext: false);

            // Act
            wrapper.Dispose();

            // Assert - DbContext should still be usable
            var exception = Record.Exception(() => _dbContext.TestEntities.Add(new TestEntityForWrapper()));
            Assert.Null(exception);
        }

        #endregion

        #region User-Managed Transaction Tests

        [Fact]
        public async Task Constructor_WithExistingTransaction_ShouldDetectUserManagedTransaction()
        {
            // Arrange
            var sqliteOptions = new DbContextOptionsBuilder<TestDbContextForWrapper>()
                .UseSqlite("DataSource=:memory:")
                .Options;
            using var sqliteContext = new TestDbContextForWrapper(sqliteOptions);
            await sqliteContext.Database.OpenConnectionAsync();
            await sqliteContext.Database.EnsureCreatedAsync();

            // Start user-managed transaction
            await sqliteContext.Database.BeginTransactionAsync();

            // Act
            var wrapper = new EFCoreDbContextWrapper(sqliteContext, _mockLogger.Object, _efCoreOptions);
            wrapper.PrepareForTransaction(UnitOfWorkOptions.Default);

            // Assert
            Assert.True(wrapper.HasActiveTransaction);
            Assert.True(wrapper.IsInitialized); // User-managed counts as initialized
        }

        #endregion

        #region Helper Classes

        public class TestDbContextForWrapper : DbContext
        {
            public TestDbContextForWrapper(DbContextOptions options) : base(options) { }

            public DbSet<TestEntityForWrapper> TestEntities { get; set; } = null!;

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                base.OnModelCreating(modelBuilder);
                modelBuilder.Entity<TestEntityForWrapper>().HasKey(e => e.Id);
            }
        }

        public class TestEntityForWrapper
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }

        #endregion

        public void Dispose()
        {
            _dbContext?.Dispose();
        }
    }
}
