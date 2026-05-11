using MiCake.EntityFrameworkCore;
using MiCake.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using MiCake.Core;
using MiCake.Core.Modularity;
using System;
using MiCake.IntegrationTests.Fixtures;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using MiCake.DDD.Domain;

namespace MiCake.IntegrationTests.Uow
{
    /// <summary>
    /// Integration tests for owned entity (value object via OwnsOne/OwnsMany) audit functionality.
    /// Tests that parent entity's UpdatedAt is set when its owned entity changes.
    /// </summary>
    [Collection("MiCakeIntegrationTests")]
    public class OwnedEntityAuditIntegrationTests : IDisposable
    {
        private readonly ServiceProvider _serviceProvider;
        private readonly TestDbContext _dbContext;
        private readonly MiCakeAppFixture _fixture;
        private readonly DateTimeOffset _fixedTimeOffset = new(2025, 1, 21, 10, 30, 0, TimeSpan.Zero);
        private readonly DateTime _fixedTime = new(2025, 1, 21, 10, 30, 0, DateTimeKind.Utc);

        public OwnedEntityAuditIntegrationTests(MiCakeAppFixture fixture)
        {
            _fixture = fixture;
            _serviceProvider = _fixture.CreateServiceProvider(services =>
            {
                services.AddLogging();

                var dbName = Guid.NewGuid().ToString();
                services.AddDbContext<TestDbContext>((sp, options) =>
                {
                    options.UseInMemoryDatabase(dbName);
                    options.ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));
                    options.UseMiCakeInterceptors(sp);
                });

                // Register custom TimeProvider for testing
                services.AddSingleton<TimeProvider>(new FakeTimeProvider(new DateTimeOffset(2025, 1, 21, 10, 30, 0, TimeSpan.Zero)));

                var builder = services.AddMiCake<TestMiCakeModule>();
                builder.UseEFCore<TestDbContext>();
                builder.UseAudit();
                builder.Build();
            });

