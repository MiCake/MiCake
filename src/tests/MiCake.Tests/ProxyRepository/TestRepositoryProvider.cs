using MiCake.DDD.Domain;
using MiCake.DDD.Extensions;
using System;

namespace MiCake.DDD.Tests.ProxyRepository
{
    public class TestRepositoryProvider<TAggregateRoot, TKey> : IRepositoryProvider<TAggregateRoot, TKey>
         where TAggregateRoot : class, IAggregateRoot<TKey>
    {
        public IReadOnlyRepository<TAggregateRoot, TKey> GetReadOnlyRepository()
        {
            var type = typeof(TestReadOnlyRepository<,>).MakeGenericType(typeof(TAggregateRoot), typeof(TKey));
            var instance = Activator.CreateInstance(type);
            return (IReadOnlyRepository<TAggregateRoot, TKey>)instance;
        }

        public IRepository<TAggregateRoot, TKey> GetRepository()
        {
            var type = typeof(TestRepository<,>).MakeGenericType(typeof(TAggregateRoot), typeof(TKey));
            var instance = Activator.CreateInstance(type);
            return (IRepository<TAggregateRoot, TKey>)instance;
        }
    }
}
