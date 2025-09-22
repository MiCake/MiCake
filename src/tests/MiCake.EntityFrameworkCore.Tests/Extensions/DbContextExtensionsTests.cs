using MiCake.Audit.SoftDeletion;
using MiCake.DDD.Domain;
using MiCake.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using Xunit;

namespace MiCake.EntityFrameworkCore.Tests.Extensions
{
    public class DbContextExtensionsTests
    {
        [Fact]
        public void CustomDbContext_UsingExtensionMethods_ShouldWorkCorrectly()
        {
            // Arrange
            var services = new ServiceCollection();
            var serviceProvider = services.BuildServiceProvider();
            
            var options = new DbContextOptionsBuilder<CustomUserDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
                
            // Act & Assert
            using var context = new CustomUserDbContext(options, serviceProvider);
            context.Database.EnsureCreated();
            
            // The context should be created without errors
            Assert.NotNull(context);
            Assert.NotNull(context.TestEntities);
        }
        
        [Fact]
        public void CustomDbContext_WithSoftDeletion_ShouldApplyQueryFilter()
        {
            // Arrange
            var services = new ServiceCollection();
            var serviceProvider = services.BuildServiceProvider();
            
            var options = new DbContextOptionsBuilder<CustomUserDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
                
            using var context = new CustomUserDbContext(options, serviceProvider);
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
            
            var optionsBuilder = new DbContextOptionsBuilder();
            var services = new ServiceCollection().BuildServiceProvider();
            
            // Should not throw
            optionsBuilder.UseMiCakeInterceptors(services);
        }
    }
    
    // Example of user's custom DbContext that doesn't inherit from MiCakeDbContext
    public class CustomUserDbContext : DbContext
    {
        private readonly IServiceProvider _serviceProvider;
        
        public CustomUserDbContext(DbContextOptions<CustomUserDbContext> options, IServiceProvider serviceProvider) 
            : base(options)
        {
            _serviceProvider = serviceProvider;
        }
        
        public DbSet<TestSoftDeletableEntity> TestEntities { get; set; }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Use MiCake extension method instead of inheriting
            modelBuilder.UseMiCakeConventions();
        }
        
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            
            // Use MiCake extension method instead of inheriting
            optionsBuilder.UseMiCakeInterceptors(_serviceProvider);
        }
    }
    
    // Test entity for the custom DbContext
    public class TestSoftDeletableEntity : Entity, ISoftDeletion
    {
        public string Name { get; set; }
        public bool IsDeleted { get; set; }
    }
}