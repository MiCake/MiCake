namespace MiCake.SqlReader
{
    /// <summary>
    /// The manager of all SQL data.
    /// </summary>
    public interface ISqlDataManager
    {
        IEnumerable<ISqlDataProvider> DataProviders { get; }

        /// <summary>
        /// Populate the all SQL file content data into memory.
        /// <para>
        ///     This method can be called multiple times, and each call will re empty the memory data and reload
        /// </para>
        /// </summary>
        void PopulateSql();

        /// <summary>
        /// Add a <see cref="ISqlDataProvider"/>.
        /// </summary>
        /// <param name="dataProvider"></param>
        void AddSqlDataProvider(ISqlDataProvider dataProvider);
    }
}
