namespace MiCake.SqlReader
{
    /// <summary>
    /// The provider that reads SQL data from the file
    /// </summary>
    public interface ISqlDataProvider : IDisposable
    {
        /// <summary>
        /// Populate the specified SQL file content data into memory.
        /// <para>
        ///     This method can be called multiple times, and each call will re empty the memory data and reload
        /// </para>
        /// </summary>
        void PopulateSql();

        /// <summary>
        /// Indicate the content corresponding to the sqlkey exist
        /// </summary>
        /// <param name="sqlKey"></param>
        /// <returns></returns>
        bool HasSqlValue(string sqlKey);

        /// <summary>
        /// Get the <see cref="SqlValue"/> by sqlkey.
        /// 
        /// <para>
        ///     If the data does not exist, a null value is returned
        /// </para>
        /// </summary>
        /// <param name="sqlKey"></param>
        /// <returns></returns>
        SqlValue? Get(string sqlKey);

        /// <summary>
        /// Get the <see cref="SqlValue"/> by sqlkey.
        /// 
        /// <para>
        ///     If the data does not exist, a null value is returned
        /// </para>
        /// </summary>
        /// <param name="sqlKey"></param>
        /// <param name="sectionName"></param>
        /// <returns></returns>
        SqlValue? Get(string sqlKey, string sectionName);

        /// <summary>
        /// Get the <see cref="SqlValue"/> by sqlkey.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sqlKey"></param>
        /// <returns></returns>
        T? Get<T>(string sqlKey) where T : SqlValue;

        /// <summary>
        /// Get the <see cref="SqlValue"/> by sqlkey.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sqlKey"></param>
        /// <param name="sectionName"></param>
        /// <returns></returns>
        T? Get<T>(string sqlKey, string sectionName) where T : SqlValue;
    }
}
