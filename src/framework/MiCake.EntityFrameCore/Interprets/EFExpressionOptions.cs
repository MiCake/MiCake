using MiCake.Core.Util.Collections;
using System.Collections.Generic;

namespace MiCake.EntityFrameworkCore.Interprets
{
    internal class EFExpressionOptions
    {
        public List<IConfigModelBuilderStrategy> Strategies { get; } = new List<IConfigModelBuilderStrategy>();

        public EFExpressionOptions()
        {
        }

        public void AddStrategy(IConfigModelBuilderStrategy strategy)
            => Strategies.AddIfNotContains(strategy);
    }
}
