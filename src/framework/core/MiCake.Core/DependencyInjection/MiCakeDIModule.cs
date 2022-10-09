using MiCake.Core.Data;

namespace MiCake.Core.DependencyInjection
{
    /// <summary>
    /// A core module to achieve auto dependency-injection.
    /// </summary>
    [CoreModule]
    public class MiCakeDIModule : MiCakeModule
    {
        public const string AutoDIConfigKey = "MiCake.TransientData.DIModule.Config";

        public override void PreConfigServices(ModuleConfigServiceContext context)
        {
            base.PreConfigServices(context);

            var customerConfig = (context.MiCakeApplication as IHasAccessor<MiCakeTransientData>)?.GetAccessor()?.TakeOut(AutoDIConfigKey);
            MiCakeDIOptions? dIOptions = (MiCakeDIOptions?)customerConfig;

            // auto register services.
            var serviceRegistrar = new DefaultServiceRegistrar(context.Services);
            if (dIOptions?.FindAutoServiceTypes != null)
                serviceRegistrar.SetServiceTypesFinder(dIOptions.FindAutoServiceTypes);

            serviceRegistrar.Register(context.MiCakeModules);
        }
    }
}
