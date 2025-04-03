namespace MiCake.Core.Modularity
{
    public interface IMiCakeModuleContext
    {
        /// <summary>
        /// MiCake Module Collection for normal modules
        /// </summary>
        IMiCakeModuleCollection MiCakeModules { get; }
    }
}
