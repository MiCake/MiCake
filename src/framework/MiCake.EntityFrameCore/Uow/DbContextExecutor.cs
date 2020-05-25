using MiCake.Uow;
using Microsoft.EntityFrameworkCore;
using System;

namespace MiCake.EntityFrameworkCore.Uow
{
    internal class DbContextExecutor<TDbContext> : DbExecutor<TDbContext>
          where TDbContext : DbContext
    {
        public DbContextExecutor(TDbContext dbContxt)
        {

        }

        protected override bool SetTransaction(ITransactionObject transaction)
        {
            throw new NotImplementedException();
        }
    }
}
