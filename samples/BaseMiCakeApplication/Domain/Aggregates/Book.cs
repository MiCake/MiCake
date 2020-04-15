using MiCake.Core.Util;
using MiCake.DDD.Domain;
using System;

namespace BaseMiCakeApplication.Domain.Aggregates
{
    public class Book : AggregateRoot<Guid>
    {
        public string Name { get; private set; }
        public string Author { get; private set; }

        public Book()
        {
            Id = Guid.NewGuid();
        }

        public Book(string name, string author) : this()
        {
            CheckValue.NotNull(name, nameof(name));
            CheckValue.NotNull(author, nameof(author));

            Name = name;
            Author = author;
        }

        public void ChangeName(string name)
        {
            CheckValue.NotNullOrEmpty(name, nameof(name));
            Name = name;
        }
    }
}
