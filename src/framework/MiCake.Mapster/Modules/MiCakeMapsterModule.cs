using MiCake.Core.Modularity;

namespace MiCake.Mapster.Modules
{
    /// <summary>
    /// Mapster is a very good object mapping framework, so we use it in micake to achieve object transformation
    /// </summary>
    public class MiCakeMapsterModule : MiCakeModule
    {
        public override bool IsFrameworkLevel => true;
    }
}
