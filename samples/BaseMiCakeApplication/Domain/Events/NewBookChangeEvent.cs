using MiCake.DDD.Domain;

namespace BaseMiCakeApplication.Domain.Events
{
    public class NewBookChangeEvent : DomainEvent
    {
        public string Name { get; set; }

        public NewBookChangeEvent(string name)
        {
            Name = name;
        }
    }
}
