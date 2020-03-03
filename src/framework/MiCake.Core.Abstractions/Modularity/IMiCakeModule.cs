namespace MiCake.Core.Modularity
{
    public interface IMiCakeModule
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

        void Initialization(ModuleBearingContext context);

        void Shuntdown(ModuleBearingContext context);
    }
}
