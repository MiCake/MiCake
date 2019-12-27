using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.Uow
{
    internal class UnitOfWorkManager : IUnitOfWorkManager
    {
        private IServiceProvider _serviceProvider;
        private UnitOfWorkCallContext _callcontext;

        public UnitOfWorkManager(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
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

        //Create a new unitofwork 
        private IUnitOfWork CreateNewUnitOfWork(UnitOfWorkOptions options)
        {
            IUnitOfWork result;

            var uowScope = _serviceProvider.CreateScope();

            try
            {
                result = uowScope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                if (options != null)
                    result.SetOptions(options);

                (result as IUnitOfWorkHook).DisposeHandler += (sender, args) =>
                {
                    uowScope.Dispose();
                };
            }
            catch (Exception ex)
            {
                uowScope.Dispose();
                throw ex;
            }

            return result;
        }
    }
}
