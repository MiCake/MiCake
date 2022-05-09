using MiCake.DDD.Domain;

namespace MiCake.DDD.Connector.Internal
{
    internal class DefaultRepositoryFacotry<TAggregateRoot, TKey> : IRepositoryFactory<TAggregateRoot, TKey>
         where TAggregateRoot : class, IAggregateRoot<TKey>
    {
        private readonly IRepositoryProvider<TAggregateRoot, TKey> _repositoryProvider;

        public DefaultRepositoryFacotry(
            IEnumerable<IRepositoryProvider<TAggregateRoot, TKey>> repositoryProvider
            )
        {
            //if has more repository provider(ef core,dapper,ado).always choose last one.
            _repositoryProvider = repositoryProvider.Last();
        }

        public IReadOnlyRepository<TAggregateRoot, TKey> CreateReadOnlyRepository()
        {
            return _repositoryProvider.GetReadOnlyRepository();
        }

        public IRepository<TAggregateRoot, TKey> CreateRepository()
        {
            return _repositoryProvider.GetRepository();
        }

    }
}
