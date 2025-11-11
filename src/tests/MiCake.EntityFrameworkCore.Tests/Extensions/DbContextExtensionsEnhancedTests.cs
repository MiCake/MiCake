using MiCake.Audit.Conventions;
using MiCake.DDD.Infrastructure.Store;
using Microsoft.EntityFrameworkCore;
using System;
using Xunit;

namespace MiCake.EntityFrameworkCore.Tests.Extensions
{
    /// <summary>
    /// Enhanced tests for DbContextExtensions, focusing on the new parameterless UseMiCakeInterceptors method
    /// </summary>
    public class DbContextExtensionsEnhancedTests : IDisposable
    {
        public DbContextExtensionsEnhancedTests()
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
        public void UseMiCakeInterceptors_WithoutParameters_WhenFactoryNotConfigured_ShouldNotThrow()
        {
            // Arrange
            var optionsBuilder = new DbContextOptionsBuilder<TestDbContext>();

            // Act & Assert - should not throw even when factory is not configured
            var exception = Record.Exception(() => optionsBuilder.UseMiCakeInterceptors());
            Assert.Null(exception);
        }

        [Fact]
        public void UseMiCakeInterceptors_WithoutParameters_CanBeCalledMultipleTimes()
        {
            // Arrange
            var optionsBuilder = new DbContextOptionsBuilder<TestDbContext>();

            // Act & Assert - calling multiple times should not throw
            optionsBuilder.UseMiCakeInterceptors();
            optionsBuilder.UseMiCakeInterceptors();
            var exception = Record.Exception(() => optionsBuilder.UseMiCakeInterceptors());
            Assert.Null(exception);
        }

        [Fact]
        public void UseMiCakeInterceptors_WithoutParameters_ReturnsCorrectBuilder()
        {
            // Arrange
            var optionsBuilder = new DbContextOptionsBuilder<TestDbContext>();

            // Act
            var result = optionsBuilder.UseMiCakeInterceptors();

            // Assert
            Assert.NotNull(result);
            Assert.Same(optionsBuilder, result); // Should return the same builder for fluent API
        }

        /// <summary>
        /// Test DbContext for extension method testing
        /// </summary>
        public class TestDbContext : DbContext
        {
            public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
            {
            }

            public DbSet<TestEntity> TestEntities { get; set; }
        }

        /// <summary>
        /// Simple test entity
        /// </summary>
        public class TestEntity
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }
    }
}