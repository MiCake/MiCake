using MiCake.Audit.Core;
using MiCake.Audit.Tests.Fakes.User;
using MiCake.Cord;
using MiCake.Core.Util.Reflection;
using MiCake.Identity;
using Microsoft.Extensions.DependencyInjection;
using System;
using Xunit;

namespace MiCake.Audit.Tests
{
    public class IdentityAuditProvider_Tests
    {
        public IdentityAuditProvider_Tests()
        {
        }


        [Fact]
        public void AuditUser_hasCreator_hasModifyUser_hasDeleteUser()
        {
            HasAuditUser user = new()
            {
                Id = 1001
            };

            var serivces = BuildServicesWithAuditProvider(user, typeof(FakeCurrentUser_long));
            var auditProvider = serivces.BuildServiceProvider().GetService<IAuditExecutor>();

            Assert.Equal(default, user.CreatedBy);
            auditProvider.Execute(user, RepositoryEntityState.Added);
            Assert.Equal(1001, user.CreatedBy);

            Assert.Equal(default, user.UpdatedBy);
            auditProvider.Execute(user, RepositoryEntityState.Modified);
            Assert.Equal(1001, user.UpdatedBy);

            Assert.Equal(default, user.DeletedBy);
            auditProvider.Execute(user, RepositoryEntityState.Deleted);
            Assert.Equal(1001, user.DeletedBy);
        }

        [Fact]
        public void AuditUser_hasCreator_hasModifyUser_hasDeleteUser_NoSoftDeletion()
        {
            HasAuditUserWithNoSoftDeletion user = new()
            {
                Id = 1001
            };

            var serivces = BuildServicesWithAuditProvider(user, typeof(FakeCurrentUser_long));
            var auditProvider = serivces.BuildServiceProvider().GetService<IAuditExecutor>();

            Assert.Equal(default, user.CreatedBy);
            auditProvider.Execute(user, RepositoryEntityState.Added);
            Assert.Equal(1001, user.CreatedBy);

            Assert.Equal(default, user.UpdatedBy);
            auditProvider.Execute(user, RepositoryEntityState.Modified);
            Assert.Equal(1001, user.UpdatedBy);

            Assert.Equal(default, user.DeletedBy);
            auditProvider.Execute(user, RepositoryEntityState.Deleted);
            Assert.Equal(default, user.DeletedBy);
        }

        [Fact]
        public void AuditUser_WithWrongKeyType()
        {
            HasAuditUserWithWrongKeyType user = new();

            var serivces = BuildServicesWithAuditProvider(user, typeof(FakeCurrentUser_long));
            var auditProvider = serivces.BuildServiceProvider().GetService<IAuditExecutor>();

            Assert.Equal(default, user.CreatedBy);
            auditProvider.Execute(user, RepositoryEntityState.Added);
            Assert.Equal(default, user.CreatedBy);
        }


        private IServiceCollection BuildServicesWithAuditProvider<TUser>(TUser user, Type CurrentUserType)
            where TUser : IMiCakeUser
        {
            var services = new ServiceCollection();
            services.Configure<AuditCoreOptions>(s => { });

            //Audit Executor
            services.AddScoped<IAuditExecutor, DefaultAuditExecutor>();

            var userKeyType = TypeHelper.GetGenericArguments(user.GetType(), typeof(IMiCakeUser<>));
            if (userKeyType == null || userKeyType[0] == null)
                throw new ArgumentException($"Can not get the primary key type of IMiCakeUser,Please check your config when AddIdentity().");

            var auditProviderType = typeof(IdentityAuditProvider<>).MakeGenericType(userKeyType[0]);
            services.AddScoped(typeof(IAuditProvider), auditProviderType);
            services.AddScoped(typeof(ICurrentMiCakeUser), CurrentUserType);

            return services;
        }
    }
}
