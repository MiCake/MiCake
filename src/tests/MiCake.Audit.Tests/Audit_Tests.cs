using MiCake.Audit.Core;
using MiCake.Audit.SoftDeletion;
using MiCake.Audit.Tests.Fakes;
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

            executor.Execute(entity, DDD.Extensions.RepositoryEntityState.Added);

            Assert.Equal(default, entity.CreationTime);
        }

        [Fact]
        public void AuditCreationTime_AddedState()
        {
            var provider = BuildServicesWithAuditProvider().BuildServiceProvider();

            HasCreationTimeModel entity = new();
            var executor = provider.GetService<IAuditExecutor>();

            var beforeGiveTime = DateTime.Now;
            executor.Execute(entity, DDD.Extensions.RepositoryEntityState.Added);

            var result = entity.CreationTime >= beforeGiveTime;
            Assert.True(result);
        }

        [Fact]
        public void AuditCreationTime_OhterState()
        {
            var provider = BuildServicesWithAuditProvider().BuildServiceProvider();

            HasCreationTimeModel entity = new();
            var executor = provider.GetService<IAuditExecutor>();

            executor.Execute(entity, DDD.Extensions.RepositoryEntityState.Deleted);
            executor.Execute(entity, DDD.Extensions.RepositoryEntityState.Modified);
            executor.Execute(entity, DDD.Extensions.RepositoryEntityState.Unchanged);

            Assert.Equal(default, entity.CreationTime);
        }

        [Fact]
        public void AuditModificationTime_ModifiedState()
        {
            var provider = BuildServicesWithAuditProvider().BuildServiceProvider();

            HasModificationTimeModel entity = new();
            var executor = provider.GetService<IAuditExecutor>();

            var beforeGiveTime = DateTime.Now;
            executor.Execute(entity, DDD.Extensions.RepositoryEntityState.Modified);

            var result = entity.ModificationTime >= beforeGiveTime;
            Assert.True(result);
        }

        [Fact]
        public void AuditModificationTime_OtherState()
        {
            var provider = BuildServicesWithAuditProvider().BuildServiceProvider();

            HasModificationTimeModel entity = new();
            var executor = provider.GetService<IAuditExecutor>();

            executor.Execute(entity, DDD.Extensions.RepositoryEntityState.Added);
            executor.Execute(entity, DDD.Extensions.RepositoryEntityState.Deleted);
            executor.Execute(entity, DDD.Extensions.RepositoryEntityState.Unchanged);

            Assert.Null(entity.ModificationTime);
        }

        [Fact]
        public void AuditObject_AddedThenModification()
        {
            var provider = BuildServicesWithAuditProvider().BuildServiceProvider();

            HasAuditModel entity = new();
            var executor = provider.GetService<IAuditExecutor>();

            var beforeGiveCreationTime = DateTime.Now;
            executor.Execute(entity, DDD.Extensions.RepositoryEntityState.Added);

            var result = entity.CreationTime >= beforeGiveCreationTime;
            Assert.True(result);
            Assert.Null(entity.ModificationTime);

            var beforeGiveModificationTime = DateTime.Now;
            executor.Execute(entity, DDD.Extensions.RepositoryEntityState.Modified);

            var modificationResult = entity.ModificationTime.Value >= beforeGiveModificationTime;
            Assert.True(modificationResult);
        }

        [Fact]
        public void AuditCreation_NotDomainObject()
        {
            var provider = BuildServicesWithAuditProvider().BuildServiceProvider();

            HasCreationTimeButNotEntity entity = new();
            var executor = provider.GetService<IAuditExecutor>();

            executor.Execute(entity, DDD.Extensions.RepositoryEntityState.Added);

            Assert.Equal(default, entity.CreationTime);
        }

        [Fact]
        public void SoftDeletion_DeletedState()
        {
            var provider = BuildServicesWithAuditProvider().BuildServiceProvider();

            SoftDeletionModel entity = new();
            var executor = provider.GetService<IAuditExecutor>();

            executor.Execute(entity, DDD.Extensions.RepositoryEntityState.Deleted);

            Assert.True(entity.IsDeleted);
        }

        [Fact]
        public void SoftDeletion_OtherStated()
        {
            var provider = BuildServicesWithAuditProvider().BuildServiceProvider();

            SoftDeletionModel entity = new();
            var executor = provider.GetService<IAuditExecutor>();

            executor.Execute(entity, DDD.Extensions.RepositoryEntityState.Added);
            executor.Execute(entity, DDD.Extensions.RepositoryEntityState.Modified);
            executor.Execute(entity, DDD.Extensions.RepositoryEntityState.Unchanged);

            Assert.False(entity.IsDeleted);
        }

        [Fact]
        public void HasDeletionTime_DeletedStated()
        {
            var provider = BuildServicesWithAuditProvider().BuildServiceProvider();

            HasDeletionTimeModel entity = new();
            var executor = provider.GetService<IAuditExecutor>();

            var beforeGiveDeletionTime = DateTime.Now;
            executor.Execute(entity, DDD.Extensions.RepositoryEntityState.Deleted);

            var result = entity.DeletionTime.Value >= beforeGiveDeletionTime;

            Assert.True(entity.IsDeleted);
            Assert.True(result);
        }

        [Fact]
        public void HasDeletionTime_OtherStated()
        {
            var provider = BuildServicesWithAuditProvider().BuildServiceProvider();

            HasDeletionTimeModel entity = new();
            var executor = provider.GetService<IAuditExecutor>();

            executor.Execute(entity, DDD.Extensions.RepositoryEntityState.Added);
            executor.Execute(entity, DDD.Extensions.RepositoryEntityState.Modified);
            executor.Execute(entity, DDD.Extensions.RepositoryEntityState.Unchanged);

            Assert.False(entity.IsDeleted);
            Assert.Null(entity.DeletionTime);
        }

        [Fact]
        public void HasDeletionTime_ButNoSoftDeletion()
        {
            var provider = BuildServicesWithAuditProvider().BuildServiceProvider();

            HasDeletionTimeModel_NoSoftDeletion entity = new();
            var executor = provider.GetService<IAuditExecutor>();

            executor.Execute(entity, DDD.Extensions.RepositoryEntityState.Deleted);

            Assert.Null(entity.DeletionTime);
        }

        private IServiceCollection BuildServices()
        {
            var services = new ServiceCollection();
            //Audit Executor
            services.AddScoped<IAuditExecutor, DefaultAuditExecutor>();

            return services;
        }

        private IServiceCollection BuildServicesWithAuditProvider()
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
