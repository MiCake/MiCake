namespace MiCake.DDD.CQS
{
    /// <summary>
    /// Indicate a dispatcher that dispatch a <see cref="ICommandModel"/> to a <see cref="ICommandHandler{T}"/>
    /// </summary>
    public interface ICommandDispatcher
    {
        /// <summary>
        /// Dispatch an <see cref="ICommandModel"/> to its corresponding <see cref="ICommandHandler{T}"/>
        /// </summary>
        /// <typeparam name="TCommand"></typeparam>
        /// <param name="command"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task Dispatch<TCommand>(TCommand command, CancellationToken cancellationToken = default) where TCommand : ICommandModel;

        /// <summary>
        /// Dispatch an <see cref="ICommandModel"/> to its corresponding <see cref="ICommandHandler{T}"/>
        /// </summary>
        /// <typeparam name="TCommand"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="command"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<TResult> Dispatch<TCommand, TResult>(TCommand command, CancellationToken cancellationToken = default) where TCommand : ICommandModel;
    }
}
