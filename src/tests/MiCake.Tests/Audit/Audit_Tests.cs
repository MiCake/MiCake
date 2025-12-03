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

            executor.Execute(entity, RepositoryEntityState.Added);

            Assert.Equal(default, entity.CreatedAt);
        }

        [Fact]
        public void AuditCreationTime_AddedState()
        {
            var provider = BuildServicesWithAuditProvider().BuildServiceProvider();

            HasCreationTimeModel entity = new();
            var executor = provider.GetService<IAuditExecutor>();

            var beforeGiveTime = DateTime.UtcNow;
            executor.Execute(entity, RepositoryEntityState.Added);

            var result = entity.CreatedAt >= beforeGiveTime;
            Assert.True(result);
        }

        [Fact]
        public void AuditCreationTime_OhterState()
        {
            var provider = BuildServicesWithAuditProvider().BuildServiceProvider();

            HasCreationTimeModel entity = new();
            var executor = provider.GetService<IAuditExecutor>();

            executor.Execute(entity, RepositoryEntityState.Deleted);
            executor.Execute(entity, RepositoryEntityState.Modified);
            executor.Execute(entity, RepositoryEntityState.Unchanged);

            Assert.Equal(default, entity.CreatedAt);
        }

        [Fact]
        public void AuditModificationTime_ModifiedState()
        {
            var provider = BuildServicesWithAuditProvider().BuildServiceProvider();

            HasModificationTimeModel entity = new();
            var executor = provider.GetService<IAuditExecutor>();

            var beforeGiveTime = DateTime.UtcNow;
            executor.Execute(entity, RepositoryEntityState.Modified);

            var result = entity.UpdatedAt >= beforeGiveTime;
            Assert.True(result);
        }

        [Fact]
        public void AuditModificationTime_OtherState()
        {
            var provider = BuildServicesWithAuditProvider().BuildServiceProvider();

            HasModificationTimeModel entity = new();
            var executor = provider.GetService<IAuditExecutor>();

            executor.Execute(entity, RepositoryEntityState.Added);
            executor.Execute(entity, RepositoryEntityState.Deleted);
            executor.Execute(entity, RepositoryEntityState.Unchanged);

            Assert.Null(entity.UpdatedAt);
        }

        [Fact]
        public void AuditObject_AddedThenModification()
        {
            var provider = BuildServicesWithAuditProvider().BuildServiceProvider();

            HasAuditModel entity = new();
            var executor = provider.GetService<IAuditExecutor>();

            var beforeGiveCreationTime = DateTime.UtcNow;
            executor.Execute(entity, RepositoryEntityState.Added);

            var result = entity.CreatedAt >= beforeGiveCreationTime;
            Assert.True(result);
            Assert.Null(entity.UpdatedAt);

            var beforeGiveModificationTime = DateTime.UtcNow;
            executor.Execute(entity, RepositoryEntityState.Modified);

            var modificationResult = entity.UpdatedAt.Value >= beforeGiveModificationTime;
            Assert.True(modificationResult);
        }

        [Fact]
        public void AuditCreation_NotDomainObject()
        {
            var provider = BuildServicesWithAuditProvider().BuildServiceProvider();

            HasCreationTimeButNotEntity entity = new();
            var executor = provider.GetService<IAuditExecutor>();

            executor.Execute(entity, RepositoryEntityState.Added);

            Assert.Equal(default, entity.CreatedAt);
        }

        [Fact]
        public void SoftDeletion_DeletedState()
        {
            var provider = BuildServicesWithAuditProvider().BuildServiceProvider();

            SoftDeletionModel entity = new();
            var executor = provider.GetService<IAuditExecutor>();

            executor.Execute(entity, RepositoryEntityState.Deleted);

            Assert.True(entity.IsDeleted);
        }

        [Fact]
        public void SoftDeletion_OtherStated()
        {
            var provider = BuildServicesWithAuditProvider().BuildServiceProvider();

            SoftDeletionModel entity = new();
            var executor = provider.GetService<IAuditExecutor>();

            executor.Execute(entity, RepositoryEntityState.Added);
            executor.Execute(entity, RepositoryEntityState.Modified);
            executor.Execute(entity, RepositoryEntityState.Unchanged);

            Assert.False(entity.IsDeleted);
        }

        [Fact]
        public void HasDeletionTime_DeletedStated()
        {
            var provider = BuildServicesWithAuditProvider().BuildServiceProvider();

            HasDeletionTimeModel entity = new();
            var executor = provider.GetService<IAuditExecutor>();

            var beforeGiveDeletionTime = DateTime.UtcNow;
            executor.Execute(entity, RepositoryEntityState.Deleted);

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

            executor.Execute(entity, RepositoryEntityState.Added);
            executor.Execute(entity, RepositoryEntityState.Modified);
            executor.Execute(entity, RepositoryEntityState.Unchanged);

            Assert.False(entity.IsDeleted);
            Assert.Null(entity.DeletedAt);
        }

        [Fact]
        public void HasDeletionTime_ButNoSoftDeletion()
        {
            var provider = BuildServicesWithAuditProvider().BuildServiceProvider();

            HasDeletionTimeModel_NoSoftDeletion entity = new();
            var executor = provider.GetService<IAuditExecutor>();

            executor.Execute(entity, RepositoryEntityState.Deleted);

            Assert.Null(entity.DeletedAt);
        }

        [Fact]
        public void DefaultTimeAuditProvider_WithCustomTimeProvider_ShouldUseCustomTime()
        {
            // Arrange
            var fixedTime = new DateTime(2025, 1, 1, 12, 0, 0);
            var originalProvider = DefaultTimeAuditProvider.CurrentTimeProvider;

            try
            {
                DefaultTimeAuditProvider.CurrentTimeProvider = () => fixedTime;
                var provider = BuildServicesWithAuditProvider().BuildServiceProvider();
                var executor = provider.GetService<IAuditExecutor>();
                var entity = new HasCreationTimeModel();

                // Act
                executor.Execute(entity, RepositoryEntityState.Added);

                // Assert
                Assert.Equal(fixedTime, entity.CreatedAt);
            }
            finally
            {
                // Restore original provider
                DefaultTimeAuditProvider.CurrentTimeProvider = originalProvider;
            }
        }

        [Fact]
        public void DefaultTimeAuditProvider_WithCustomTimeProvider_ShouldAffectModificationTime()
        {
            // Arrange
            var fixedTime = new DateTime(2025, 6, 15, 10, 30, 0);
            var originalProvider = DefaultTimeAuditProvider.CurrentTimeProvider;

            try
            {
                DefaultTimeAuditProvider.CurrentTimeProvider = () => fixedTime;
                var provider = BuildServicesWithAuditProvider().BuildServiceProvider();
                var executor = provider.GetService<IAuditExecutor>();
                var entity = new HasModificationTimeModel();

                // Act
                executor.Execute(entity, RepositoryEntityState.Modified);

                // Assert
                Assert.Equal(fixedTime, entity.UpdatedAt);
            }
            finally
            {
                // Restore original provider
                DefaultTimeAuditProvider.CurrentTimeProvider = originalProvider;
            }
        }

        [Fact]
        public void DefaultTimeAuditProvider_AfterResettingProvider_ShouldUseNewProvider()
        {
            // Arrange
            var firstTime = new DateTime(2025, 1, 1);
            var secondTime = new DateTime(2025, 12, 31);
            var originalProvider = DefaultTimeAuditProvider.CurrentTimeProvider;

            try
            {
                // Set first time provider
                DefaultTimeAuditProvider.CurrentTimeProvider = () => firstTime;
                var provider = BuildServicesWithAuditProvider().BuildServiceProvider();
                var executor = provider.GetService<IAuditExecutor>();
                var entity1 = new HasCreationTimeModel();
                executor.Execute(entity1, RepositoryEntityState.Added);

                // Change time provider
                DefaultTimeAuditProvider.CurrentTimeProvider = () => secondTime;
                var entity2 = new HasCreationTimeModel();
                executor.Execute(entity2, RepositoryEntityState.Added);

                // Assert
                Assert.Equal(firstTime, entity1.CreatedAt);
                Assert.Equal(secondTime, entity2.CreatedAt);
            }
            finally
            {
                // Restore original provider
                DefaultTimeAuditProvider.CurrentTimeProvider = originalProvider;
            }
        }

        [Fact]
        public void DefaultTimeAuditProvider_WithDefaultProvider_ShouldUseCurrentTime()
        {
            // Arrange - ensure using default provider
            var originalProvider = DefaultTimeAuditProvider.CurrentTimeProvider;
            try
            {
                DefaultTimeAuditProvider.CurrentTimeProvider = () => DateTime.Now;
                var provider = BuildServicesWithAuditProvider().BuildServiceProvider();
                var executor = provider.GetService<IAuditExecutor>();
                var entity = new HasAuditModel();

                var beforeTime = DateTime.Now;

                // Act
                executor.Execute(entity, RepositoryEntityState.Added);

                var afterTime = DateTime.Now;

                // Assert
                Assert.True(entity.CreatedAt >= beforeTime);
                Assert.True(entity.CreatedAt <= afterTime);
            }
            finally
            {
                DefaultTimeAuditProvider.CurrentTimeProvider = originalProvider;
            }
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
