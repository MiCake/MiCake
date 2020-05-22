using System;

namespace MiCake.Uow.Internal
{
    internal class CurrentUnitOfWork : ICurrentUnitOfWork
    {
        public IUnitOfWork Value => throw new NotImplementedException();
    }
}
