using BaseMiCakeApplication.Infrastructure.StroageModel;
using JetBrains.Annotations;
using MiCake.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;

namespace BaseMiCakeApplication.EFCore
{
    public class BaseAppDbContext : DbContext
    {
        public BaseAppDbContext([NotNull] DbContextOptions options) : base(options)
        {
        }

        public DbSet<ItinerarySnapshotModel> Itinerary { get; set; }


        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
        }
    }
}
