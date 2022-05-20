using MiCake.Core.Data;

namespace MiCake.EntityFrameworkCore
{
    /// <summary>
    /// The options of EFCore extension for MiCake.
    /// </summary>
    public class MiCakeEFCoreOptions : ICanApplyData<MiCakeEFCoreOptions>
    {
        /// <summary>
        /// Type of <see cref="MiCakeDbContext"/>.
        /// </summary>
        public Type? DbContextType { get; set; }

        public void Apply(MiCakeEFCoreOptions options)
        {
            DbContextType = options.DbContextType;
        }
    }
}
