using MiCake.DDD.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
