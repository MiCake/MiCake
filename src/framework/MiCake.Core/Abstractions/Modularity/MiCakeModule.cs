namespace MiCake.Core.Modularity
{
    /// <summary>
    /// Base class for MiCake modules - Provides default implementation.
    /// <para>
    /// All MiCake modules should inherit from this base class.
    /// For fine-grained lifecycle control, inherit from <see cref="MiCakeModuleAdvanced"/>.
    /// </para>
    /// </summary>
    public abstract class MiCakeModule : IMiCakeModule
    {
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public virtual bool IsFrameworkLevel => false;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public virtual bool EnableAutoServiceRegistration => true;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public virtual string Description => string.Empty;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public virtual void ConfigureServices(ModuleConfigServiceContext context)
        {
            // Default implementation - no services to configure
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public virtual void OnApplicationInitialization(ModuleInitializationContext context)
        {
            // Default implementation - no initialization needed
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public virtual void OnApplicationShutdown(ModuleShutdownContext context)
        {
            // Default implementation - no cleanup needed
        }
    }
}
