using MiCake.Dapper;
using MiCake.SqlReader;
using Microsoft.Extensions.Options;
using Npgsql;
using System.Data;

namespace TodoApp.QueryReader
{
    public abstract class NgSqlQuery : DapperSqlQuery
    {
        public NgSqlQuery(ISqlReader sqlReader, IOptions<MiCakeDapperOptions> options) : base(sqlReader, options)
        {
        }

        public NpgsqlConnection? NgConnection => DbConnection as NpgsqlConnection;

        public override IDbConnection GetDbConnection(string connectionString)
        {
            return new NpgsqlConnection(connectionString);
        }
    }
}
