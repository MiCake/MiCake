namespace MiCake.SqlReader
{
    internal class DefaultSqlReader : ISqlReader
    {
        private readonly ISqlDataManager _manager;

        public DefaultSqlReader(ISqlDataManager manager)
        {
            _manager = manager;
        }

        public SqlValue? Get(string sqlKey)
        {
            return Get<SqlValue>(sqlKey);
        }

        public SqlValue? Get(string sqlKey, string sectionName)
        {
            return Get<SqlValue>(sqlKey, sectionName);
        }

        public T? Get<T>(string sqlKey) where T : SqlValue
        {
            foreach (var provider in _manager.DataProviders)
            {
                var result = provider.Get<T>(sqlKey);

                if (result != null)
                    return result;
            }

            return null;
        }

        public T? Get<T>(string sqlKey, string sectionName) where T : SqlValue
        {
            foreach (var provider in _manager.DataProviders)
            {
                var result = provider.Get<T>(sqlKey, sectionName);

                if (result != null)
                    return result;
            }

            return null;
        }

        public bool HasSqlValue(string sqlKey)
        {
            foreach (var provider in _manager.DataProviders)
            {
                var result = provider.HasSqlValue(sqlKey);

                if (result)
                    return result;
            }

            return false;
        }
    }
}
