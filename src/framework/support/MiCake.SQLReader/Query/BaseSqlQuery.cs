using MiCake.Core.Util;

namespace MiCake.SqlReader.Query
{
    /// <summary>
    /// A base query class use <see cref="ISqlReader"/> to read SQL.
    /// 
    /// <para>
    ///     You can implement this class to read data from a database by execute SQL,
    /// </para>
    /// </summary>
    public abstract class BaseSqlQuery : IDisposable
    {
        public ISqlReader SqlReader { get; }

        public abstract string CurrentSectionName { get; }

        public BaseSqlQuery(ISqlReader sqlReader)
        {
            SqlReader = sqlReader;
        }

        protected virtual string? GetSql(string sqlKey, string sectionName)
        {
            CheckValue.NotNull(SqlReader, nameof(SqlReader));

            return SqlReader!.Get(sqlKey, sectionName)?.CommandText;
        }

        protected virtual string? GetSql(string sqlKey)
        {
            CheckValue.NotNull(SqlReader, nameof(SqlReader));

            return SqlReader!.Get(sqlKey, CurrentSectionName)?.CommandText;
        }

        public abstract void Dispose();
    }
}
