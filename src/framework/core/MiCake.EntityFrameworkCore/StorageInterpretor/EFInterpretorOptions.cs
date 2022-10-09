using MiCake.Core.Util.Collections;

namespace MiCake.EntityFrameworkCore.StorageInterpretor
{
    internal class EFInterpretorOptions
    {
        public List<IConfigModelBuilderStrategy> Strategies { get; } = new List<IConfigModelBuilderStrategy>();

        public EFInterpretorOptions()
        {
        }

        public void AddStrategy(IConfigModelBuilderStrategy strategy) => Strategies.AddIfNotContains(strategy);
    }
}
