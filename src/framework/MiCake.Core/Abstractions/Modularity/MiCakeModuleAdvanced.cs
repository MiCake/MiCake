namespace MiCake.Core.Modularity
{

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
    public abstract class MiCakeModuleAdvanced : MiCakeModule, IMiCakeModuleAdvanced
    {
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public virtual void PreConfigureServices(ModuleConfigServiceContext context)
        {
            // Default implementation - no pre-configuration needed
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public virtual void PostConfigureServices(ModuleConfigServiceContext context)
        {
            // Default implementation - no post-configuration needed
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public virtual void PreInitialization(ModuleInitializationContext context)
        {
            // Default implementation - no pre-initialization needed
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public virtual void PostInitialization(ModuleInitializationContext context)
        {
            // Default implementation - no post-initialization needed
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public virtual void PreShutdown(ModuleShutdownContext context)
        {
            // Default implementation - no pre-shutdown needed
        }
    }
}
