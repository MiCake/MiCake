using System;

namespace MiCake.Uow
{
    public interface IUnitOfWorkProvider
    {
        IUnitOfWork GetCurrentUnitOfWork();

        IUnitOfWork GetUnitOfWork(Guid Id);
    }
}
