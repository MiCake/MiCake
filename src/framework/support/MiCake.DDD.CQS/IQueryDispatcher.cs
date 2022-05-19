namespace MiCake.DDD.CQS
{
    /// <summary>
    /// Indicate a dispatcher that dispatch a <see cref="IQueryModel"/> to a <see cref="IQueryHandler{TQuery, TResult}"/>
    /// </summary>
    public interface IQueryDispatcher
    {
        /// <summary>
        /// Dispatch an <see cref="IQueryModel"/> to its corresponding <see cref="IQueryHandler{TQuery, TResult}"/>
        /// </summary>
        /// <typeparam name="TQuery"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="queryModel"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<TResult> Dispatch<TQuery, TResult>(TQuery queryModel, CancellationToken cancellationToken = default) where TQuery : IQueryModel;
    }
}
