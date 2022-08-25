using MiCake.Audit.Core;
using MiCake.Audit.SoftDeletion;
using MiCake.Audit.Tests.Fakes;
using MiCake.Cord;
using MiCake.Core.Time;
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

            Assert.Equal(default, entity.CreatedTime);
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

            Assert.Equal(default, entity.CreatedTime);
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

            Assert.Null(entity.UpdatedTime);
        }

        [Fact]
        public void AuditCreation_NotDomainObject()
        {
            var provider = BuildServicesWithAuditProvider().BuildServiceProvider();

            HasCreationTimeButNotEntity entity = new();
            var executor = provider.GetService<IAuditExecutor>();

            executor.Execute(entity, RepositoryEntityState.Added);

            Assert.Equal(default, entity.CreatedTime);
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

            var result = entity.DeletedTime.Value >= beforeGiveDeletionTime;

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
            Assert.Null(entity.DeletedTime);
        }

        [Fact]
        public void HasDeletionTime_ButNoSoftDeletion()
        {
            var provider = BuildServicesWithAuditProvider().BuildServiceProvider();

            HasDeletionTimeModel_NoSoftDeletion entity = new();
            var executor = provider.GetService<IAuditExecutor>();

            executor.Execute(entity, RepositoryEntityState.Deleted);

            Assert.Null(entity.DeletedTime);
        }

        private IServiceCollection BuildServices()
        {
            var services = new ServiceCollection();
            services.Configure<AuditCoreOptions>(s => { });
            //Audit Executor
            services.AddScoped<IAuditExecutor, DefaultAuditExecutor>();

            return services;
        }

        private IServiceCollection BuildServicesWithAuditProvider()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IAppClock, AppClock>();
            services.Configure<AuditCoreOptions>(s => { });
            //Audit Executor
            services.AddScoped<IAuditExecutor, DefaultAuditExecutor>();
            //Audit Deletion Time
            services.AddScoped<IAuditProvider, SoftDeletionAuditProvider>();

            return services;
        }

        class AppClock : IAppClock
        {
            public DateTime Now => DateTime.UtcNow;
        }
    }
}
