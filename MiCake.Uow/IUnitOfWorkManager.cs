using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.Uow
{
    public interface IUnitOfWorkManager : IUnitOfWorkProvider
    {
        IUnitOfWork Create(UnitOfWorkOptions options);
    }
}
