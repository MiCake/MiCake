using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.Uow.Easy
{
    /// <summary>
    /// 
    /// </summary>
    public interface IUnitOfWorkManager : IUnitOfWokrProvider, IDisposable
    {
        IUnitOfWork Create(UnitOfWorkOptions options);
    }
}
