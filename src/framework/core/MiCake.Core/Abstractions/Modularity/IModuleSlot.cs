namespace MiCake.Core.Modularity
{
    public interface IModuleSlot
    {
        /// <summary>
        /// Insert the module into MiCake's module system, which will be started later
        /// </summary>
        /// <param name="moduleType"></param>
        void Slot(Type moduleType);

        /// <summary>
        /// Insert the module into MiCake's module system, which will be started later
        /// </summary>
        /// <typeparam name="TModule"></typeparam>
        void Slot<TModule>() where TModule : IMiCakeModule;
    }
}
