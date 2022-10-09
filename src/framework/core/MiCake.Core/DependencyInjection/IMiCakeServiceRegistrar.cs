namespace MiCake.Core.DependencyInjection
{
    /// <summary>
    /// Manage the services that need to be injected automatically in MiCake
    /// </summary>
    internal interface IMiCakeServiceRegistrar
    {
        void Register(IMiCakeModuleCollection miCakeModules);

        /// <summary>
        /// Set user-defined service registration type lookup rules
        /// </summary>
        /// <param name="findAutoServiceTypes"><see cref="FindAutoServiceTypesDelegate"/></param>
        void SetServiceTypesFinder(FindAutoServiceTypesDelegate findAutoServiceTypes);
    }
}
