using MiCake.Audit.Tests.Fakes;
using MiCake.Identity.User;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.Audit.Tests
{
    public class AuditTestBase
    {
        protected IServiceProvider ServiceProvider { get; private set; }

        public AuditTestBase()
        {
            ServiceProvider = BuildSerivce();
        }

        protected virtual IServiceProvider BuildSerivce()
        {
            IServiceCollection services = new ServiceCollection();
            services.AddScoped<IAuditContext, AuditContext>();

            RegisterCurrentUser<TestCurrentUser>(services);

            return services.BuildServiceProvider();
        }

        protected virtual void RegisterCurrentUser<TUserType>(IServiceCollection services)
           where TUserType : class, ICurrentUser, ISetAuditPropertyAbility
        {
            var currentOptions = new CurrentUserOptions() { CurrentUserType = typeof(TUserType) };
            services.Replace(
                new ServiceDescriptor(typeof(IOptions<CurrentUserOptions>), currentOptions));

            var currentUserType = typeof(ICurrentUser<>).MakeGenericType(currentOptions.UserKeyType);
            services.Replace(
                new ServiceDescriptor(currentUserType, typeof(TUserType), ServiceLifetime.Transient));

            services.AddTransient<ISetAuditPropertyAbility, TUserType>();
        }
    }
}
