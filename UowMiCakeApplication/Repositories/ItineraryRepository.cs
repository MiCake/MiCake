using MiCake.EntityFrameCore.Easy;
using MiCake.Uow.Easy;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UowMiCakeApplication.AggregateRoots;
using UowMiCakeApplication.EFCore;

namespace UowMiCakeApplication.Repositories
{
    public class ItineraryRepository : EFRepository<Itinerary, Guid>
    {
        public ItineraryRepository(IUnitOfWorkManager unitOfWorkManager, UowAppDbContext dbContext) : base(unitOfWorkManager, dbContext)
        {
        }
    }
}
