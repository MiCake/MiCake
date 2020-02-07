using MiCake.DDD.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BaseMiCakeApplication.Domain.Aggregates
{
    public class ItineraryNote : ValueObject
    {
        public string Content { get;private set; }
        public DateTime NoteTime { get; private set; }

        public ItineraryNote(string content)
        {
            if (content.Length > 200)
                throw new Exception();

            Content = content;
            NoteTime = DateTime.Now;
        }
    }
}