            _dbContext = _serviceProvider.GetRequiredService<TestDbContext>();
        }

        #region OwnsOne Tests

        [Fact]
        public async Task SaveChanges_WhenOwnedEntityChanged_ShouldUpdateOwnerUpdatedAt()
        {
            // Arrange - Create a book with author
            var book = new BookEntity("Test Book", "John", "Doe");
            _dbContext.Books.Add(book);
            await _dbContext.SaveChangesAsync();
            
            var originalCreatedAt = book.CreatedAt;
            Assert.Equal(_fixedTime, originalCreatedAt);
            Assert.Null(book.UpdatedAt); // Not modified yet

            _dbContext.ChangeTracker.Clear();

            // Act - Change the author (owned entity)
            var existingBook = await _dbContext.Books.FirstAsync(b => b.Id == book.Id);
            existingBook.ChangeAuthor("Jane", "Smith");
            await _dbContext.SaveChangesAsync();

            // Assert - UpdatedAt should be set even though Book itself wasn't directly modified
            Assert.NotNull(existingBook.UpdatedAt);
            Assert.Equal(_fixedTime, existingBook.UpdatedAt.Value);
            Assert.Equal(originalCreatedAt, existingBook.CreatedAt); // CreatedAt unchanged
        }

        [Fact]
        public async Task SaveChanges_WhenOnlyOwnerPropertyChanged_ShouldUpdateUpdatedAt()
        {
            // Arrange
            var book = new BookEntity("Test Book", "John", "Doe");
            _dbContext.Books.Add(book);
            await _dbContext.SaveChangesAsync();
            
            _dbContext.ChangeTracker.Clear();

            // Act - Change only the owner's property (not owned entity)
            var existingBook = await _dbContext.Books.FirstAsync(b => b.Id == book.Id);
            existingBook.UpdateTitle("New Title");
            await _dbContext.SaveChangesAsync();

            // Assert
            Assert.NotNull(existingBook.UpdatedAt);
            Assert.Equal(_fixedTime, existingBook.UpdatedAt.Value);
        }

        [Fact]
        public async Task SaveChanges_WhenBothOwnerAndOwnedChanged_ShouldUpdateUpdatedAt()
        {
            // Arrange
            var book = new BookEntity("Test Book", "John", "Doe");
            _dbContext.Books.Add(book);
            await _dbContext.SaveChangesAsync();
            
            _dbContext.ChangeTracker.Clear();

            // Act - Change both owner and owned entity
            var existingBook = await _dbContext.Books.FirstAsync(b => b.Id == book.Id);
            existingBook.UpdateTitle("New Title");
            existingBook.ChangeAuthor("Jane", "Smith");
            await _dbContext.SaveChangesAsync();

            // Assert
            Assert.NotNull(existingBook.UpdatedAt);
            Assert.Equal(_fixedTime, existingBook.UpdatedAt.Value);
        }

        [Fact]
        public async Task SaveChanges_WhenOwnedEntityNotChanged_ShouldNotUpdateUpdatedAt()
        {
            // Arrange
            var book = new BookEntity("Test Book", "John", "Doe");
            _dbContext.Books.Add(book);
            await _dbContext.SaveChangesAsync();
            
            _dbContext.ChangeTracker.Clear();

            // Act - Just load the entity without changes
            var existingBook = await _dbContext.Books.FirstAsync(b => b.Id == book.Id);
            // Don't make any changes
            await _dbContext.SaveChangesAsync();

            // Assert - UpdatedAt should remain null (no modifications)
            Assert.Null(existingBook.UpdatedAt);
        }

        [Fact]
        public async Task SaveChanges_WithDateTimeOffset_WhenOwnedEntityChanged_ShouldUpdateOwnerUpdatedAt()
        {
            // Arrange
            var article = new ArticleEntity("Test Article", "Category A");
            _dbContext.Articles.Add(article);
            await _dbContext.SaveChangesAsync();
            
            var originalCreatedAt = article.CreatedAt;
            
            _dbContext.ChangeTracker.Clear();

            // Act - Change the metadata (owned entity)
            var existingArticle = await _dbContext.Articles.FirstAsync(a => a.Id == article.Id);
            existingArticle.UpdateMetadata("Category B", 5);
            await _dbContext.SaveChangesAsync();

            // Assert
            Assert.NotNull(existingArticle.UpdatedAt);
            Assert.Equal(_fixedTimeOffset, existingArticle.UpdatedAt.Value);
        }

        #endregion

        #region Multiple Owned Entities Tests

        [Fact]
        public async Task SaveChanges_WhenOneOfMultipleOwnedEntitiesChanged_ShouldUpdateOwnerUpdatedAt()
        {
            // Arrange
            var product = new ProductEntity("Test Product", "USD", 99.99m);
            _dbContext.Products.Add(product);
            await _dbContext.SaveChangesAsync();
            
            _dbContext.ChangeTracker.Clear();

            // Act - Change only the price (one of two owned entities)
            var existingProduct = await _dbContext.Products.FirstAsync(p => p.Id == product.Id);
            existingProduct.UpdatePrice("EUR", 89.99m);
            await _dbContext.SaveChangesAsync();

            // Assert
            Assert.NotNull(existingProduct.UpdatedAt);
            Assert.Equal(_fixedTime, existingProduct.UpdatedAt.Value);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public async Task SaveChanges_CreateEntity_ShouldSetCreatedAtNotUpdatedAt()
        {
            // Arrange
            var book = new BookEntity("New Book", "Author", "Name");

            // Act
            _dbContext.Books.Add(book);
            await _dbContext.SaveChangesAsync();

            // Assert
            Assert.Equal(_fixedTime, book.CreatedAt);
            Assert.Null(book.UpdatedAt); // New entity should not have UpdatedAt
        }

        [Fact]
        public async Task SaveChanges_DeleteEntity_ShouldNotAffectTimestamps()
        {
            // Arrange
            var book = new BookEntity("To Delete", "Author", "Name");
            _dbContext.Books.Add(book);
            await _dbContext.SaveChangesAsync();
            
            var originalCreatedAt = book.CreatedAt;
            _dbContext.ChangeTracker.Clear();

            // Act
            var existingBook = await _dbContext.Books.FirstAsync(b => b.Id == book.Id);
            _dbContext.Books.Remove(existingBook);
            await _dbContext.SaveChangesAsync();

            // Assert - CreatedAt should remain unchanged
            Assert.Equal(originalCreatedAt, existingBook.CreatedAt);
        }

        #endregion

        public void Dispose()
        {
            _fixture?.ReleaseServiceProvider(_serviceProvider);
        }

        #region Test Infrastructure

        [RelyOn(typeof(EntityFrameworkCore.Modules.MiCakeEFCoreModule), typeof(Modules.MiCakeEssentialModule))]
        private class TestMiCakeModule : MiCakeModule
        {
        }

        private class TestDbContext : DbContext
        {
            public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
            {
            }

            public DbSet<BookEntity> Books { get; set; }
            public DbSet<ArticleEntity> Articles { get; set; }
            public DbSet<ProductEntity> Products { get; set; }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                base.OnModelCreating(modelBuilder);
                
                // Configure Book with owned Author
                modelBuilder.Entity<BookEntity>(entity =>
                {
                    entity.OwnsOne(b => b.Author, author =>
                    {
                        author.Property(a => a.FirstName).HasColumnName("AuthorFirstName");
                        author.Property(a => a.LastName).HasColumnName("AuthorLastName");
                    });
                });

                // Configure Article with owned Metadata
                modelBuilder.Entity<ArticleEntity>(entity =>
                {
                    entity.OwnsOne(a => a.Metadata, meta =>
                    {
                        meta.Property(m => m.Category).HasColumnName("Category");
                        meta.Property(m => m.ViewCount).HasColumnName("ViewCount");
                    });
                });

                // Configure Product with multiple owned entities
                modelBuilder.Entity<ProductEntity>(entity =>
                {
                    entity.OwnsOne(p => p.Price, price =>
                    {
                        price.Property(pr => pr.Currency).HasColumnName("PriceCurrency");
                        price.Property(pr => pr.Amount).HasColumnName("PriceAmount");
                    });
                    entity.OwnsOne(p => p.Dimensions, dims =>
                    {
                        dims.Property(d => d.Width).HasColumnName("Width");
                        dims.Property(d => d.Height).HasColumnName("Height");
                        dims.Property(d => d.Depth).HasColumnName("Depth");
                    });
                });

                modelBuilder.UseMiCakeConventions();
            }
        }

        #region Domain Entities

        /// <summary>
        /// Book aggregate root with Author as owned value object (using DateTime).
        /// </summary>
        private class BookEntity : AggregateRoot<Guid>, IHasCreatedAt<DateTime>, IHasUpdatedAt<DateTime>
        {
            public string Title { get; private set; }
            public AuthorValueObject Author { get; private set; }
            public DateTime CreatedAt { get; set; }
            public DateTime? UpdatedAt { get; set; }

            public BookEntity() { }

            public BookEntity(string title, string authorFirstName, string authorLastName)
            {
                Id = Guid.NewGuid();
                Title = title;
                Author = new AuthorValueObject(authorFirstName, authorLastName);
            }

            public void UpdateTitle(string newTitle)
            {
                Title = newTitle;
            }

            public void ChangeAuthor(string firstName, string lastName)
            {
                Author = new AuthorValueObject(firstName, lastName);
            }
        }

        /// <summary>
        /// Author value object.
        /// </summary>
        private class AuthorValueObject : ValueObject
        {
            public string FirstName { get; private set; }
            public string LastName { get; private set; }

            public AuthorValueObject() { }

            public AuthorValueObject(string firstName, string lastName)
            {
                FirstName = firstName;
                LastName = lastName;
            }

            protected override IEnumerable<object> GetEqualityComponents()
            {
                yield return FirstName;
                yield return LastName;
            }
        }

        /// <summary>
        /// Article aggregate root with Metadata as owned value object (using DateTimeOffset).
        /// </summary>
        private class ArticleEntity : AggregateRoot<Guid>, IHasCreatedAt<DateTimeOffset>, IHasUpdatedAt<DateTimeOffset>
        {
            public string Title { get; private set; }
            public ArticleMetadataValueObject Metadata { get; private set; }
            public DateTimeOffset CreatedAt { get; set; }
            public DateTimeOffset? UpdatedAt { get; set; }

            public ArticleEntity() { }

            public ArticleEntity(string title, string category)
            {
                Id = Guid.NewGuid();
                Title = title;
                Metadata = new ArticleMetadataValueObject(category, 0);
            }

            public void UpdateMetadata(string category, int viewCount)
            {
                Metadata = new ArticleMetadataValueObject(category, viewCount);
            }
        }

        /// <summary>
        /// Article metadata value object.
        /// </summary>
        private class ArticleMetadataValueObject : ValueObject
        {
            public string Category { get; private set; }
            public int ViewCount { get; private set; }

            public ArticleMetadataValueObject() { }

            public ArticleMetadataValueObject(string category, int viewCount)
            {
                Category = category;
                ViewCount = viewCount;
            }

            protected override IEnumerable<object> GetEqualityComponents()
            {
                yield return Category;
                yield return ViewCount;
            }
        }

        /// <summary>
        /// Product aggregate root with multiple owned value objects.
        /// </summary>
        private class ProductEntity : AggregateRoot<Guid>, IHasCreatedAt<DateTime>, IHasUpdatedAt<DateTime>
        {
            public string Name { get; private set; }
            public PriceValueObject Price { get; private set; }
            public DimensionsValueObject Dimensions { get; private set; }
            public DateTime CreatedAt { get; set; }
            public DateTime? UpdatedAt { get; set; }

            public ProductEntity() { }

            public ProductEntity(string name, string currency, decimal amount)
            {
                Id = Guid.NewGuid();
                Name = name;
                Price = new PriceValueObject(currency, amount);
                Dimensions = new DimensionsValueObject(10, 10, 10);
            }

            public void UpdatePrice(string currency, decimal amount)
            {
                Price = new PriceValueObject(currency, amount);
            }

            public void UpdateDimensions(double width, double height, double depth)
            {
                Dimensions = new DimensionsValueObject(width, height, depth);
            }
        }

        /// <summary>
        /// Price value object.
        /// </summary>
        private class PriceValueObject : ValueObject
        {
            public string Currency { get; private set; }
            public decimal Amount { get; private set; }

            public PriceValueObject() { }

            public PriceValueObject(string currency, decimal amount)
            {
                Currency = currency;
                Amount = amount;
            }

            protected override IEnumerable<object> GetEqualityComponents()
            {
                yield return Currency;
                yield return Amount;
            }
        }

        /// <summary>
        /// Dimensions value object.
        /// </summary>
        private class DimensionsValueObject : ValueObject
        {
            public double Width { get; private set; }
            public double Height { get; private set; }
            public double Depth { get; private set; }

            public DimensionsValueObject() { }

            public DimensionsValueObject(double width, double height, double depth)
            {
                Width = width;
                Height = height;
                Depth = depth;
            }

            protected override IEnumerable<object> GetEqualityComponents()
            {
                yield return Width;
                yield return Height;
                yield return Depth;
            }
        }

        #endregion

        private class FakeTimeProvider : TimeProvider
        {
            private readonly DateTimeOffset _fixedTime;

            public FakeTimeProvider(DateTimeOffset fixedTime)
            {
                _fixedTime = fixedTime;
            }

            public override DateTimeOffset GetUtcNow() => _fixedTime;
        }

        #endregion
    }
}
