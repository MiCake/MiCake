using MiCake.DDD.Domain;

namespace BaseMiCakeApplication.Domain.Aggregates.Events
{
    public class BookChangeEvent : DomainEvent
    {
        public string Name { get; set; }

        public BookChangeEvent(string name)
        {
            Name = name;
        }
    }
}
