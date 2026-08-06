using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace MiCake.EntityFrameworkCore.Tests.Summary
{
    /// <summary>
    /// Documents the write-path contract for MiCakeDbContext without an IServiceProvider:
    /// construction and reads work, but writes fail fast with guidance because the MiCake
    /// write pipeline cannot be resolved without the provider-based configuration.
    /// </summary>
    [Collection("MiCakeStaticFactory")]
    public class NoServiceProviderDependencySummaryTests
    {
        [Fact]
        public void MiCakeDbContext_CanBeCreatedAndRead_WithoutServiceProvider()
        {
            var options = new DbContextOptionsBuilder<TestMiCakeDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var context = new TestMiCakeDbContext(options);

            Assert.NotNull(context);
            Assert.Equal(0, context.TestEntities.Count());
        }

        [Fact]
        public void MiCakeDbContext_SaveChanges_WithoutServiceProvider_ThrowsWithGuidance()
        {
            var options = new DbContextOptionsBuilder<TestMiCakeDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var context = new TestMiCakeDbContext(options);
            context.TestEntities.Add(new TestEntity { Name = "Write without provider" });

            var exception = Assert.Throws<InvalidOperationException>(() => context.SaveChanges());
            Assert.Contains("UseMiCakeInterceptors(IServiceProvider)", exception.Message);
        }

        [Fact]
        public async Task MiCakeDbContext_SaveChangesAsync_WithoutServiceProvider_ThrowsWithGuidance()
        {
            var options = new DbContextOptionsBuilder<TestMiCakeDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var context = new TestMiCakeDbContext(options);
            context.TestEntities.Add(new TestEntity { Name = "Async write without provider" });

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
            Assert.Contains("UseMiCakeInterceptors(IServiceProvider)", exception.Message);
        }

        [Fact]
        public void ParameterlessConstructor_SaveChanges_WithoutServiceProvider_ThrowsWithGuidance()
        {
            using var context = new TestMiCakeDbContextWithParameterlessConstructor();
            context.TestEntities.Add(new TestEntity { Name = "Parameterless write" });

            var exception = Assert.Throws<InvalidOperationException>(() => context.SaveChanges());
            Assert.Contains("UseMiCakeInterceptors(IServiceProvider)", exception.Message);
        }

        private class TestMiCakeDbContext : MiCakeDbContext
        {
            public TestMiCakeDbContext(DbContextOptions options) : base(options)
            {
            }

            public DbSet<TestEntity> TestEntities { get; set; }
        }

        private class TestMiCakeDbContextWithParameterlessConstructor : MiCakeDbContext
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

        private class TestEntity
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }
    }
}
