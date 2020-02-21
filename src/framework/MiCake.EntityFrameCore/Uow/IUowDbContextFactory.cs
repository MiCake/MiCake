using Microsoft.EntityFrameworkCore;

namespace MiCake.EntityFrameworkCore.Uow
{
    internal interface IUowDbContextFactory<TDbCotnext>
        where TDbCotnext : DbContext
    {
        TDbCotnext CreateDbContext();
    }
}
