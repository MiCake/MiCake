using MiCake.Audit.Conventions;
using MiCake.EntityFrameworkCore.Options;

namespace MiCake.EntityFrameworkCore
{
    /// <summary>
    /// Extension methods for configuring MiCake EF Core conventions
    /// </summary>
    public static class ConventionBuilderExtensions
    {
        /// <summary>
        /// Enable soft deletion convention
        /// </summary>
        public static MiCakeEFCoreConventionOptions UseSoftDeletion(this MiCakeEFCoreConventionOptions options)
        {
            if (!options.HasConvention<SoftDeletionConvention>())
            {
                options.AddConvention(new SoftDeletionConvention());
            }
            return options;
        }
        
        /// <summary>
        /// Enable audit time convention
        /// </summary>
        public static MiCakeEFCoreConventionOptions UseAuditTime(this MiCakeEFCoreConventionOptions options)
        {
            if (!options.HasConvention<AuditTimeConvention>())
            {
                options.AddConvention(new AuditTimeConvention());
            }
            return options;
        }
        
        /// <summary>
        /// Disable soft deletion convention
        /// </summary>
        public static MiCakeEFCoreConventionOptions DisableSoftDeletion(this MiCakeEFCoreConventionOptions options)
        {
            options.RemoveConvention<SoftDeletionConvention>();
            return options;
        }
        
        /// <summary>
        /// Disable audit time convention
        /// </summary>
        public static MiCakeEFCoreConventionOptions DisableAuditTime(this MiCakeEFCoreConventionOptions options)
        {
            options.RemoveConvention<AuditTimeConvention>();
            return options;
        }
    }
}