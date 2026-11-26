using MiCake.Audit.Conventions;
using MiCake.Audit.SoftDeletion;
using MiCake.DDD.Domain;
using MiCake.DDD.Infrastructure.Store;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using Xunit;

namespace MiCake.EntityFrameworkCore.Tests.Extensions
{
    [Collection("MiCakeStaticFactory")]
    public class SoftDeleteQueryFilterTests : IDisposable
    {
        public SoftDeleteQueryFilterTests()
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
        public void SoftDeletableEntity_GetsOneNamedFilter()
        {
            using var context = NewContext<SdNamedEntity>(modelBuilder => modelBuilder.UseMiCakeConventions());

            var filter = Assert.Single(EntityTypeOf<SdNamedEntity>(context).GetDeclaredQueryFilters());
            Assert.Equal(MiCakeQueryFilterNames.SoftDelete, filter.Key);
            Assert.False(filter.IsAnonymous);
        }

        [Fact]
        public void UserNamedFilterConfiguredAfterConventions_CoexistsAndSoftDeleteCanBeBypassedAlone()
        {
            using var context = NewContext<SdTenantAfterEntity>(modelBuilder =>
            {
                modelBuilder.UseMiCakeConventions();
                modelBuilder.Entity<SdTenantAfterEntity>().HasQueryFilter("User.Tenant", e => e.TenantId == 1);
            });

            AssertCoexistence(context, context.Set<SdTenantAfterEntity>());
        }

        [Fact]
        public void UserNamedFilterConfiguredBeforeConventions_CoexistsAndSoftDeleteCanBeBypassedAlone()
        {
            using var context = NewContext<SdTenantBeforeEntity>(modelBuilder =>
            {
                modelBuilder.Entity<SdTenantBeforeEntity>().HasQueryFilter("User.Tenant", e => e.TenantId == 1);
                modelBuilder.UseMiCakeConventions();
            });

            AssertCoexistence(context, context.Set<SdTenantBeforeEntity>());
        }

        [Fact]
        public void AnonymousFilterConfiguredBeforeConventions_ThrowsConventionException()
        {
            var exception = Assert.Throws<MiCakeConventionException>(() =>
            {
                using var context = NewContext<SdAnonymousBeforeEntity>(modelBuilder =>
                {
                    modelBuilder.Entity<SdAnonymousBeforeEntity>().HasQueryFilter(e => !e.IsDeleted);
                    modelBuilder.UseMiCakeConventions();
                });
                _ = context.Model;
            });

            Assert.Equal(typeof(SdAnonymousBeforeEntity), exception.EntityType);
            Assert.Contains(nameof(SdAnonymousBeforeEntity), exception.Message);
            Assert.Contains("anonymous", exception.Message);
        }

        [Fact]
        public void AnonymousFilterConfiguredAfterConventions_ThrowsEfCoreExceptionInUserCode()
        {
            var exception = Assert.Throws<InvalidOperationException>(() =>
            {
                using var context = NewContext<SdAnonymousAfterEntity>(modelBuilder =>
                {
                    modelBuilder.UseMiCakeConventions();
                    modelBuilder.Entity<SdAnonymousAfterEntity>().HasQueryFilter(e => !e.IsDeleted);
                });
                _ = context.Model;
            });

            Assert.IsNotType<MiCakeConventionException>(exception);
        }

        [Fact]
        public void SoftDeletableHierarchy_GetsTheFilterOnTheRootOnly()
        {
            using var context = NewContext<SdRootEntity>(modelBuilder =>
            {
                modelBuilder.Entity<SdDerivedEntity>();
                modelBuilder.UseMiCakeConventions();
            });

            var model = context.Model;
            var rootFilter = Assert.Single(model.FindEntityType(typeof(SdRootEntity)).GetDeclaredQueryFilters());
            Assert.Equal(MiCakeQueryFilterNames.SoftDelete, rootFilter.Key);
            Assert.Empty(model.FindEntityType(typeof(SdDerivedEntity)).GetDeclaredQueryFilters());

            context.Database.EnsureCreated();
            context.AddRange(
                new SdDerivedEntity { Name = "live" },
                new SdDerivedEntity { Name = "deleted", IsDeleted = true });
            context.SaveChanges();

            Assert.Equal("live", Assert.Single(context.Set<SdDerivedEntity>().ToList()).Name);
            Assert.Equal(2, context.Set<SdDerivedEntity>().IgnoreQueryFilters(new[] { MiCakeQueryFilterNames.SoftDelete }).Count());
        }

