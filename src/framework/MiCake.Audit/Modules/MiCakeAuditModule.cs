using MiCake.Audit.Core;
using MiCake.Audit.LifeTime;
using MiCake.Audit.SoftDeletion;
using MiCake.Audit.Store;
using MiCake.Core.Modularity;
using MiCake.DDD.Extensions.LifeTime;
using MiCake.DDD.Extensions.Store.Configure;
using Microsoft.Extensions.DependencyInjection;

namespace MiCake.Audit.Modules
{
    public class MiCakeAuditModule : MiCakeModule
    {
        public override bool IsFrameworkLevel => true;

        public override void PreConfigServices(ModuleConfigServiceContext context)
        {
            StoreConfig.Instance.AddModelProvider(new SoftDeletionStoreEntityConfig());
        }

        public override void ConfigServices(ModuleConfigServiceContext context)
        {
            var services = context.Services;

            //Audit Executor
            services.AddScoped<IAuditExecutor, DefaultAuditExecutor>();
            //Audit CreationTime and ModifationTime
            services.AddScoped<IAuditProvider, DefaultTimeAuditProvider>();
            //Audit Deletion Time
            services.AddScoped<IAuditProvider, SoftDeletionAuditProvider>();

            //RepositoryLifeTime
            services.AddTransient<IRepositoryPreSaveChanges, AuditRepositoryLifetime>();
            services.AddTransient<IRepositoryPreSaveChanges, SoftDeletionRepositoryLifetime>();
        }
    }
}
