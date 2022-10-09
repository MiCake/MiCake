namespace MiCake.Core.Modularity
{
    public abstract class MiCakeModule : IMiCakeModule
    {
        public MiCakeModule()
        {
        }

        public virtual void ConfigServices(ModuleConfigServiceContext context)
        {
        }

        public virtual void Initialization(ModuleLoadContext context)
        {
        }

        public virtual void PostConfigServices(ModuleConfigServiceContext context)
        {
        }

        public virtual void PostInitialization(ModuleLoadContext context)
        {
        }

        public virtual void PreConfigServices(ModuleConfigServiceContext context)
        {
        }

        public virtual void PreInitialization(ModuleLoadContext context)
        {
        }

        public virtual void PreShutDown(ModuleLoadContext context)
        {
        }

        public virtual void Shutdown(ModuleLoadContext context)
        {
        }
    }
}
