using MiCake.Audit.Core;
using MiCake.Audit.SoftDeletion;
using MiCake.Audit.Tests.Fakes;
using MiCake.DDD.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System;
using Xunit;

namespace MiCake.Audit.Tests
{
    public class Audit_Tests
    {
        public Audit_Tests()
        {

        }

        [Fact]
        public void AuditEntity_WithNoProvider()
        {
            var provider = BuildServices().BuildServiceProvider();

            HasCreationTimeModel entity = new();
            var executor = provider.GetService<IAuditExecutor>();

            executor.Execute(entity, RepositoryEntityStates.Added);

            Assert.Equal(default, entity.CreatedAt);
        }

        [Fact]
        public void AuditCreationTime_AddedState()
        {
            var provider = BuildServicesWithAuditProvider().BuildServiceProvider();

            HasCreationTimeModel entity = new();
            var executor = provider.GetService<IAuditExecutor>();

            var beforeGiveTime = DateTime.UtcNow;
            executor.Execute(entity, RepositoryEntityStates.Added);

            var result = entity.CreatedAt >= beforeGiveTime;
            Assert.True(result);
        }

        [Fact]
        public void AuditCreationTime_OhterState()
        {
            var provider = BuildServicesWithAuditProvider().BuildServiceProvider();

            HasCreationTimeModel entity = new();
            var executor = provider.GetService<IAuditExecutor>();

            executor.Execute(entity, RepositoryEntityStates.Deleted);
            executor.Execute(entity, RepositoryEntityStates.Modified);
            executor.Execute(entity, RepositoryEntityStates.Unchanged);

            Assert.Equal(default, entity.CreatedAt);
        }

        [Fact]
        public void AuditModificationTime_ModifiedState()
        {
            var provider = BuildServicesWithAuditProvider().BuildServiceProvider();

            HasModificationTimeModel entity = new();
            var executor = provider.GetService<IAuditExecutor>();

            var beforeGiveTime = DateTime.UtcNow;
            executor.Execute(entity, RepositoryEntityStates.Modified);

            var result = entity.UpdatedAt >= beforeGiveTime;
            Assert.True(result);
        }

        [Fact]
        public void AuditModificationTime_OtherState()
        {
            var provider = BuildServicesWithAuditProvider().BuildServiceProvider();

            HasModificationTimeModel entity = new();
            var executor = provider.GetService<IAuditExecutor>();

            executor.Execute(entity, RepositoryEntityStates.Added);
            executor.Execute(entity, RepositoryEntityStates.Deleted);
            executor.Execute(entity, RepositoryEntityStates.Unchanged);

            Assert.Null(entity.UpdatedAt);
        }

        [Fact]
        public void AuditObject_AddedThenModification()
        {
            var provider = BuildServicesWithAuditProvider().BuildServiceProvider();

            HasAuditModel entity = new();
            var executor = provider.GetService<IAuditExecutor>();

            var beforeGiveCreationTime = DateTime.UtcNow;
            executor.Execute(entity, RepositoryEntityStates.Added);

            var result = entity.CreatedAt >= beforeGiveCreationTime;
            Assert.True(result);
            Assert.Null(entity.UpdatedAt);

            var beforeGiveModificationTime = DateTime.UtcNow;
            executor.Execute(entity, RepositoryEntityStates.Modified);

            var modificationResult = entity.UpdatedAt.Value >= beforeGiveModificationTime;
            Assert.True(modificationResult);
        }

        [Fact]
        public void AuditCreation_NotDomainObject()
        {
            var provider = BuildServicesWithAuditProvider().BuildServiceProvider();

            HasCreationTimeButNotEntity entity = new();
            var executor = provider.GetService<IAuditExecutor>();

            executor.Execute(entity, RepositoryEntityStates.Added);

            Assert.Equal(default, entity.CreatedAt);
        }

        [Fact]
        public void SoftDeletion_DeletedState()
        {
            var provider = BuildServicesWithAuditProvider().BuildServiceProvider();

            SoftDeletionModel entity = new();
            var executor = provider.GetService<IAuditExecutor>();

            executor.Execute(entity, RepositoryEntityStates.Deleted);

            Assert.True(entity.IsDeleted);
        }

        [Fact]
        public void SoftDeletion_OtherStated()
        {
            var provider = BuildServicesWithAuditProvider().BuildServiceProvider();

            SoftDeletionModel entity = new();
            var executor = provider.GetService<IAuditExecutor>();

            executor.Execute(entity, RepositoryEntityStates.Added);
            executor.Execute(entity, RepositoryEntityStates.Modified);
            executor.Execute(entity, RepositoryEntityStates.Unchanged);

            Assert.False(entity.IsDeleted);
        }

