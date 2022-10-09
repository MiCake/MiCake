using Microsoft.Extensions.DependencyInjection;

namespace MiCake.DDD.CQS
{
    internal class QueryDispatcher : IQueryDispatcher
    {
        private readonly IServiceProvider _handerFactory;

        public QueryDispatcher(IServiceProvider serviceProvider)
        {
            _handerFactory = serviceProvider;
        }

        public virtual Task<TResult> Dispatch<TQuery, TResult>(TQuery queryModel, CancellationToken cancellationToken = default) where TQuery : IQueryModel
        {
            var handlerInstance = _handerFactory.GetService<IQueryHandler<TQuery, TResult>>() ?? throw new InvalidOperationException($"No handler found for query type {typeof(TQuery).Name}");

            return handlerInstance.Handle(queryModel, cancellationToken);
        }
    }
}
