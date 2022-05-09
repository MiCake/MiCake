namespace MiCake.Uow
{
    public interface IUnitOfWorkInterceptor
    {
    }

    /// <summary>
    /// A unit of work interceptor when called after uow completed.
    /// </summary>
    public interface IUnitOfWorkCompletedInterceptor : IUnitOfWorkInterceptor
    {
        Task Completed(IUnitOfWork context, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// A unit of work interceptor when called after uow robbacked.
    /// </summary>
    public interface IUnitOfWorkRollbackedInterceptor : IUnitOfWorkInterceptor
    {
        Task Rollbacked(IUnitOfWork context, CancellationToken cancellationToken = default);
    }
}
