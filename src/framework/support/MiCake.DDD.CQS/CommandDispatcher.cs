using Microsoft.Extensions.DependencyInjection;

namespace MiCake.DDD.CQS
{
    internal class CommandDispatcher : ICommandDispatcher
    {
        private readonly IServiceProvider _handerFactory;

        public CommandDispatcher(IServiceProvider serviceProvider)
        {
            _handerFactory = serviceProvider;
        }

        public Task Dispatch<TCommand>(TCommand command, CancellationToken cancellationToken = default) where TCommand : ICommandModel
        {
            var handlerInstance = _handerFactory.GetService<ICommandHandler<TCommand>>() ?? throw new Exception($"Command handler not found for command {typeof(TCommand).FullName}");

            return handlerInstance.Handle(command, cancellationToken);
        }

        public Task<TResult> Dispatch<TCommand, TResult>(TCommand command, CancellationToken cancellationToken = default) where TCommand : ICommandModel
        {
            var handlerInstance = _handerFactory.GetService<ICommandHandler<TCommand>>();

            var hasResultHandler = handlerInstance as ICommandHandler<TCommand, TResult>
                                        ?? throw new InvalidOperationException($"You are going to dispatch a command with a return value, but you do not implement the interface for that.");

            return hasResultHandler.Handle(command, cancellationToken);
        }
    }
}
