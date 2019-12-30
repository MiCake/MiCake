using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.Uow
{
    public interface IUnitOfWorkProvider
    {
        IUnitOfWork GetCurrentUnitOfWork();

        IUnitOfWork GetUnitOfWork(Guid Id);
    }
}
