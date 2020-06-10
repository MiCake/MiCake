using MiCake.Audit.Core;
using MiCake.Core.Util.Reflection;
using MiCake.Identity.Audit;
using MiCake.Identity.Tests.FakeUser;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace MiCake.Identity.Tests.Audit
{
    public class IdentityAuditProvider_Tests
    {
        public IdentityAuditProvider_Tests()
        {
        }


        [Fact]
        public void AuditUser_withMoreAudit()
        {
            HasAuditUser user = new HasAuditUser()
            {
                Id = 1001
            };

            var serivces = BuildServicesWithAuditProvider(user);
            var auditProvider = serivces.BuildServiceProvider().GetService<IAuditExecutor>();

        }


        private IServiceCollection BuildServicesWithAuditProvider<TUser>(TUser user)
            where TUser : IMiCakeUser
        {
            var services = new ServiceCollection();
            //Audit Executor
            services.AddScoped<IAuditExecutor, DefaultAuditExecutor>();

            var userKeyType = TypeHelper.GetGenericArguments(user.GetType(), typeof(IMiCakeUser<>));
            if (userKeyType == null || userKeyType[0] == null)
                throw new ArgumentException($"Can not get the primary key type of IMiCakeUser,Please check your config when AddIdentity().");

            var auditProviderType = typeof(IdentityAuditProvider<>).MakeGenericType(userKeyType[0]);
            services.AddScoped(typeof(IAuditProvider), auditProviderType);

            return services;
        }
    }
}
