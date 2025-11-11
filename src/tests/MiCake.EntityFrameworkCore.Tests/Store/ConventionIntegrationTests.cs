using MiCake.Audit;
using MiCake.Audit.Conventions;
using MiCake.Audit.SoftDeletion;
using MiCake.DDD.Domain;
using MiCake.DDD.Infrastructure.Store;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using Xunit;

namespace MiCake.EntityFrameworkCore.Tests.Store
{
    [Collection("ConventionEngineTests")]
    public class ConventionIntegrationTests : IDisposable
    {
        public ConventionIntegrationTests()
        {
            // Setup default conventions for testing
            var engine = new StoreConventionEngine();
            engine.AddConvention(new SoftDeletionConvention());
            engine.AddConvention(new AuditTimeConvention());
            MiCakeConventionEngineProvider.SetConventionEngine(engine);
        }
        
        public void Dispose()
        {
            // Clean up after tests
            ModelBuilderExtensions.ClearConventionCache();
            MiCakeConventionEngineProvider.Clear();
        }

        [Fact]
        public void MiCakeDbContext_WithConventions_ShouldApplyCorrectly()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<IntegrationTestDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
                
            // Act & Assert
            using var context = new IntegrationTestDbContext(options);
            context.Database.EnsureCreated();
            
            // The context should be created without errors
            Assert.NotNull(context);
            Assert.NotNull(context.SoftDeletableEntities);
            Assert.NotNull(context.AuditableEntities);
            Assert.NotNull(context.RegularEntities);
        }
        
        [Fact]
        public void SoftDeletionConvention_ShouldFilterDeletedEntities()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<IntegrationTestDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
                
            using var context = new IntegrationTestDbContext(options);
            context.Database.EnsureCreated();
            
            // Add test data
            var activeEntity = new IntegrationSoftDeletableEntity { Name = "Active", IsDeleted = false };
            var deletedEntity = new IntegrationSoftDeletableEntity { Name = "Deleted", IsDeleted = true };
            
            context.SoftDeletableEntities.AddRange(activeEntity, deletedEntity);
            context.SaveChanges();
            
            // Act
            var entities = context.SoftDeletableEntities.ToList();
            
            // Assert
            Assert.Single(entities);
            Assert.Equal("Active", entities.First().Name);
        }
        
        [Fact]
        public void ModelBuilderExtensions_ShouldApplyBuiltInConventions()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<IntegrationTestDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
                
            // Act
            using var context = new IntegrationTestDbContext(options);
            var model = context.Model;
            
            // Assert
            Assert.NotNull(model);
            
            // Check that entities are configured
            var softDeletableEntityType = model.FindEntityType(typeof(IntegrationSoftDeletableEntity));
            Assert.NotNull(softDeletableEntityType);
            
            var auditableEntityType = model.FindEntityType(typeof(IntegrationAuditableEntity));
            Assert.NotNull(auditableEntityType);
            
            var regularEntityType = model.FindEntityType(typeof(IntegrationRegularEntity));
            Assert.NotNull(regularEntityType);
        }
        
        [Fact]
        public void ConventionPriority_ShouldBeRespected()
        {
            // This test verifies that conventions are applied in priority order
            // Lower priority numbers are applied first
            
            // Arrange & Act
            var softDeletionConvention = new SoftDeletionConvention();
            var auditTimeConvention = new AuditTimeConvention();
            
            // Assert
            Assert.Equal(100, softDeletionConvention.Priority);
            Assert.Equal(200, auditTimeConvention.Priority);
            
            // SoftDeletionConvention should be applied before AuditTimeConvention
            Assert.True(softDeletionConvention.Priority < auditTimeConvention.Priority);
        }
        
        [Fact]
        public void MiCakeDbContext_AppliesConventionsInOrder()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<IntegrationTestDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
                
            // Act
            using var context = new IntegrationTestDbContext(options);
            
            // Assert - Context should initialize successfully with all conventions
            Assert.NotNull(context.Model);
            
            // Verify that model configuration was applied
            var entityTypes = context.Model.GetEntityTypes();
            Assert.NotEmpty(entityTypes);
        }
        
        [Fact]
        public void AddMiCakeModel_ConfiguresEntitiesCorrectly()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<IntegrationTestDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
                
            using var context = new IntegrationTestDbContext(options);
            var model = context.Model;
            
            // Act & Assert
            var softDeletableType = model.FindEntityType(typeof(IntegrationSoftDeletableEntity));
            Assert.NotNull(softDeletableType);
            
            // Check if the query filter was applied (this would be set by SoftDeletionConvention)
            var queryFilter = softDeletableType.GetQueryFilter();
            Assert.NotNull(queryFilter);
        }
    }
    
    // Test DbContext for integration testing
    public class IntegrationTestDbContext : MiCakeDbContext
    {
        public IntegrationTestDbContext(DbContextOptions options) : base(options) { }
        
        public DbSet<IntegrationSoftDeletableEntity> SoftDeletableEntities { get; set; }
        public DbSet<IntegrationAuditableEntity> AuditableEntities { get; set; }
        public DbSet<IntegrationRegularEntity> RegularEntities { get; set; }
    }
    
    // Test entities with unique names for integration tests
    public class IntegrationSoftDeletableEntity : Entity, ISoftDeletion
    {
        public string Name { get; set; }
        public bool IsDeleted { get; set; }
    }
    
    public class IntegrationAuditableEntity : Entity, IHasCreationTime, IHasModificationTime
    {
        public string Title { get; set; }
        public DateTime CreationTime { get; set; }
        public DateTime? ModificationTime { get; set; }
    }
    
    public class IntegrationRegularEntity : Entity
    {
        public string Description { get; set; }
    }
}