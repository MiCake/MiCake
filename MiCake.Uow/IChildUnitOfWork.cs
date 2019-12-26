using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.Uow
{
    internal interface IChildUnitOfWork : IUnitOfWork
    {
        IUnitOfWork GetParentUnitOfWork();
    }
}
