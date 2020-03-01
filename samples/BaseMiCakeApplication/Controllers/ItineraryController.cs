﻿using BaseMiCakeApplication.Domain.Aggregates;
using BaseMiCakeApplication.Domain.Repositories;
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

        public ItineraryController(
            IItineraryRepository repository)
        {
            _repository = repository;
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
    }
}
