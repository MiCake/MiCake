using BaseMiCakeApplication.Infrastructure.StroageModels;
using MiCake.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BaseMiCakeApplication.EFCore
{
    public class BaseAppDbContext : MiCakeDbContext
    {
        public BaseAppDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<ItinerarySnapshotModel> Itinerary { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}
