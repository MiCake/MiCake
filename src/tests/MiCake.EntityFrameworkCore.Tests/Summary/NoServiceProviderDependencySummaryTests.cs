using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using Xunit;

namespace MiCake.EntityFrameworkCore.Tests.Summary
{
    /// <summary>
    /// Summary test demonstrating that MiCakeDbContext no longer requires IServiceProvider dependency
    /// This test serves as documentation of the completed feature
    /// </summary>
    public class NoServiceProviderDependencySummaryTests
    {
        [Fact]
        public void MiCakeDbContext_NoLongerRequiresServiceProvider_SuccessfullyCompleted()
        {
            // ✅ BEFORE: MiCakeDbContext required IServiceProvider in constructor
            // ❌ public MiCakeDbContext(DbContextOptions options, IServiceProvider serviceProvider)
            
            // ✅ AFTER: MiCakeDbContext only requires DbContextOptions
            // ✅ public MiCakeDbContext(DbContextOptions options)
            
            // Arrange: Create context without any IServiceProvider dependency
            var options = new DbContextOptionsBuilder<TestMiCakeDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            // Act: Successfully create MiCakeDbContext without IServiceProvider
            using var context = new TestMiCakeDbContext(options);

            // Assert: Context works perfectly
            Assert.NotNull(context);
            
            // Verify basic functionality works
            context.TestEntities.Add(new TestEntity { Name = "Dependency Elimination Test" });
            var result = context.SaveChanges();
            
            Assert.Equal(1, result);
            Assert.Equal(1, context.TestEntities.Count());
        }

        [Fact]
        public void MiCakeDbContext_StaticFactoryPattern_EliminatesServiceProviderDependency()
        {
            // ✅ IMPLEMENTATION DETAILS:
            // 1. Created MiCakeInterceptorFactory static factory
            // 2. Added parameterless UseMiCakeInterceptors() extension method
            // 3. Modified MiCakeDbContext.OnConfiguring to use parameterless version
            // 4. Factory is configured during module initialization via MiCakeBuilderEFCoreExtension
            
            // This test demonstrates the clean usage pattern
            var options = new DbContextOptionsBuilder<TestMiCakeDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var context = new TestMiCakeDbContext(options);
            
            // Verify the context can perform all standard operations
            var entity1 = new TestEntity { Name = "Entity 1" };
            var entity2 = new TestEntity { Name = "Entity 2" };
            
            context.TestEntities.AddRange(entity1, entity2);
            context.SaveChanges();
            
            // Update operation
            entity1.Name = "Updated Entity 1";
            context.SaveChanges();
            
            // Delete operation
            context.TestEntities.Remove(entity2);
            context.SaveChanges();
            
            // Verify final state
            Assert.Equal(1, context.TestEntities.Count());
            Assert.Equal("Updated Entity 1", context.TestEntities.First().Name);
        }

        [Fact]
        public void ParameterlessConstructor_AlsoWorksWithoutServiceProvider()
        {
            // ✅ BONUS: Parameterless constructor also works without dependency injection
            using var context = new TestMiCakeDbContextWithParameterlessConstructor();
            
            context.TestEntities.Add(new TestEntity { Name = "Parameterless Test" });
            var result = context.SaveChanges();
            
            Assert.Equal(1, result);
        }

        /// <summary>
        /// Test MiCakeDbContext demonstrating the simplified constructor
        /// </summary>
        private class TestMiCakeDbContext : MiCakeDbContext
        {
            public TestMiCakeDbContext(DbContextOptions options) : base(options)
            {
                // ✅ No IServiceProvider required anymore!
            }

            public DbSet<TestEntity> TestEntities { get; set; }
        }

        /// <summary>
        /// Test MiCakeDbContext using parameterless constructor
        /// </summary>
        private class TestMiCakeDbContextWithParameterlessConstructor : MiCakeDbContext
        {
            protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            {
                if (!optionsBuilder.IsConfigured)
                {
                    optionsBuilder.UseInMemoryDatabase(Guid.NewGuid().ToString());
                }
                base.OnConfiguring(optionsBuilder); // ✅ Uses parameterless UseMiCakeInterceptors()
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