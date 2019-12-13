using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.Uow.Easy
{
    public interface IUnitOfWokrProvider
    {
        IUnitOfWork GetCurrentUnitOfWork();
    }
}
