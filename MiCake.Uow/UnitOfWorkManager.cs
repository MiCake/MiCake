using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.Uow
{
    internal class UnitOfWorkManager : IUnitOfWorkManager
    {
        private IServiceProvider _serviceProvider;

        // Storage location for the previous current unit of work.
        private IUnitOfWork _savedCurrent;

        public UnitOfWorkManager(IServiceProvider serviceProvider)
        {
        }

        public IUnitOfWork Create(UnitOfWorkOptions options)
        {
            throw new NotImplementedException();
        }

        public IUnitOfWork GetCurrentUnitOfWork()
        {
            throw new NotImplementedException();
        }

        public IUnitOfWork GetUnitOfWork(Guid Id)
        {
            throw new NotImplementedException();
        }
    }
}
