using MiCake.Cord;
using Microsoft.EntityFrameworkCore;

namespace MiCake.EntityFrameworkCore.Internal
{
    internal static class EntityStateExtension
    {
        public static RepositoryEntityState ToRepositoryState(this EntityState entityState)
        {
            return entityState switch
            {
                EntityState.Unchanged => RepositoryEntityState.Unchanged,
                EntityState.Deleted => RepositoryEntityState.Deleted,
                EntityState.Modified => RepositoryEntityState.Modified,
                EntityState.Added => RepositoryEntityState.Added,
                _ => RepositoryEntityState.Unchanged
            };
        }

        public static EntityState ToEFState(this RepositoryEntityState entityState)
        {
            return entityState switch
            {
                RepositoryEntityState.Unchanged => EntityState.Unchanged,
                RepositoryEntityState.Deleted => EntityState.Deleted,
                RepositoryEntityState.Modified => EntityState.Modified,
                RepositoryEntityState.Added => EntityState.Added,
                _ => EntityState.Unchanged
            };
        }
    }
}
