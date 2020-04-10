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
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ItinerarySnapshotModel>().Property(s => s.Content).HasMaxLength(100);
            base.OnModelCreating(modelBuilder);
        }
    }
}
