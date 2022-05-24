using MiCake.Cord.Modules;
using MiCake.Core.Modularity;
using MiCake.EntityFrameworkCore.Internal;
using MiCake.Uow.Modules;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MiCake.EntityFrameworkCore.Uow;
using MiCake.Uow;
using Microsoft.Extensions.DependencyInjection;

namespace MiCake.EntityFrameworkCore.Modules
{
    [RelyOn(typeof(MiCakeUowModule), typeof(MiCakeDDDModule))]
    [CoreModule]
    public class MiCakeEFCoreModule : MiCakeModule
    {
        public MiCakeEFCoreModule()
        {
        }

        public override void PreConfigServices(ModuleConfigServiceContext context)
        {
            var services = context.Services!;

            services.TryAddTransient<IEFSaveChangesLifetime, DefaultEFSaveChangesLifetime>();

            //add uow TransactionProvider of EFCore.
            services.AddTransient<ITransactionProvider, EFCoreTransactionProvider>();
        }
    }
}
