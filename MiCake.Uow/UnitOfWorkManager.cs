using MiCake.Uow.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;

namespace MiCake.Uow
{
    public class UnitOfWorkManager : IUnitOfWorkManager
    {
        private IServiceProvider _serviceProvider;
        private UnitOfWorkCallContext _callcontext;
        private UnitOfWorkDefaultOptions _defaultOptions;

        public UnitOfWorkManager(
            IServiceProvider serviceProvider,
            IOptions<UnitOfWorkDefaultOptions> defaultOptions)
        {
            _serviceProvider = serviceProvider;
            _defaultOptions = defaultOptions.Value;

            _callcontext = new UnitOfWorkCallContext();
        }

        public IUnitOfWork Create()
        {
            return Create(_defaultOptions.ConvertToUnitOfWorkOptions());
        }

        public IUnitOfWork Create(UnitOfWorkOptions options)
        {
            IUnitOfWork resultUow;

            if (NeedCreateNewUnitOfWork(options))
            {
                resultUow = CreateNewUnitOfWork(options);
            }
            else
            {
                resultUow = new ChildUnitOfWork(_callcontext.GetCurrentUow());
                (resultUow as IUnitOfWorkHook).DisposeHandler += (sender, args) =>
                {
                    _callcontext.PopUnitOfWork();
                };
            }
            _callcontext.PushUnitOfWork(resultUow);

            return resultUow;
        }

        public IUnitOfWork GetCurrentUnitOfWork()
        {
            return _callcontext.GetCurrentUow();
        }

        public IUnitOfWork GetUnitOfWork(Guid Id)
        {
            return _callcontext.GetUowByID(Id);
        }

        //Determine whether a new unit of work needs to be created
        private bool NeedCreateNewUnitOfWork(UnitOfWorkOptions options)
        {
            bool result = false;

            switch (options.Limit)
            {
                case UnitOfWorkLimit.Required:
                    result = _callcontext.GetCurrentUow() == null;
                    break;
                case UnitOfWorkLimit.RequiresNew:
                    result = true;
                    break;
                case UnitOfWorkLimit.Suppress:
                    result = true;
                    break;
                default:
                    break;
            }
            return result;
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
                    _callcontext.PopUnitOfWork();
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
