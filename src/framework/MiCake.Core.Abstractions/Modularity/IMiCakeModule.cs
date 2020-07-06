namespace MiCake.Core.Modularity
{
    /// <summary>
    /// Define a MiCake module info.
    /// </summary>
    public interface IMiCakeModule : IModuleConfigServicesLifetime, IModuleLifetime
    {
        /// <summary>
        /// Tag this module is farmework level.
        /// Framework level modules do not need to be traversed.
        /// </summary>
        public bool IsFrameworkLevel { get; }

        /// <summary>
        /// Auto register service to dependency injection framework.
        /// </summary>
        public bool IsAutoRegisterServices { get; }
    }
}
