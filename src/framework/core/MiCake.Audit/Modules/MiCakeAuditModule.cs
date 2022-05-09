using MiCake.Audit.Core;
using MiCake.Audit.Lifetime;
using MiCake.Audit.SoftDeletion;
using MiCake.Audit.Store;
using MiCake.Core.Modularity;
using MiCake.DDD.Connector.Lifetime;
using MiCake.DDD.Connector.Modules;
using MiCake.DDD.Connector.Store.Configure;
using Microsoft.Extensions.DependencyInjection;

namespace MiCake.Audit.Modules
{
    [RelyOn(typeof(MiCakeDDDModule))]
    public class MiCakeAuditModule : MiCakeModule
    {
        public override bool IsFrameworkLevel => true;

        public override void PreConfigServices(ModuleConfigServiceContext context)
        {
            StoreConfig.Instance.AddModelProvider(new SoftDeletionStoreEntityConfig());
        }

        public override void ConfigServices(ModuleConfigServiceContext context)
        {
            var auditOptions = (MiCakeAuditOptions)context.MiCakeApplicationOptions.AdditionalInfo.TakeOut(MiCakeBuilderAuditCoreExtension.AuditForApplicationOptionsKey);
            var services = context.Services;

            if (auditOptions?.UseAudit != false)
            {
                //Audit Executor
                services.AddScoped<IAuditExecutor, DefaultAuditExecutor>();
                //Audit CreationTime and ModifationTime
                services.AddScoped<IAuditProvider, DefaultTimeAuditProvider>();
                //RepositoryLifeTime
                services.AddTransient<IRepositoryPreSaveChanges, AuditRepositoryLifetime>();

                if (auditOptions?.UseSoftDeletion != false)
                {
                    //Audit Deletion Time
                    services.AddScoped<IAuditProvider, SoftDeletionAuditProvider>();
                    //RepositoryLifeTime
                    services.AddTransient<IRepositoryPreSaveChanges, SoftDeletionRepositoryLifetime>();
                }
            }
        }
    }
}
