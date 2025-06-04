namespace MiCake.DDD.Uow.Internal
{
    internal class CurrentUnitOfWork : ICurrentUnitOfWork
    {
        private readonly IUnitOfWorkManager _unitOfWorkManager;

        public CurrentUnitOfWork(IUnitOfWorkManager unitOfWorkManager)
        {
            _unitOfWorkManager = unitOfWorkManager;
        }

        public IUnitOfWork Value => _unitOfWorkManager.GetCurrentUnitOfWork();
    }
}