        [Fact]
        public void HasDeletionTime_DeletedStated()
        {
            var provider = BuildServicesWithAuditProvider().BuildServiceProvider();

            HasDeletionTimeModel entity = new();
            var executor = provider.GetService<IAuditExecutor>();

            var beforeGiveDeletionTime = DateTime.UtcNow;
            executor.Execute(entity, RepositoryEntityStates.Deleted);

            var result = entity.DeletedAt.Value >= beforeGiveDeletionTime;

            Assert.True(entity.IsDeleted);
            Assert.True(result);
        }

        [Fact]
        public void HasDeletionTime_OtherStated()
        {
            var provider = BuildServicesWithAuditProvider().BuildServiceProvider();

            HasDeletionTimeModel entity = new();
            var executor = provider.GetService<IAuditExecutor>();

            executor.Execute(entity, RepositoryEntityStates.Added);
            executor.Execute(entity, RepositoryEntityStates.Modified);
            executor.Execute(entity, RepositoryEntityStates.Unchanged);

            Assert.False(entity.IsDeleted);
            Assert.Null(entity.DeletedAt);
        }

        [Fact]
        public void HasDeletionTime_ButNoSoftDeletion()
        {
            var provider = BuildServicesWithAuditProvider().BuildServiceProvider();

            HasDeletionTimeModel_NoSoftDeletion entity = new();
            var executor = provider.GetService<IAuditExecutor>();

            executor.Execute(entity, RepositoryEntityStates.Deleted);

            Assert.Null(entity.DeletedAt);
        }

        [Fact]
        public void DefaultTimeAuditProvider_WithCustomTimeProvider_ShouldUseCustomTime()
        {
            // Arrange
            var fixedTime = new DateTime(2025, 1, 1, 12, 0, 0);
            var fakeTimeProvider = new FakeTimeProvider(new DateTimeOffset(fixedTime));
            var provider = new DefaultTimeAuditProvider(fakeTimeProvider);
            var entity = new HasCreationTimeModel();

            // Act
            provider.ApplyAudit(new AuditOperationContext(entity, RepositoryEntityStates.Added));

            // Assert
            Assert.Equal(fixedTime, entity.CreatedAt);
        }

        [Fact]
        public void DefaultTimeAuditProvider_WithCustomTimeProvider_ShouldAffectModificationTime()
        {
            // Arrange
            var fixedTime = new DateTime(2025, 6, 15, 10, 30, 0);
            var fakeTimeProvider = new FakeTimeProvider(new DateTimeOffset(fixedTime));
            var provider = new DefaultTimeAuditProvider(fakeTimeProvider);
            var entity = new HasModificationTimeModel();

            // Act
            provider.ApplyAudit(new AuditOperationContext(entity, RepositoryEntityStates.Modified));

            // Assert
            Assert.Equal(fixedTime, entity.UpdatedAt);
        }

        [Fact]
        public void DefaultTimeAuditProvider_WithDifferentTimeProviders_ShouldUseDifferentTimes()
        {
            // Arrange
            var firstTime = new DateTime(2025, 1, 1);
            var secondTime = new DateTime(2025, 12, 31);
            
            var firstProvider = new DefaultTimeAuditProvider(new FakeTimeProvider(new DateTimeOffset(firstTime)));
            var secondProvider = new DefaultTimeAuditProvider(new FakeTimeProvider(new DateTimeOffset(secondTime)));
            
            var entity1 = new HasCreationTimeModel();
            var entity2 = new HasCreationTimeModel();

            // Act
            firstProvider.ApplyAudit(new AuditOperationContext(entity1, RepositoryEntityStates.Added));
            secondProvider.ApplyAudit(new AuditOperationContext(entity2, RepositoryEntityStates.Added));

            // Assert
            Assert.Equal(firstTime, entity1.CreatedAt);
            Assert.Equal(secondTime, entity2.CreatedAt);
        }

        [Fact]
        public void DefaultTimeAuditProvider_WithDefaultProvider_ShouldUseCurrentTime()
        {
            // Arrange
            var provider = new DefaultTimeAuditProvider(TimeProvider.System);
            var entity = new HasAuditModel();
            var beforeTime = DateTime.UtcNow;

            // Act
            provider.ApplyAudit(new AuditOperationContext(entity, RepositoryEntityStates.Added));

            var afterTime = DateTime.UtcNow;

            // Assert
            Assert.True(entity.CreatedAt >= beforeTime);
            Assert.True(entity.CreatedAt <= afterTime);
        }

        private static ServiceCollection BuildServices()
        {
            var services = new ServiceCollection();
            //Audit Executor
            services.AddScoped<IAuditExecutor, DefaultAuditExecutor>();

            return services;
        }

        private static ServiceCollection BuildServicesWithAuditProvider()
        {
            var services = new ServiceCollection();
            //Audit Executor
            services.AddScoped<IAuditExecutor, DefaultAuditExecutor>();
            //Audit CreationTime and ModifationTime
            services.AddScoped<IAuditProvider, DefaultTimeAuditProvider>();
            //Audit Deletion Time
            services.AddScoped<IAuditProvider, SoftDeletionAuditProvider>();

            return services;
        }
    }
}
