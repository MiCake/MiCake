using BaseMiCakeApplication.Infrastructure.StroageModel;
using JetBrains.Annotations;
using MiCake.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;

namespace BaseMiCakeApplication.EFCore
{
    public class BaseAppDbContext : MiCakeDbContext
    {
        public BaseAppDbContext([NotNull] DbContextOptions options, IServiceProvider serviceProvider)
            : base(options, serviceProvider)
        {
        }

        public DbSet<ItinerarySnapshotModel> Itinerary { get; set; }


        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
        }
    }
}
