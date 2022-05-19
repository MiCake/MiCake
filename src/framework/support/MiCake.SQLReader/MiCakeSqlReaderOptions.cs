using MiCake.Core.Data;
using MiCake.Core.Util;

namespace MiCake.SqlReader
{
    /// <summary>
    /// The options to config MiCake SqlReader.
    /// </summary>
    public class MiCakeSqlReaderOptions : IHasAccessor<IEnumerable<ISqlDataProvider>>
    {
        private readonly List<ISqlDataProvider> sqlDataProviders = new();

        IEnumerable<ISqlDataProvider> IHasAccessor<IEnumerable<ISqlDataProvider>>.AccessibleData => sqlDataProviders;

        public void AddProvider(ISqlDataProvider sqlDataProvider)
        {
            CheckValue.NotNull(sqlDataProvider, nameof(sqlDataProvider));

            sqlDataProviders.Add(sqlDataProvider);
        }
    }
}
