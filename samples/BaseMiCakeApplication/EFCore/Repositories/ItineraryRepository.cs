using BaseMiCakeApplication.Domain.Aggregates;
using BaseMiCakeApplication.Domain.Repositories;
using BaseMiCakeApplication.Infrastructure.StroageModels;
using MiCake.EntityFrameworkCore.Repository;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BaseMiCakeApplication.EFCore.Repositories
{
    public class ItineraryRepository :
        EFRepositoryWithPO<BaseAppDbContext, Itinerary, ItinerarySnapshotModel, Guid>,
        IItineraryRepository
    {
        public ItineraryRepository(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public List<Itinerary> GetLastWeekItineraryInfo()
        {
            var persistentObjects = DbSet.Where(s => s.CreationTime > DateTime.Now.AddDays(-7)).ToList();
            return MapToDO(persistentObjects).ToList();
        }

        public void UpdateLastWeekItineraryInfo(List<Itinerary> itineraries)
        {
            var doToPo = MapToDO(DbSet.Where(s => s.CreationTime > DateTime.Now.AddDays(-7)).ToList());
            DbSet.UpdateRange(MapToPO(doToPo));
        }
    }
}
