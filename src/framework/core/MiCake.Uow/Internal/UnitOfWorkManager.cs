using Microsoft.Extensions.DependencyInjection;

namespace MiCake.Uow.Internal
{
    internal class UnitOfWorkManager : IUnitOfWorkManager
    {
        private readonly IServiceProvider _currentProvider;

        private bool _isDisposed = false;

        private IUnitOfWorkNode? _currentUOW;

        public UnitOfWorkManager(IServiceProvider serivceProvider)
        {
            _currentProvider = serivceProvider;
        }

        public IUnitOfWork Create()
        {
            return CreateNewUnitOfWork(_currentProvider, new());
        }

        public IUnitOfWork Create(UnitOfWorkCreateType createType, UnitOfWorkOptions unitOfWorkOptions)
        {
            throw new NotImplementedException();
        }

        public IUnitOfWork CreateCore(UnitOfWorkCreateType createType, UnitOfWorkOptions unitOfWorkOptions)
        {
            throw new NotImplementedException();
        }

        void CoreUowNodeDisposeHanlder(IUnitOfWorkNode node)
        {
            _currentUOW = node.Parent;
        }

        public virtual IUnitOfWork? GetCurrentUnitOfWork()
        {
            return _currentUOW as IUnitOfWork;
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;

            //It's always be null?
            (_currentUOW as UnitOfWork)?.Dispose();
        }

        //Create a new unit of work with options. 
        private IUnitOfWork CreateNewUnitOfWork(IServiceProvider serviceProvider, UnitOfWorkOptions options)
        {
            UnitOfWork result;
            result = serviceProvider.GetRequiredService<UnitOfWork>();

            if (result == _currentUOW)
            {
                return result;
            }

            result.UnitOfWorkOptions = options;
            result.SetData(CoreUowNodeDisposeHanlder);

            // keep node link.
            result.Parent = _currentUOW;
            _currentUOW = result;

            return result;
        }
    }
}
