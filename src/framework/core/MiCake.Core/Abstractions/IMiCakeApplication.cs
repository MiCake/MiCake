namespace MiCake.Core
{
    /// <summary>
    /// Represents a micake application.
    /// </summary>
    public interface IMiCakeApplication : IDisposable
    {
        /// <summary>
        /// <see cref="MiCakeApplicationOptions"/>
        /// </summary>
        public MiCakeApplicationOptions ApplicationOptions { get; }

        /// <summary>
        /// <see cref="IMiCakeModuleManager"/>
        /// </summary>
        IMiCakeModuleManager ModuleManager { get; }

        /// <summary>
        /// Set entry module to start micake.
        /// </summary>
        /// <param name="type"></param>
        void SetEntry(Type type);

        /// <summary>
        /// Use to build micake modules and trigger config services lifetime.
        /// </summary>
        void Initialize();

        /// <summary>
        /// Start micake appliction.
        /// </summary>
        void Start();

        /// <summary>
        /// Used to gracefully shutdown the application and all modules.
        /// </summary>
        void ShutDown();
    }
}
