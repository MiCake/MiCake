using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace MiCake.EntityFrameworkCore.Tests.Integration
{
    /// <summary>
    /// Integration tests for MiCakeDbContext to ensure it works correctly without IServiceProvider dependency
    /// </summary>
    public class MiCakeDbContextIntegrationTests
    {
        [Fact]
        public void MiCakeDbContext_CanBeCreatedWithOptionsOnly()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            // Act & Assert
            using var context = new TestDbContext(options);
            Assert.NotNull(context);
        }

        [Fact]
        public void MiCakeDbContext_OnConfiguringDoesNotThrow()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            // Act
            using var context = new TestDbContext(options);
            
            // Assert - if no exception occurs, the OnConfiguring method worked
            Assert.NotNull(context);
            
            // Verify that the context can be used for basic operations
            context.TestEntities.Add(new TestEntity { Name = "Test" });
            var result = context.SaveChanges();
            Assert.Equal(1, result);
        }

        [Fact]
        public void MiCakeDbContext_ParameterlessConstructor_WorksCorrectly()
        {
            // Act & Assert
            using var context = new TestDbContextWithParameterlessConstructor();
            Assert.NotNull(context);
        }

        [Fact]
        public async Task MiCakeDbContext_SaveChangesAsync_WorksCorrectly()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            // Act
            using var context = new TestDbContext(options);
            context.TestEntities.Add(new TestEntity { Name = "Async Test" });
            var result = await context.SaveChangesAsync();

            // Assert
            Assert.Equal(1, result);
            
            // Verify entity was saved
            var savedEntity = await context.TestEntities.FirstOrDefaultAsync();
            Assert.NotNull(savedEntity);
            Assert.Equal("Async Test", savedEntity.Name);
        }

        [Fact]
        public void MiCakeDbContext_MultipleContexts_WorkIndependently()
        {
            // Arrange
            var options1 = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase("db1")
                .Options;
            var options2 = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase("db2")
                .Options;

            // Act & Assert
            using var context1 = new TestDbContext(options1);
            using var context2 = new TestDbContext(options2);
            
            context1.TestEntities.Add(new TestEntity { Name = "Context1" });
            context2.TestEntities.Add(new TestEntity { Name = "Context2" });
            
            context1.SaveChanges();
            context2.SaveChanges();
            
            Assert.Equal(1, context1.TestEntities.Count());
            Assert.Equal(1, context2.TestEntities.Count());
            Assert.Equal("Context1", context1.TestEntities.First().Name);
            Assert.Equal("Context2", context2.TestEntities.First().Name);
        }

        /// <summary>
        /// Test implementation of MiCakeDbContext
        /// </summary>
        private class TestDbContext : MiCakeDbContext
        {
            public TestDbContext(DbContextOptions options) : base(options)
            {
            }

            public DbSet<TestEntity> TestEntities { get; set; }
        }

        /// <summary>
        /// Test DbContext using parameterless constructor
        /// </summary>
        private class TestDbContextWithParameterlessConstructor : MiCakeDbContext
        {
            protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            {
                if (!optionsBuilder.IsConfigured)
                {
                    optionsBuilder.UseInMemoryDatabase(Guid.NewGuid().ToString());
                }
                base.OnConfiguring(optionsBuilder);
            }

            public DbSet<TestEntity> TestEntities { get; set; }
        }

        /// <summary>
        /// Simple test entity
        /// </summary>
        private class TestEntity
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }
    }
}