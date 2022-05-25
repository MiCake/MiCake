using MiCake.Audit.SoftDeletion;
using MiCake.Cord.Storage;

namespace MiCake.Audit.Storage
{
    internal class AuditStoreEntityConfig : IStoreModelProvider
    {
        private readonly MiCakeAuditOptions _otpions;

        public AuditStoreEntityConfig(MiCakeAuditOptions options)
        {
            if (string.IsNullOrEmpty(options.TimeGenerateSql))
            {
                throw new InvalidOperationException($"When you want to use MiCake Audit, you must assign {nameof(options.TimeGenerateSql)} vaule in {nameof(MiCakeAuditOptions)}");
            }

            _otpions = options;
        }

        public void Config(StoreModelBuilder modelBuilder)
        {
            // use soft deletion.
            modelBuilder.Entity<ISoftDeletion>().DirectDeletion(false);
            modelBuilder.Entity<ISoftDeletion>().HasQueryFilter(s => !s.IsDeleted);

            // create time and modify time
            modelBuilder.Entity<IHasCreationTime>().Property(s => s.CreationTime).DefaultValue(_otpions.TimeGenerateSql!, StorePropertyDefaultValueType.SqlValue, StorePropertyDefaultValueSetOpportunity.Add);

            // update modification time in EFCore is need provider support.
            // modelBuilder.Entity<IHasModificationTime>().Property(s => s.ModificationTime).DefaultValue(_otpions.TimeGenerateSql!, StorePropertyDefaultValueType.SqlValue, StorePropertyDefaultValueSetOpportunity.Update);
        }
    }
}
