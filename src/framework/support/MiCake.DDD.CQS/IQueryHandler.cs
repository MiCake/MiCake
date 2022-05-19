namespace MiCake.DDD.CQS
{
    /// <summary>
    /// Indicate a handler to handle a <see cref="IQueryModel"/>
    /// </summary>
    public interface IQueryHandler<TQuery, TResult> where TQuery : IQueryModel
    {
        /// <summary>
        /// Handle a <see cref="IQueryModel"/> to query data from data store.
        /// </summary>
        /// <param name="queryModel"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<TResult> Handle(TQuery queryModel, CancellationToken cancellationToken = default);
    }
}
