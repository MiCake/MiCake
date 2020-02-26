using BaseMiCakeApplication.Domain.Aggregates;
using MiCake.DDD.Domain;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace BaseMiCakeApplication.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class ItineraryController : ControllerBase
    {
        private readonly IRepository<Itinerary, Guid> _repository;

        public ItineraryController(
            IRepository<Itinerary, Guid> repository)
        {
            _repository = repository;
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

        [HttpPost]
        public async Task ChangeItineraryNoteAsync(Guid id, string content)
        {
            var entity = await _repository.FindAsync(id);
            entity.ChangeNote(content);

            await _repository.UpdateAsync(entity);
        }
    }
}