        [Fact]
        public void SoftDeletableDerivedTypeWithoutSoftDeletableRoot_ThrowsConventionException()
        {
            var exception = Assert.Throws<MiCakeConventionException>(() =>
            {
                using var context = NewContext<SdPlainRootEntity>(modelBuilder =>
                {
                    modelBuilder.Entity<SdSoftDerivedEntity>();
                    modelBuilder.UseMiCakeConventions();
                });
                _ = context.Model;
            });

            Assert.Equal(typeof(SdSoftDerivedEntity), exception.EntityType);
            Assert.Contains(nameof(SdSoftDerivedEntity), exception.Message);
            Assert.Contains(nameof(SdPlainRootEntity), exception.Message);
        }

        [Fact]
        public void UserFilterWithTheSoftDeleteName_ConfiguredBeforeConventions_IsKept()
        {
            using var context = NewContext<SdOwnFilterBeforeEntity>(modelBuilder =>
            {
                modelBuilder.Entity<SdOwnFilterBeforeEntity>().HasQueryFilter(MiCakeQueryFilterNames.SoftDelete, e => true);
                modelBuilder.UseMiCakeConventions();
            });

            AssertUserFilterWins(context, context.Set<SdOwnFilterBeforeEntity>(), new SdOwnFilterBeforeEntity { Name = "live" }, new SdOwnFilterBeforeEntity { Name = "deleted", IsDeleted = true });
        }

        [Fact]
        public void UserFilterWithTheSoftDeleteName_ConfiguredAfterConventions_ReplacesIt()
        {
            using var context = NewContext<SdOwnFilterAfterEntity>(modelBuilder =>
            {
                modelBuilder.UseMiCakeConventions();
                modelBuilder.Entity<SdOwnFilterAfterEntity>().HasQueryFilter(MiCakeQueryFilterNames.SoftDelete, e => true);
            });

            AssertUserFilterWins(context, context.Set<SdOwnFilterAfterEntity>(), new SdOwnFilterAfterEntity { Name = "live" }, new SdOwnFilterAfterEntity { Name = "deleted", IsDeleted = true });
        }

        [Fact]
        public void ApplyingConventionsTwiceToTheSameModel_KeepsOneNamedFilter()
        {
            var modelBuilder = new ModelBuilder();
            modelBuilder.Entity<SdIdempotentEntity>();

            modelBuilder.UseMiCakeConventions();
            modelBuilder.UseMiCakeConventions();

            var filter = Assert.Single(modelBuilder.Model.FindEntityType(typeof(SdIdempotentEntity)).GetDeclaredQueryFilters());
            Assert.Equal(MiCakeQueryFilterNames.SoftDelete, filter.Key);
        }

        [Fact]
        public void RootThatEnablesSoftDeletionWithoutAQueryFilter_ThrowsConventionExceptionNamingTheMissingFilter()
        {
            var engine = new StoreConventionEngine();
            engine.AddConvention(new SoftDeletionWithoutFilterOnRootConvention(typeof(SdFilterlessRoot), typeof(SdFilterlessDerived)));
            MiCakeConventionEngineProvider.SetConventionEngine(engine);

            var exception = Assert.Throws<MiCakeConventionException>(() =>
            {
                using var context = NewContext<SdFilterlessRoot>(modelBuilder =>
                {
                    modelBuilder.Entity<SdFilterlessDerived>();
                    modelBuilder.UseMiCakeConventions();
                });
                _ = context.Model;
            });

            Assert.Equal(typeof(SdFilterlessDerived), exception.EntityType);
            Assert.Contains(nameof(SdFilterlessRoot), exception.Message);
            Assert.Contains("did not produce a query filter", exception.Message);
        }

