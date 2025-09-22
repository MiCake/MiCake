using MiCake.Audit;
using MiCake.Audit.SoftDeletion;
using MiCake.DDD.Domain;
using MiCake.DDD.Extensions.Store.Conventions;
using MiCake.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using Xunit;

namespace MiCake.EntityFrameworkCore.Tests.Extensions
{
    public class EFCoreModelBuilderConventionTests
    {
        [Fact]
        public void AddMiCakeModelSimplified_ShouldApplyBuiltInConventions()
        {
            // Arrange
            var services = new ServiceCollection();
            var serviceProvider = services.BuildServiceProvider();
            
            var options = new DbContextOptionsBuilder<TestConventionDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
                
            // Act
            using var context = new TestConventionDbContext(options, serviceProvider);
            var model = context.Model;
            
            // Assert
            Assert.NotNull(model);
            
            // Check that soft deletion entity has query filter
            var softDeletableType = model.FindEntityType(typeof(TestSoftDeletableDbEntity));
            Assert.NotNull(softDeletableType);
            
            var queryFilter = softDeletableType.GetQueryFilter();
            Assert.NotNull(queryFilter);
        }
        
        [Fact]
        public void ConventionApplication_ShouldRespectPriority()
        {
            // This test verifies that conventions are applied in priority order
            // We test this by checking the convention classes directly
            
            // Arrange & Act
            var softDeletionConvention = new SoftDeletionConvention();
            var auditTimeConvention = new AuditTimeConvention();
            
            // Assert
            Assert.True(softDeletionConvention.Priority < auditTimeConvention.Priority);
        }
        
        [Fact]
        public void MiCakeDbContext_ShouldUseSimplifiedConventions()
        {
            // Arrange
            var services = new ServiceCollection();
            var serviceProvider = services.BuildServiceProvider();
            
            var options = new DbContextOptionsBuilder<TestConventionDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
                
            // Act
            using var context = new TestConventionDbContext(options, serviceProvider);
            context.Database.EnsureCreated();
            
            // Add test data to verify soft deletion
            var activeEntity = new TestSoftDeletableDbEntity { Name = "Active", IsDeleted = false };
            var deletedEntity = new TestSoftDeletableDbEntity { Name = "Deleted", IsDeleted = true };
            
            context.SoftDeletableEntities.AddRange(activeEntity, deletedEntity);
            context.SaveChanges();
            
            // Assert - Only non-deleted entities should be returned
            var entities = context.SoftDeletableEntities.ToList();
            Assert.Single(entities);
            Assert.Equal("Active", entities.First().Name);
        }
        
        [Fact]
        public void PropertyConventions_ShouldBeApplied()
        {
            // Arrange
            var services = new ServiceCollection();
            var serviceProvider = services.BuildServiceProvider();
            
            var options = new DbContextOptionsBuilder<TestConventionDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
                
            // Act
            using var context = new TestConventionDbContext(options, serviceProvider);
            var model = context.Model;
            
            // Assert
            var auditableType = model.FindEntityType(typeof(TestAuditableDbEntity));
            Assert.NotNull(auditableType);
            
            // Check that CreationTime property is configured
            var creationTimeProperty = auditableType.FindProperty(nameof(TestAuditableDbEntity.CreationTime));
            Assert.NotNull(creationTimeProperty);
        }
    }
    
    // Test DbContext for convention testing
    public class TestConventionDbContext : MiCakeDbContext
    {
        public TestConventionDbContext(DbContextOptions options, IServiceProvider serviceProvider) : base(options, serviceProvider) { }
        
        public DbSet<TestSoftDeletableDbEntity> SoftDeletableEntities { get; set; }
        public DbSet<TestAuditableDbEntity> AuditableEntities { get; set; }
        public DbSet<TestRegularDbEntity> RegularEntities { get; set; }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Use the new simplified convention system
            modelBuilder.AddMiCakeModelSimplified();
            base.OnModelCreating(modelBuilder);
        }
    }
    
    // Test entities for DbContext convention testing
    public class TestSoftDeletableDbEntity : Entity, ISoftDeletion
    {
        public string Name { get; set; }
        public bool IsDeleted { get; set; }
    }
    
    public class TestAuditableDbEntity : Entity, IHasCreationTime, IHasModificationTime
    {
        public string Title { get; set; }
        public DateTime CreationTime { get; set; }
        public DateTime? ModificationTime { get; set; }
    }
    
    public class TestRegularDbEntity : Entity
    {
        public string Description { get; set; }
    }
}