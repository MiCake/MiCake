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
[Collection("MiCakeStaticFactory")]
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
        public void MiCakeDbContext_OnConfiguringDoesNotThrow_AndReadsWork()
        {
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var context = new TestDbContext(options);

            Assert.NotNull(context);
            Assert.Equal(0, context.TestEntities.Count());
        }

        [Fact]
        public void MiCakeDbContext_ParameterlessConstructor_WorksCorrectly()
        {
            // Act & Assert
            using var context = new TestDbContextWithParameterlessConstructor();
            Assert.NotNull(context);
        }

        [Fact]
        public async Task MiCakeDbContext_SaveChangesAsync_WithoutProvider_ThrowsWithGuidance()
        {
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var context = new TestDbContext(options);
            context.TestEntities.Add(new TestEntity { Name = "Async Test" });

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
            Assert.Contains("UseMiCakeInterceptors(IServiceProvider)", exception.Message);
        }

        [Fact]
        public void MiCakeDbContext_MultipleContexts_WithoutProvider_WritesThrowIndependently()
        {
            var options1 = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase("db1")
                .Options;
            var options2 = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase("db2")
                .Options;

            using var context1 = new TestDbContext(options1);
            using var context2 = new TestDbContext(options2);

            context1.TestEntities.Add(new TestEntity { Name = "Context1" });
            context2.TestEntities.Add(new TestEntity { Name = "Context2" });

            var ex1 = Assert.Throws<InvalidOperationException>(() => context1.SaveChanges());
            var ex2 = Assert.Throws<InvalidOperationException>(() => context2.SaveChanges());
            Assert.Contains("UseMiCakeInterceptors(IServiceProvider)", ex1.Message);
            Assert.Contains("UseMiCakeInterceptors(IServiceProvider)", ex2.Message);
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
