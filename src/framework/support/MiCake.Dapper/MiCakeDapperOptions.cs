using MiCake.Core.Data;

namespace MiCake.Dapper
{
    public class MiCakeDapperOptions : ICanApplyData<MiCakeDapperOptions>
    {
        public string? DbConnectionStr { get; set; }

        public void Apply(MiCakeDapperOptions data)
        {
            DbConnectionStr = data.DbConnectionStr;
        }
    }
}
