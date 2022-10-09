using MiCake.Core.Util;

namespace MiCake.SqlReader
{
    internal class SqlDataManager : ISqlDataManager
    {
        public SqlDataManager()
        {
            _providers = new List<ISqlDataProvider>();
        }

        private readonly List<ISqlDataProvider> _providers;
        public IEnumerable<ISqlDataProvider> DataProviders => _providers;

        public void AddSqlDataProvider(ISqlDataProvider dataProvider)
        {
            CheckValue.NotNull(dataProvider, nameof(dataProvider));

            _providers.Add(dataProvider);
        }

        public void PopulateSql()
        {
            foreach (var provider in _providers)
            {
                provider.PopulateSql();
            }
        }
    }
}
