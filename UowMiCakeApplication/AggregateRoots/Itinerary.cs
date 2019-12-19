using MiCake.DDD.Domain;
using System;

namespace UowMiCakeApplication.AggregateRoots
{
    public class Itinerary : AggregateRoot<Guid>
    {
        public string Participants { get; set; }

        public string Places { get; set; }

        public string Note { get; set; }

        public string TripTime { get; set; }

        public string Status { get; set; }

        public Itinerary()
        {
            Id = Guid.NewGuid();
        }

        //ctor
        public Itinerary(string p1, string p2, string p3, string p4, string p5)
        {
            Id = Guid.NewGuid();
            Participants = p1;
            Places = p2;
            Note = p3;
            TripTime = p4;
            Status = p5;
        }
    }
}
