using MiCake.Audit.Core;
using MiCake.Audit.RepoLifetime;
using MiCake.Audit.SoftDeletion;
using MiCake.Audit.Storage;
using MiCake.Cord.Lifetime;
using MiCake.Cord.Modules;
using MiCake.Core.Data;
using MiCake.Core.Modularity;
using MiCake.Identity.Modules;
using Microsoft.Extensions.DependencyInjection;

namespace MiCake.Audit.Modules
{
    [RelyOn(typeof(MiCakeIdentityModule))]
    [CoreModule]
    public class MiCakeAuditModule : MiCakeModule
    {
        internal const string ConfigTransientDataKey = "MiCake.Audit.Options";

        public override void PreConfigServices(ModuleConfigServiceContext context)
        {
            // config store entity.
            var appTransientData = (context.MiCakeApplication as IHasAccessor<MiCakeTransientData>)?.AccessibleData ?? throw new InvalidOperationException($"Can not get application {nameof(MiCakeTransientData)}.");

            var auditOptions = appTransientData.TakeOut(ConfigTransientDataKey) as MiCakeAuditOptions;

            var storeConfig = DDDModuleHelper.MustGetStoreConfig(context.MiCakeApplication);
            storeConfig.AddModelProvider(new AuditStoreEntityConfig(auditOptions!));


            var services = context.Services;
            // core options
            services.Configure<AuditCoreOptions>(s =>
            {
                s.DateTimeValueProvider = auditOptions?.AuditDateTimeProvider;
                s.AssignCreatedTime = !(auditOptions?.UseSqlToGenerateTime ?? false);
                s.AssignUpdatedTime = true;
            });

            //Audit Executor
            services.AddScoped<IAuditExecutor, DefaultAuditExecutor>();
            services.AddScoped<IAuditProvider, DefaultTimeAuditProvider>();

            //RepositoryLifeTime
            services.AddTransient<IRepositoryPreSaveChanges, AuditRepositoryLifetime>();

            if (auditOptions!.UseSoftDeletion)
            {
                services.AddTransient<IRepositoryPreSaveChanges, SoftDeletionRepositoryLifetime>();
                //Audit Deletion Time
                services.AddScoped<IAuditProvider, SoftDeletionAuditProvider>();
            }

            // register identity audit
            var userKeyType = appTransientData.TakeOut(MiCakeIdentityModule.CurrentIdentityUserKeyType);
            // if no retrieve user key type, It's mean user not AddIdentity, so we needn't register identity audit.
            if (userKeyType != null && userKeyType is Type userKeyClearType)
            {
                Type auditProviderType;
                if (userKeyClearType.IsValueType)
                {
                    auditProviderType = typeof(IdentityAuditProvider<>).MakeGenericType((userKeyType as Type)!);
                }
                else
                {
                    auditProviderType = typeof(IdentityAuditProviderForReferenceKey<>).MakeGenericType((userKeyType as Type)!);
                }

                services.AddScoped(typeof(IAuditProvider), auditProviderType);
            }
        }
    }
}
