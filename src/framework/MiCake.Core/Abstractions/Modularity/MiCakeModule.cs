using System.Threading.Tasks;

namespace MiCake.Core.Modularity
{
    public abstract class MiCakeModule : IMiCakeModule
    {
        /// <summary>
        /// Tag this module is farmework level.
        /// Framework level modules do not need to be traversed.
        /// </summary>
        public virtual bool IsFrameworkLevel => false;

        /// <summary>
        /// Auto register service to dependency injection framework.
        /// </summary>
        public virtual bool IsAutoRegisterServices => true;

        public MiCakeModule()
        {
        }

        public virtual Task ConfigServices(ModuleConfigServiceContext context)
        {
            return Task.CompletedTask;
        }

        public virtual Task Initialization(ModuleLoadContext context)
        {
            return Task.CompletedTask;
        }

        public virtual Task PostConfigServices(ModuleConfigServiceContext context)
        {
            return Task.CompletedTask;
        }

        public virtual Task PostInitialization(ModuleLoadContext context)
        {
            return Task.CompletedTask;
        }

        public virtual Task PreConfigServices(ModuleConfigServiceContext context)
        {
            return Task.CompletedTask;
        }

        public virtual Task PreInitialization(ModuleLoadContext context)
        {
            return Task.CompletedTask;
        }

        public virtual Task PreShutDown(ModuleLoadContext context)
        {
            return Task.CompletedTask;
        }

        public virtual Task Shutdown(ModuleLoadContext context)
        {
            return Task.CompletedTask;
        }
    }
}