        [Fact]
        public void SoftDeletableRootThatIsNotADomainType_StillGetsTheFilterThroughItsDerivedType()
        {
            using var context = NewContext<SdNonDomainRoot>(modelBuilder =>
            {
                modelBuilder.Entity<SdDomainDerivedOfNonDomainRoot>();
                modelBuilder.UseMiCakeConventions();
            });

            var rootFilter = Assert.Single(context.Model.FindEntityType(typeof(SdNonDomainRoot)).GetDeclaredQueryFilters());
            Assert.Equal(MiCakeQueryFilterNames.SoftDelete, rootFilter.Key);
            Assert.Empty(context.Model.FindEntityType(typeof(SdDomainDerivedOfNonDomainRoot)).GetDeclaredQueryFilters());
        }

        [Fact]
        public void ConventionFilterWithoutSoftDeletion_LogsWarningAndAppliesNothing()
        {
            var engine = new StoreConventionEngine();
            engine.AddConvention(new QueryFilterOnlyConvention(typeof(SdQueryOnlyEntity)));
            MiCakeConventionEngineProvider.SetConventionEngine(engine);
            var logs = new CapturingLoggerFactory();

            using var context = NewContext<SdQueryOnlyEntity>(modelBuilder => modelBuilder.UseMiCakeConventions(logs));

            Assert.Empty(EntityTypeOf<SdQueryOnlyEntity>(context).GetDeclaredQueryFilters());
            var entry = Assert.Single(logs.Entries, e => e.Level == LogLevel.Warning);
            Assert.Equal(
                "A convention produced a query filter for {EntityType} without enabling soft deletion; the filter was not applied",
                entry.Template);
            Assert.Contains(nameof(SdQueryOnlyEntity), entry.Message);
        }

        private static void AssertCoexistence<TEntity>(SdContext<TEntity> context, DbSet<TEntity> set)
            where TEntity : Entity, ISoftDeletable, ISdTenantOwned
        {
            var filterKeys = EntityTypeOf<TEntity>(context).GetDeclaredQueryFilters().Select(f => f.Key).ToList();
            Assert.Contains(MiCakeQueryFilterNames.SoftDelete, filterKeys);
            Assert.Contains("User.Tenant", filterKeys);

            context.Database.EnsureCreated();
            context.AddRange(
                NewTenantRow<TEntity>("live-tenant1", 1, false),
                NewTenantRow<TEntity>("deleted-tenant1", 1, true),
                NewTenantRow<TEntity>("live-tenant2", 2, false));
            context.SaveChanges();

            Assert.Equal(new[] { "live-tenant1" }, set.Select(e => e.Name).ToArray());
            Assert.Equal(
                new[] { "deleted-tenant1", "live-tenant1" },
                set.IgnoreQueryFilters(new[] { MiCakeQueryFilterNames.SoftDelete }).Select(e => e.Name).OrderBy(n => n).ToArray());
            Assert.Equal(3, set.IgnoreQueryFilters().Count());
        }

        private static void AssertUserFilterWins<TEntity>(SdContext<TEntity> context, DbSet<TEntity> set, params TEntity[] rows)
            where TEntity : Entity, ISoftDeletable
        {
            var filter = Assert.Single(EntityTypeOf<TEntity>(context).GetDeclaredQueryFilters());
            Assert.Equal(MiCakeQueryFilterNames.SoftDelete, filter.Key);

            context.Database.EnsureCreated();
            context.AddRange(rows);
            context.SaveChanges();

            Assert.Equal(rows.Length, set.Count());
        }

        private static TEntity NewTenantRow<TEntity>(string name, int tenantId, bool isDeleted) where TEntity : Entity, ISoftDeletable, ISdTenantOwned
        {
            var row = Activator.CreateInstance<TEntity>();
            row.Name = name;
            row.TenantId = tenantId;
            row.IsDeleted = isDeleted;
            return row;
        }

        private static Microsoft.EntityFrameworkCore.Metadata.IEntityType EntityTypeOf<TEntity>(DbContext context)
        {
            return context.Model.FindEntityType(typeof(TEntity));
        }

        private static SdContext<TEntity> NewContext<TEntity>(Action<ModelBuilder> configure) where TEntity : class
        {
            var options = new DbContextOptionsBuilder<SdContext<TEntity>>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new SdContext<TEntity>(options, configure);
        }
    }

    public interface ISdTenantOwned
    {
        string Name { get; set; }
        int TenantId { get; set; }
    }

    public class QueryFilterOnlyConvention(Type target) : IEntityConvention
    {
        public int Priority => 10;

