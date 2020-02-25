using System;

namespace MiCake.Core.Modularity
{
    /// <summary>
    /// Micake module boot.use to initialization module and shutdown module.
    /// </summary>
    public interface IMiCakeModuleBoot
    {
        void ConfigServices(ModuleConfigServiceContext context, Action<ModuleConfigServiceContext> otherPartConfigServicesAction = null);

        void Initialization(ModuleBearingContext context, Action<ModuleBearingContext> otherPartInitAction = null);

        void ShutDown(ModuleBearingContext context);
    }
}
