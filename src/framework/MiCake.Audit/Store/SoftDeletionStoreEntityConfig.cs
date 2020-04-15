using MiCake.DDD.Extensions.Store.Configure;

namespace MiCake.Audit.Store
{
    internal class SoftDeletionStoreEntityConfig : IStoreModelProvider
    {
        public void Config(StoreModelBuilder modelBuilder)
        {
            // use soft deletion.
            modelBuilder.Entity<ISoftDeletion>().DirectDeletion(false);
            modelBuilder.Entity<ISoftDeletion>().HasQueryFilter(s => !s.IsDeleted);
        }
    }
}
