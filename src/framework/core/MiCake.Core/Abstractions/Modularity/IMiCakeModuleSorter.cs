namespace MiCake.Core.Modularity
{
    public interface IMiCakeModuleSorter
    {
        /// <summary>
        /// Custom sort for ordered modules
        /// </summary>
        /// <param name="ordinalModules"></param>
        /// <returns></returns>
        IMiCakeModuleCollection Sort(IMiCakeModuleCollection ordinalModules);
    }
}
