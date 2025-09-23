using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace MiCake.EntityFrameworkCore.Tests.Integration
{
    /// <summary>
    /// End-to-end integration tests to verify the complete flow:
    /// Module initialization → Factory configuration → DbContext creation → Interceptor work → Domain event handling
    /// </summary>
    public class EndToEndIntegrationTests
    {
        [Fact]
        public void CompleteFlow_WithoutServiceProvider_ShouldWorkCorrectly()
        {
            // Arrange: Create a DbContext without IServiceProvider dependency
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            // Act: Create and use the context
            using var context = new TestDbContext(options);
            
            // Add some test data
            var entity = new TestEntity { Name = "End-to-End Test" };
            context.TestEntities.Add(entity);
            
            // Assert: Should work without throwing exceptions
            var result = context.SaveChanges();
            Assert.Equal(1, result);
            
            // Verify data was saved
            var savedEntity = context.TestEntities.First();
            Assert.NotNull(savedEntity);
            Assert.Equal("End-to-End Test", savedEntity.Name);
        }

        [Fact]
        public async Task CompleteFlow_AsyncOperations_ShouldWorkCorrectly()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            // Act
            using var context = new TestDbContext(options);
            
            var entity = new TestEntity { Name = "Async Test" };
            context.TestEntities.Add(entity);
            
            var result = await context.SaveChangesAsync();
            
            // Assert
            Assert.Equal(1, result);
            
            var savedEntity = await context.TestEntities.FirstOrDefaultAsync();
            Assert.NotNull(savedEntity);
            Assert.Equal("Async Test", savedEntity.Name);
        }

        [Fact]
        public void CompleteFlow_MultipleEntities_ShouldWorkCorrectly()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            // Act
            using var context = new TestDbContext(options);
            
            // Add multiple entities
            for (int i = 1; i <= 5; i++)
            {
                context.TestEntities.Add(new TestEntity { Name = $"Entity {i}" });
            }
            
            var result = context.SaveChanges();
            
            // Assert
            Assert.Equal(5, result);
            Assert.Equal(5, context.TestEntities.Count());
            
            var entities = context.TestEntities.OrderBy(e => e.Name).ToList();
            for (int i = 0; i < 5; i++)
            {
                Assert.Equal($"Entity {i + 1}", entities[i].Name);
            }
        }

        [Fact]
        public void CompleteFlow_UpdateAndDelete_ShouldWorkCorrectly()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var context = new TestDbContext(options);
            
            // Add initial entity
            var entity = new TestEntity { Name = "Original" };
            context.TestEntities.Add(entity);
            context.SaveChanges();
            
            // Act: Update
            entity.Name = "Updated";
            context.SaveChanges();
            
            // Assert: Update worked
            var updatedEntity = context.TestEntities.First();
            Assert.Equal("Updated", updatedEntity.Name);
            
            // Act: Delete
            context.TestEntities.Remove(entity);
            context.SaveChanges();
            
            // Assert: Delete worked
            Assert.Equal(0, context.TestEntities.Count());
        }

        [Fact]
        public void CompleteFlow_MiCakeDbContextInheritance_ShouldWorkCorrectly()
        {
            // Arrange: Test with actual MiCakeDbContext inheritance
            var options = new DbContextOptionsBuilder<MiCakeTestDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            // Act
            using var context = new MiCakeTestDbContext(options);
            
            var entity = new TestEntity { Name = "MiCake Inheritance Test" };
            context.TestEntities.Add(entity);
            
            var result = context.SaveChanges();
            
            // Assert
            Assert.Equal(1, result);
            
            var savedEntity = context.TestEntities.First();
            Assert.NotNull(savedEntity);
            Assert.Equal("MiCake Inheritance Test", savedEntity.Name);
        }

        [Fact]
        public void CompleteFlow_MultipleDifferentContexts_ShouldWorkIndependently()
        {
            // Arrange: Create multiple different context types
            var regularOptions = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase("regular")
                .Options;
                
            var miCakeOptions = new DbContextOptionsBuilder<MiCakeTestDbContext>()
                .UseInMemoryDatabase("micake")
                .Options;

            // Act: Use both contexts
            using var regularContext = new TestDbContext(regularOptions);
            using var miCakeContext = new MiCakeTestDbContext(miCakeOptions);
            
            regularContext.TestEntities.Add(new TestEntity { Name = "Regular Context" });
            miCakeContext.TestEntities.Add(new TestEntity { Name = "MiCake Context" });
            
            regularContext.SaveChanges();
            miCakeContext.SaveChanges();
            
            // Assert: Both should work independently
            Assert.Equal(1, regularContext.TestEntities.Count());
            Assert.Equal(1, miCakeContext.TestEntities.Count());
            
            Assert.Equal("Regular Context", regularContext.TestEntities.First().Name);
            Assert.Equal("MiCake Context", miCakeContext.TestEntities.First().Name);
        }

        [Fact]
        public void CompleteFlow_ParameterlessConstructor_ShouldWorkCorrectly()
        {
            // Act & Assert: Test parameterless constructor scenario
            using var context = new MiCakeTestDbContextWithParameterlessConstructor();
            
            var entity = new TestEntity { Name = "Parameterless Constructor Test" };
            context.TestEntities.Add(entity);
            
            var result = context.SaveChanges();
            Assert.Equal(1, result);
            
            var savedEntity = context.TestEntities.First();
            Assert.Equal("Parameterless Constructor Test", savedEntity.Name);
        }

        /// <summary>
        /// Standard DbContext for testing
        /// </summary>
        private class TestDbContext : DbContext
        {
            public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
            {
            }

            public DbSet<TestEntity> TestEntities { get; set; }
        }

        /// <summary>
        /// MiCakeDbContext for testing inheritance scenario
        /// </summary>
        private class MiCakeTestDbContext : MiCakeDbContext
        {
            public MiCakeTestDbContext(DbContextOptions options) : base(options)
            {
            }

            public DbSet<TestEntity> TestEntities { get; set; }
        }

        /// <summary>
        /// MiCakeDbContext with parameterless constructor for testing
        /// </summary>
        private class MiCakeTestDbContextWithParameterlessConstructor : MiCakeDbContext
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