using MiCake.Core.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;

namespace MiCake.Uow.Internal
{
    /// <summary>
    /// Set the DI level to scoped in order to automatically release resources on each HTTP request.
    /// </summary>
    internal class UnitOfWorkManager(IServiceProvider serviceProvider, IOptions<UnitOfWorkOptions> defaultOptions) : IUnitOfWorkManager
    {
        /// <summary>
        /// The ServiceProvider use to create ServiceScope for each of unit of work.
        /// </summary>
        private readonly IServiceProvider _serviceProvider = serviceProvider;

        /// <summary>
        /// When no configuration item is specified, this default configuration will be used.
        /// </summary>
        private readonly UnitOfWorkOptions _defaultOptions = defaultOptions.Value;

        /// <summary>
        /// Used to save existing units of work as stack structure.
        /// </summary>
        private readonly UnitOfWorkCallContext _callContext = new();
        internal UnitOfWorkCallContext CallContext => _callContext;

        private bool _isDisposed = false;

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

                void handler(IUnitOfWork context)
                {
                    _callContext.PopUnitOfWork();
                }

                UnitOfWorkNeedParts uowNeedParts = new() { Options = options, DisposeHandler = handler };
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
            var uowScope = options.ServiceScope ?? _serviceProvider.CreateScope();
            var autoDispose = options.ServiceScope == null;
            try
            {
                //Release resources and update _callContext status through Ondispose event.
                void handler(IUnitOfWork context)
                {
                    _callContext.PopUnitOfWork();

                    if (autoDispose)
                        uowScope.Dispose();
                }

                result = uowScope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                UnitOfWorkNeedParts uowNeedParts = new()
                {
                    Options = options,
                    ServiceScope = uowScope,
                    DisposeHandler = handler
                };
                (result as INeedParts<UnitOfWorkNeedParts>)?.SetParts(uowNeedParts);
            }
            catch (Exception)
            {
                uowScope.Dispose();
                throw;
            }

            return result;
        }
    }
}
