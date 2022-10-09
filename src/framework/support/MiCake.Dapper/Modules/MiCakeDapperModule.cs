using MiCake.Core.Modularity;
using MiCake.SqlReader.Modules;

namespace MiCake.Dapper.Modules
{
    [RelyOn(typeof(MiCakeSqlReaderModule))]
    [CoreModule]
    public class MiCakeDapperModule : MiCakeModule
    {
    }
}
