using MiCake.Audit.Conventions;
using MiCake.Audit.SoftDeletion;
using MiCake.DDD.Domain;
using MiCake.DDD.Infrastructure.Store;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using Xunit;

namespace MiCake.EntityFrameworkCore.Tests.Extensions
{
    [Collection("MiCakeStaticFactory")]
    public class ConventionEngineLifetimeTests : IDisposable
    {
        public ConventionEngineLifetimeTests()
        {
            var engine = new StoreConventionEngine();
            engine.AddConvention(new SoftDeletionConvention());
            MiCakeConventionEngineProvider.SetConventionEngine(engine);
        }

        public void Dispose()
        {
            MiCakeConventionEngineProvider.Clear();
        }

        [Fact]
        public void SecondDbContextType_ReceivesConventionsAfterFirstModelBuild()
        {
            using (var first = new LifetimeFirstDbContext(NewOptions<LifetimeFirstDbContext>()))
            {
                _ = first.Model;
            }

            using var second = new LifetimeSecondDbContext(NewOptions<LifetimeSecondDbContext>());
            var entityType = second.Model.FindEntityType(typeof(LifetimeSecondEntity));

            Assert.NotNull(entityType);
            Assert.Single(entityType.GetDeclaredQueryFilters());

            second.Database.EnsureCreated();
            second.Entities.AddRange(
                new LifetimeSecondEntity { Name = "Active" },
                new LifetimeSecondEntity { Name = "Deleted", IsDeleted = true });
            second.SaveChanges();

            Assert.Equal("Active", Assert.Single(second.Entities.ToList()).Name);
        }

        [Fact]
        public void ClearConventionCache_KeepsConfiguredEngine()
        {
            ModelBuilderExtensions.ClearConventionCache();

            Assert.NotNull(MiCakeConventionEngineProvider.GetConventionEngine());
        }

        private static DbContextOptions<TContext> NewOptions<TContext>() where TContext : DbContext
        {
            return new DbContextOptionsBuilder<TContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
        }
    }

    public class LifetimeFirstEntity : Entity, ISoftDeletable
    {
        public string Name { get; set; }
        public bool IsDeleted { get; set; }
    }

    public class LifetimeSecondEntity : Entity, ISoftDeletable
    {
        public string Name { get; set; }
        public bool IsDeleted { get; set; }
    }

    public class LifetimeFirstDbContext : DbContext
    {
        public LifetimeFirstDbContext(DbContextOptions<LifetimeFirstDbContext> options) : base(options) { }

        public DbSet<LifetimeFirstEntity> Entities { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.UseMiCakeConventions();
        }
    }

    public class LifetimeSecondDbContext : DbContext
    {
        public LifetimeSecondDbContext(DbContextOptions<LifetimeSecondDbContext> options) : base(options) { }

        public DbSet<LifetimeSecondEntity> Entities { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.UseMiCakeConventions();
        }
    }
}
