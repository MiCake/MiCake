using MiCake.DDD.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace MiCake.EntityFrameworkCore
{
    internal static class EntityStateExtension
    {
        public static RepositoryEntityStates ToRepositoryState(this EntityState entityState)
        {
            return entityState switch
            {
                EntityState.Unchanged => RepositoryEntityStates.Unchanged,
                EntityState.Deleted => RepositoryEntityStates.Deleted,
                EntityState.Modified => RepositoryEntityStates.Modified,
                EntityState.Added => RepositoryEntityStates.Added,
                _ => RepositoryEntityStates.Unchanged
            };
        }

        public static EntityState ToEFState(this RepositoryEntityStates entityState)
        {
            return entityState switch
            {
                RepositoryEntityStates.Unchanged => EntityState.Unchanged,
                RepositoryEntityStates.Deleted => EntityState.Deleted,
                RepositoryEntityStates.Modified => EntityState.Modified,
                RepositoryEntityStates.Added => EntityState.Added,
                _ => EntityState.Unchanged
            };
        }
    }
}
