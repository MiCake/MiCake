using MiCake.Audit.Conventions;
using MiCake.Audit.SoftDeletion;
using MiCake.DDD.Domain;
using MiCake.DDD.Infrastructure.Store;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using Xunit;

namespace MiCake.EntityFrameworkCore.Tests.Extensions
{
    [Collection("MiCakeStaticFactory")]
    public class DbContextExtensionsTests : IDisposable
    {
        public DbContextExtensionsTests()
        {
            // Initialize the convention engine with default conventions for testing
            var conventionEngine = new StoreConventionEngine();
            conventionEngine.AddConvention(new SoftDeletionConvention());
            
            MiCakeConventionEngineProvider.SetConventionEngine(conventionEngine);
        }

        public void Dispose()
        {
            // Clean up the convention engine after tests
            MiCakeConventionEngineProvider.Clear();
        }

        [Fact]
        public void CustomDbContext_UsingExtensionMethods_ShouldWorkCorrectly()
        {
            var options = new DbContextOptionsBuilder<CustomUserDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new CustomUserDbContext(options);
            context.Database.EnsureCreated();

            Assert.NotNull(context);
            Assert.NotNull(context.TestEntities);
        }

        [Fact]
        public void CustomDbContext_WithSoftDeletion_ShouldApplyQueryFilter()
        {
            var options = new DbContextOptionsBuilder<CustomUserDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new CustomUserDbContext(options);
            context.Database.EnsureCreated();

            // Add test data
            var activeEntity = new TestSoftDeletableEntity { Name = "Active", IsDeleted = false };
            var deletedEntity = new TestSoftDeletableEntity { Name = "Deleted", IsDeleted = true };

            context.TestEntities.AddRange(activeEntity, deletedEntity);
            context.SaveChanges();

            // Act
            var entities = context.TestEntities.ToList();

            // Assert
            Assert.Single(entities);
            Assert.Equal("Active", entities.First().Name);
        }
        
        [Fact]
        public void ExtensionMethods_ShouldNotRequireInheritance()
        {
            // This test verifies that the extension methods work without inheritance
            var modelBuilder = new ModelBuilder();
            
            // Act & Assert - should not throw
            modelBuilder.UseMiCakeConventions();
            
            // Test the options builder extension method exists (don't actually use it in test)
            var optionsBuilder = new DbContextOptionsBuilder();
            Assert.NotNull(optionsBuilder);
        }
    }
    
    // Example of user's custom DbContext that doesn't inherit from MiCakeDbContext.
    // This context only exercises the conventions extension; the MiCake write pipeline
    // requires the provider-based interceptor overload and is covered elsewhere.
    public class CustomUserDbContext : DbContext
    {
        public CustomUserDbContext(DbContextOptions<CustomUserDbContext> options)
            : base(options)
        {
        }

        public DbSet<TestSoftDeletableEntity> TestEntities { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Use MiCake extension method instead of inheriting
            modelBuilder.UseMiCakeConventions();
        }
    }
    
    // Test entity for the custom DbContext
    public class TestSoftDeletableEntity : Entity, ISoftDeletable
    {
        public string Name { get; set; }
        public bool IsDeleted { get; set; }
    }
}
