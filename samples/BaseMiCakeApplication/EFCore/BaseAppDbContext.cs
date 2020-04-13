using BaseMiCakeApplication.Infrastructure.StroageModels;
using Microsoft.EntityFrameworkCore;

namespace BaseMiCakeApplication.EFCore
{
    public class BaseAppDbContext : DbContext
    {
        public BaseAppDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<ItinerarySnapshotModel> Itinerary { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            optionsBuilder.UseMySql("Server=localhost;Database=MiCakeDemo;User=sa;Password={your password};");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}
