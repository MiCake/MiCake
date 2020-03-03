using BaseMiCakeApplication.Domain.Aggregates;
using MiCake.DDD.Domain;
using System;
using System.Collections.Generic;

namespace BaseMiCakeApplication.Domain.Repositories
{
    public interface IItineraryRepository : IRepository<Itinerary, Guid>
    {
        List<Itinerary> GetLastWeekItineraryInfo();

        void UpdateLastWeekItineraryInfo(List<Itinerary> itineraries);
    }
}
