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
        public long? CreatorID { get; set; }

        public Book()
        {
        }

        public Book(string bookName, string authorFirstName, string authorLastName)
        {
            if (string.IsNullOrEmpty(bookName))
                throw new SlightMiCakeException("书名不能为空");

            Id = Guid.NewGuid();
            BookName = bookName;
            Author = new BookAuthor(authorFirstName, authorLastName);
        }

        public void ChangeAuthor(string firstName, string lastName)
        {
            RaiseDomainEvent(new BookChangeEvent(BookName));
            RaiseDomainEvent(new NewBookChangeEvent(BookName));

            Author = new BookAuthor(firstName, lastName);
        }
    }
}
