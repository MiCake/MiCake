using Autofac.Extensions.DependencyInjection;
using MiCake.Core.Modularity;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace MiCake.Autofac
{
    public class MiCakeAutofacModule : MiCakeModule, IFeatureModule
    {
        public override bool IsFrameworkLevel => true;

        public FeatureModuleLoadOrder Order { get; set; }
        public bool AutoRegisted { get; set; }

        public MiCakeAutofacModule()
        {
        }

        public override void PreConfigServices(ModuleConfigServiceContext context)
        {
            context.Services.AddSingleton<IAutofacLocator, AutofacLocator>(provider =>
             {
                 AutofacLocator.Instance.Locator = provider.GetAutofacRoot();
                 return AutofacLocator.Instance;
             });
        }

        public override void PreInitialization(ModuleBearingContext context)
        {
            if (!(context.ServiceProvider is AutofacServiceProvider))
                throw new ArgumentException("It is detected that you are trying to use aufofac in micake, but you are not registered in host.");

            context.ServiceProvider.GetRequiredService<IAutofacLocator>();
        }
    }
}
