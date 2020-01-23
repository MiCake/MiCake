using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.EntityFrameworkCore.Uow
{
    internal interface IUowDbContextFactory<TDbCotnext>
        where TDbCotnext : DbContext
    {
        TDbCotnext CreateDbContext();
    }
}
