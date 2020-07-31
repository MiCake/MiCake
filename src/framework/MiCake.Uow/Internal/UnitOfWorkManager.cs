using MiCake.Core.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;

namespace MiCake.Uow.Internal
{
    /// <summary>
    /// Set the DI level to scoped in order to automatically release resources on each HTTP request.
    /// </summary>
    internal class UnitOfWorkManager : IUnitOfWorkManager
    {
        /// <summary>
        /// The ServiceProvider use to create ServiceScope for each of unit of work.
        /// </summary>
        private IServiceProvider _serviceProvider;

        /// <summary>
        /// When no configuration item is specified, this default configuration will be used.
        /// </summary>
        private UnitOfWorkOptions _defaultOptions;

        /// <summary>
        /// Used to save existing units of work as stack structure.
        /// </summary>
        private UnitOfWorkCallContext _callContext = new UnitOfWorkCallContext();

        private bool _isDisposed = false;

        //Only for test.
        internal UnitOfWorkCallContext CallContext => _callContext;

        public UnitOfWorkManager(IServiceProvider serviceProvider, IOptions<UnitOfWorkOptions> defaultOptions)
        {
            _serviceProvider = serviceProvider;
            _defaultOptions = defaultOptions.Value;
        }

        public virtual IUnitOfWork Create()
        {
            return Create(_defaultOptions.Clone());
        }

        public virtual IUnitOfWork Create(UnitOfWorkScope unitOfWorkScope)
        {
            var options = _defaultOptions.Clone();
            options.Scope = unitOfWorkScope;

            return Create(options);
        }

        public virtual IUnitOfWork Create(UnitOfWorkOptions options)
        {
            IUnitOfWork resultUow;

            if (NeedCreateNewUnitOfWork(options))
            {
                resultUow = CreateNewUnitOfWork(options);
            }
            else
            {
                //create child unit of work ,it will use the configuration of the previous unit of work
                resultUow = new ChildUnitOfWork(_callContext.GetCurrentUow());

                Action<IUnitOfWork> handler = context =>
                {
                    _callContext.PopUnitOfWork();
                };

                UnitOfWorkNeedParts uowNeedParts = new UnitOfWorkNeedParts() { Options = options, DisposeHandler = handler };
                (resultUow as INeedParts<UnitOfWorkNeedParts>)?.SetParts(uowNeedParts);
            }
            _callContext.PushUnitOfWork(resultUow);

            return resultUow;
        }

        public virtual IUnitOfWork GetCurrentUnitOfWork()
        {
            return _callContext.GetCurrentUow();
        }

        public virtual IUnitOfWork GetUnitOfWork(Guid Id)
        {
            return _callContext.GetUowByID(Id);
        }

        public virtual void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;

            //It's always be null?
            var currentUow = _callContext.GetCurrentUow();
            while (currentUow != null)
            {
                currentUow.Dispose();

                currentUow = _callContext.PopUnitOfWork();
            }
        }

        //Determine whether a new unit of work needs to be created
        private bool NeedCreateNewUnitOfWork(UnitOfWorkOptions options)
        => options.Scope switch
        {
            UnitOfWorkScope.Required => _callContext.GetCurrentUow() == null,
            UnitOfWorkScope.RequiresNew => true,
            UnitOfWorkScope.Suppress => true,
            _ => throw new ArgumentException($"{options.Scope} is not supported.")
        };

        //Create a new unit of work with options. 
        private IUnitOfWork CreateNewUnitOfWork(UnitOfWorkOptions options)
        {
            IUnitOfWork result;

            //Give this scope to unit of work who will be created.
            var uowScope = _serviceProvider.CreateScope();

            try
            {
                //Release resources and update _callContext status through Ondispose event.
                Action<IUnitOfWork> handler = context =>
                {
                    uowScope.Dispose();
                    _callContext.PopUnitOfWork();
                };

                result = uowScope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                UnitOfWorkNeedParts uowNeedParts = new UnitOfWorkNeedParts()
                {
                    Options = options,
                    ServiceScope = uowScope,
                    DisposeHandler = handler
                };
                (result as INeedParts<UnitOfWorkNeedParts>)?.SetParts(uowNeedParts);
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
