using BaseMiCakeApplication.Domain.Aggregates;
using BaseMiCakeApplication.Domain.Repositories;
using MiCake.DDD.Domain;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BaseMiCakeApplication.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class ItineraryController : ControllerBase
    {
        private readonly IItineraryRepository _repository;
        private readonly IRepository<Itinerary, Guid> _test;

        public ItineraryController(
            IRepository<Itinerary, Guid> test,
            IItineraryRepository repository)
        {
            _repository = repository;
            _test = test;
        }

        [HttpGet]
        public async Task<Itinerary> GetItineraryAsync(Guid id)
        {
            return await _repository.FindAsync(id);
        }

        [HttpGet]
        public List<Itinerary> GetItineraryLastWeekInfo()
        {
            return _repository.GetLastWeekItineraryInfo();
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

        [HttpPost]
        public void ChangeItineraryNotesAsync()
        {
            var s = _repository.GetLastWeekItineraryInfo();
            _repository.UpdateLastWeekItineraryInfo(s);
        }

        [HttpPost]
        public async Task DeleteItineraryAsync(Guid id)
        {
            var entity = await _repository.FindAsync(id);
            await _repository.DeleteAsync(entity);
        }
    }
}
