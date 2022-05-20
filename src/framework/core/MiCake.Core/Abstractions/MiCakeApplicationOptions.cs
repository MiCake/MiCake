using MiCake.Core.Data;

namespace MiCake.Core
{
    /// <summary>
    /// The configuration of building the core program of micake
    /// </summary>
    public class MiCakeApplicationOptions : ICanApplyData<MiCakeApplicationOptions>
    {
        /// <summary>
        /// Specifies a custom module sorter that can change the startup order of MiCake application modules.
        /// </summary>
        public IMiCakeModuleSorter? ModuleSorter { get; set; }

        /// <summary>
        /// Use given option value.
        /// </summary>
        /// <param name="applicationOptions"></param>
        public void Apply(MiCakeApplicationOptions applicationOptions)
        {
            ModuleSorter = applicationOptions.ModuleSorter;
        }
    }
}
