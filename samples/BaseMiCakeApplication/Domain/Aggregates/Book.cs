using BaseMiCakeApplication.Domain.Aggregates.Events;
using BaseMiCakeApplication.Domain.Events;
using MiCake.Core;
using MiCake.DDD.Domain;
using System;

namespace BaseMiCakeApplication.Domain.Aggregates
{
    public class Book : AggregateRoot<Guid>
    {
        public string BookName { get; private set; }

        public BookAuthor Author { get; private set; }

        public Book()
        {
        }

        public Book(string bookName, string authorFirstName, string authorLastName)
        {
            if (string.IsNullOrEmpty(bookName))
                throw new SoftlyMiCakeException("书名不能为空");

            Id = Guid.NewGuid();
            BookName = bookName;
            Author = new BookAuthor(authorFirstName, authorLastName);
        }

        public void ChangeAuthor(string firstName, string lastName)
        {
            AddDomainEvent(new BookChangeEvent(BookName));
            AddDomainEvent(new NewBookChangeEvent(BookName));

            Author = new BookAuthor(firstName, lastName);
        }
    }
}
