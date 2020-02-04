using BaseMiCakeApplication.Domain.Aggregates;
using MiCake.DDD.Domain;
using MiCake.Uow;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace BaseMiCakeApplication.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ItineraryController : ControllerBase
    {
        private readonly IRepository<Itinerary, Guid> _repository;
        private IServiceProvider _serviceProvider;

        public ItineraryController(IRepository<Itinerary, Guid> repository, IServiceProvider serviceProvider)
        {
            _repository = repository;
            _serviceProvider = serviceProvider;
        }

        [HttpGet]
        public async Task<Itinerary> GetItineraryAsync(Guid id)
        {
            return await _repository.FindAsync(id);
        }

        [HttpPost]
        public async Task AddItineraryAsync(string content)
        {
            Itinerary itinerary = new Itinerary(content);
            await _repository.AddAsync(itinerary);
        }
    }
}
