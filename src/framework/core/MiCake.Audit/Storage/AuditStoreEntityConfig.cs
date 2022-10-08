using MiCake.Audit.SoftDeletion;
using MiCake.Cord.Storage;

namespace MiCake.Audit.Storage
{
    internal class AuditStoreEntityConfig : IStoreModelProvider
    {
        private readonly MiCakeAuditOptions _otpions;

        public AuditStoreEntityConfig(MiCakeAuditOptions options)
        {
            if (options.UseSqlToGenerateTime && string.IsNullOrEmpty(options.SqlForGenerateTime))
            {
                throw new InvalidOperationException($"When you want to use MiCake Audit and the {nameof(options.UseSqlToGenerateTime)} is true, you must assign {nameof(options.SqlForGenerateTime)} vaule in {nameof(MiCakeAuditOptions)}");
            }

            _otpions = options;
        }

        public void Config(StoreModelBuilder modelBuilder)
        {
            // use soft deletion.
            modelBuilder.Entity<ISoftDeletion>().DirectDeletion(false);
            modelBuilder.Entity<ISoftDeletion>().HasQueryFilter(s => !s.IsDeleted);

            if (_otpions.UseSqlToGenerateTime)
            {
                // create time and modify time
                modelBuilder.Entity<IHasCreatedTime>().Property(s => s.CreatedTime).DefaultValue(_otpions.SqlForGenerateTime!, StorePropertyDefaultValueType.SqlValue, StorePropertyDefaultValueSetOpportunity.Add);

                // update modification time in EFCore is need provider support.
                // modelBuilder.Entity<IHasModificationTime>().Property(s => s.ModificationTime).DefaultValue(_otpions.TimeGenerateSql!, StorePropertyDefaultValueType.SqlValue, StorePropertyDefaultValueSetOpportunity.Update);
            }
        }
    }
}
