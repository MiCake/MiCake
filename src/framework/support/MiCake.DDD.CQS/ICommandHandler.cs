namespace MiCake.DDD.CQS
{
    /// <summary>
    /// Indicate a handler to handler a <see cref="ICommandModel"/>
    /// </summary>
    public interface ICommandHandler<TCommand> where TCommand : ICommandModel
    {
        /// <summary>
        /// Handle a <see cref="IQueryModel"/> to query data from data store.
        /// </summary>
        /// <param name="command"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task Handle(TCommand command, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Indicate a handler to handler a <see cref="ICommandModel"/>
    /// </summary>
    public interface ICommandHandler<TCommand, TResult> : ICommandHandler<TCommand> where TCommand : ICommandModel
    {

        /// <summary>
        /// Handle a <see cref="IQueryModel"/> to query data from data store.
        /// </summary>
        /// <param name="command"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        new Task<TResult> Handle(TCommand command, CancellationToken cancellationToken = default);
    }
}
