using Dapper;
using System.Data;
using System.Text.Json;

namespace MiCake.Dapper.TypeHandler
{
    /// <summary>
    /// A dapper type handler for covert json to object.
    /// </summary>
    public class DapperJsonTypeHandler : SqlMapper.ITypeHandler
    {
        public object Parse(Type destinationType, object value)
        {
            return JsonSerializer.Deserialize(value!.ToString()!, destinationType)!;
        }

        public void SetValue(IDbDataParameter parameter, object value)
        {
            parameter.Value = (value == null) ? (object)DBNull.Value : JsonSerializer.Serialize(value);
            parameter.DbType = DbType.String;
        }
    }
}
