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

        public EFCoreTransactionProvider(IServiceProvider service, IOptions<MiCakeEFCoreOptions> efCoreOptions)
        {
            Type? dbContextType = efCoreOptions.Value?.DbContextType;
            CheckValue.NotNull(dbContextType, "DbContext Type");

            var dbContext = service.GetService(dbContextType!);
            CheckValue.NotNull(dbContext, nameof(dbContext), $"Can not resolve {dbContextType!.Name}");

            _dbContextType = dbContextType;
            _dbContext = (dbContext as DbContext)!;
        }

        public Task<ITransactionObject> GetTransactionObjectAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new EFCoreTransactionObject(_dbContext, _dbContextType) as ITransactionObject);
        }
    }
}
