namespace MiCake.SqlReader
{
    /// <summary>
    /// A Reader that read SQL from loaded data
    /// </summary>
    public interface ISqlReader
    {
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
