namespace MiCake.SqlReader
{
    public abstract class BaseSqlDataProvider : ISqlDataProvider
    {
        public Dictionary<string, Dictionary<string, SqlValue>> SqlValueData { get; protected set; } = new Dictionary<string, Dictionary<string, SqlValue>>();

        public SqlValue? Get(string sqlKey)
        {
            return Get<SqlValue>(sqlKey);
        }

        public SqlValue? Get(string sqlKey, string sectionName)
        {
            return GetData(sqlKey, sectionName);
        }

        public T? Get<T>(string sqlKey) where T : SqlValue
        {
            foreach (var sectionName in SqlValueData.Keys)
            {
                var currentSectionResult = Get<T>(sqlKey, sectionName);
                if (currentSectionResult != null)
                    return currentSectionResult;
            }

            return null;
        }

        public T? Get<T>(string sqlKey, string sectionName) where T : SqlValue
        {
            return Get(sqlKey, sectionName) as T;
        }

        public bool HasSqlValue(string sqlKey)
        {
            return Get(sqlKey) != null;
        }

        private SqlValue? GetData(string sqlKey, string sectionName)
        {
            if (!SqlValueData.TryGetValue(sectionName, out var sectionData))
                return null;

            if (!sectionData.TryGetValue(sqlKey, out var result))
                return null;

            return result;
        }

        public abstract void PopulateSql();
        public abstract void Dispose();
    }
}