        public bool CanApply(Type entityType) => entityType == target;

        public void Configure(Type entityType, EntityConventionContext context)
        {
            var parameter = System.Linq.Expressions.Expression.Parameter(entityType, "e");
            context.QueryFilter = System.Linq.Expressions.Expression.Lambda(System.Linq.Expressions.Expression.Constant(true), parameter);
        }
    }

    // EF Core caches one model per context type, so every scenario needs its own TEntity;
    // reusing an entity type with a different configure delegate silently returns the first model.
    public class SdContext<TEntity> : DbContext where TEntity : class
    {
        private readonly Action<ModelBuilder> _configure;

        public SdContext(DbContextOptions<SdContext<TEntity>> options, Action<ModelBuilder> configure) : base(options)
        {
            _configure = configure;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TEntity>();
            _configure(modelBuilder);
        }
    }

    public class SdNamedEntity : Entity, ISoftDeletable
    {
        public string Name { get; set; }
        public bool IsDeleted { get; set; }
    }

    public class SdTenantAfterEntity : Entity, ISoftDeletable, ISdTenantOwned
    {
        public string Name { get; set; }
        public int TenantId { get; set; }
        public bool IsDeleted { get; set; }
    }

    public class SdTenantBeforeEntity : Entity, ISoftDeletable, ISdTenantOwned
    {
        public string Name { get; set; }
        public int TenantId { get; set; }
        public bool IsDeleted { get; set; }
    }

    public class SdAnonymousBeforeEntity : Entity, ISoftDeletable
    {
        public string Name { get; set; }
        public bool IsDeleted { get; set; }
    }

    public class SdAnonymousAfterEntity : Entity, ISoftDeletable
    {
        public string Name { get; set; }
        public bool IsDeleted { get; set; }
    }

    public class SdRootEntity : Entity, ISoftDeletable
    {
        public string Name { get; set; }
        public bool IsDeleted { get; set; }
    }

    public class SdDerivedEntity : SdRootEntity
    {
        public string Extra { get; set; }
    }

    public class SdPlainRootEntity : Entity
    {
        public string Name { get; set; }
    }

    public class SdSoftDerivedEntity : SdPlainRootEntity, ISoftDeletable
    {
        public bool IsDeleted { get; set; }
    }

    public class SdOwnFilterBeforeEntity : Entity, ISoftDeletable
    {
        public string Name { get; set; }
        public bool IsDeleted { get; set; }
    }

    public class SdOwnFilterAfterEntity : Entity, ISoftDeletable
    {
        public string Name { get; set; }
        public bool IsDeleted { get; set; }
    }

    public class SdIdempotentEntity : Entity, ISoftDeletable
    {
        public string Name { get; set; }
        public bool IsDeleted { get; set; }
    }

    public class SdQueryOnlyEntity : Entity
    {
        public string Name { get; set; }
    }

    public class SdFilterlessRoot : Entity
    {
        public string Name { get; set; }
    }

    public class SdFilterlessDerived : SdFilterlessRoot
    {
        public bool IsDeleted { get; set; }
    }

    // Enables soft deletion for both types but only hands the derived type a filter expression.
    public class SoftDeletionWithoutFilterOnRootConvention(Type root, Type derived) : IEntityConvention
    {
        public int Priority => 10;

        public bool CanApply(Type entityType) => entityType == root || entityType == derived;

        public void Configure(Type entityType, EntityConventionContext context)
        {
            context.EnableSoftDeletion = true;
            if (entityType == derived)
            {
                var parameter = System.Linq.Expressions.Expression.Parameter(entityType, "e");
                context.QueryFilter = System.Linq.Expressions.Expression.Lambda(System.Linq.Expressions.Expression.Constant(true), parameter);
            }
        }
    }

    public class SdNonDomainRoot : ISoftDeletable
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsDeleted { get; set; }
    }

    public class SdDomainDerivedOfNonDomainRoot : SdNonDomainRoot, IEntity
    {
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public System.Collections.Generic.IReadOnlyCollection<MiCake.DDD.Domain.IDomainEvent> DomainEvents { get; } = Array.Empty<MiCake.DDD.Domain.IDomainEvent>();

        public void ClearDomainEvents() { }
    }
}
