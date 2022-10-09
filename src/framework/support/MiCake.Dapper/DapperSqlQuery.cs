using MiCake.Core.Util;
using MiCake.SqlReader;
using MiCake.SqlReader.Query;
using Microsoft.Extensions.Options;
using System.Data;

namespace MiCake.Dapper
{
    /// <summary>
    /// A sql query class implement by dapper.
    /// </summary>
    public abstract class DapperSqlQuery : BaseSqlQuery
    {
        public IDbConnection? DbConnection { get; private set; }

        public MiCakeDapperOptions DapperOptions { get; }

        protected DapperSqlQuery(ISqlReader sqlReader, IOptions<MiCakeDapperOptions> options) : base(sqlReader)
        {
            DapperOptions = options.Value;

            CheckValue.NotNullOrEmpty(DapperOptions.DbConnectionStr, nameof(DapperOptions.DbConnectionStr));

            DbConnection = GetDbConnection(DapperOptions.DbConnectionStr!);
        }

        public abstract IDbConnection GetDbConnection(string connectionString);

        public override void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (DbConnection != null)
                {
                    var openState = ConnectionState.Open | ConnectionState.Connecting | ConnectionState.Executing | ConnectionState.Fetching;
                    if (openState.HasFlag(DbConnection.State))
                    {
                        DbConnection.Close();
                    }
                    DbConnection.Dispose();
                    DbConnection = null;
                }
            }
        }
    }
}
