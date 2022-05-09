namespace MiCake.EntityFrameworkCore
{
    /// <summary>
    /// The options of EFCore extension for MiCake.
    /// </summary>
    public class MiCakeEFCoreOptions
    {
        /// <summary>
        /// Type of <see cref="MiCakeDbContext"/>.
        /// </summary>
        public Type? DbContextType { get; set; }
    }
}
