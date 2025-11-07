using Microsoft.EntityFrameworkCore;

namespace MiCake.EntityFrameworkCore
{
    /// <summary>
    /// Base DbContext class with MiCake features pre-configured.
    /// Follows MiCake's lightweight and non-intrusive design principles.
    /// You can inherit from this class, or use the extension methods in your own DbContext.
    /// </summary>
    public class MiCakeDbContext : DbContext
    {
        /// <summary>
        /// Primary constructor for MiCake DbContext.
        /// No longer requires IServiceProvider dependency.
        /// </summary>
        /// <param name="options">The DbContext options</param>
        public MiCakeDbContext(DbContextOptions options) : base(options)
        {
        }

        /// <summary>
        /// Protected parameterless constructor for derived classes
        /// </summary>
        protected MiCakeDbContext() : base()
        {
        }

        /// <summary>
        /// Configure model conventions and apply MiCake DDD patterns.
        /// This method automatically applies entity configurations for DDD entities.
        /// </summary>
        /// <param name="modelBuilder">The model builder instance</param>
        /// <remarks>
        /// Call base.OnModelCreating() when overriding to ensure MiCake conventions are applied.
        /// </remarks>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            modelBuilder.UseMiCakeConventions();
        }

        /// <summary>
        /// Configure DbContext options including MiCake interceptors.
        /// This method automatically adds domain event handling interceptors.
        /// </summary>
        /// <param name="optionsBuilder">The options builder instance</param>
        /// <remarks>
        /// Call base.OnConfiguring() when overriding to ensure MiCake interceptors are added.
        /// </remarks>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            
            optionsBuilder.UseMiCakeInterceptors();
        }
    }
}
