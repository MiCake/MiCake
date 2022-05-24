using MiCake.Core.Util;
using MiCake.Uow;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MiCake.EntityFrameworkCore.Uow
{
    internal class EFCoreTransactionProvider : ITransactionProvider
    {
        public int Order => 100;

        private readonly Type _dbContextType;
        private readonly DbContext _dbContext;

        public EFCoreTransactionProvider(IServiceProvider service, IOptions<EFCoreDbContextTypeAccessor> efCoreDbTypeAccessor)
        {
            Type? dbContextType = efCoreDbTypeAccessor.Value?.DbContextType;
            CheckValue.NotNull(dbContextType, "DbContext Type");

            var dbContext = service.GetService(dbContextType!);
            CheckValue.NotNull(dbContext, nameof(dbContext), $"Can not resolve {dbContextType!.Name}");

            _dbContextType = dbContextType;
            _dbContext = (dbContext as DbContext)!;
        }

        public ITransactionObject GetTransactionObject()
        {
            return new EFCoreTransactionObject(_dbContext, _dbContextType);
        }
    }
}
