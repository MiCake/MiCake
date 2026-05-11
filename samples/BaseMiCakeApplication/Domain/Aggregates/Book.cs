using BaseMiCakeApplication.Domain.Aggregates.Events;
using MiCake.Audit;
using MiCake.Core;
using MiCake.DDD.Domain;
using System;

namespace BaseMiCakeApplication.Domain.Aggregates
{
    /// <summary>
    /// Book aggregate root - Manages book information and lifecycle.
    /// </summary>
    /// <remarks>
    /// This aggregate root demonstrates DDD patterns including:
    /// - Value objects (BookAuthor)
    /// - Domain events
    /// - Business rule validation
    /// - Aggregate root responsibilities
    /// </remarks>
    public class Book : AggregateRoot<Guid>, IHasCreatedAt<DateTimeOffset>, IHasUpdatedAt<DateTimeOffset>
    {
        /// <summary>
        /// Gets the book name/title.
        /// </summary>
        public string BookName { get; private set; }

        /// <summary>
        /// Gets the book author information (Value Object).
        /// </summary>
        public BookAuthor Author { get; private set; }

        /// <summary>
        /// Gets the description of the book.
        /// </summary>
        public string Description { get; private set; }

        /// <summary>
        /// Gets the ISBN of the book.
        /// </summary>
        public string ISBN { get; private set; }

        /// <summary>
        /// Gets the publication year.
        /// </summary>
        public int? PublicationYear { get; private set; }

        /// <summary>
        /// Gets or sets the user ID who created this book record.
        /// </summary>
        public long? CreatorID { get; set; }

        /// <summary>
        /// Gets the creation time (Audit support).
        /// </summary>
        public DateTimeOffset CreatedAt { get; set; }

        /// <summary>
        /// Gets the modification time (Audit support).
        /// </summary>
        public DateTimeOffset? UpdatedAt { get; set; }

        /// <summary>
        /// Initializes a new instance of the Book class.
        /// </summary>
        public Book()
        {
        }

        /// <summary>
        /// Creates a new Book with required information.
        /// </summary>
        /// <param name="bookName">The name/title of the book</param>
        /// <param name="authorFirstName">Author's first name</param>
        /// <param name="authorLastName">Author's last name</param>
        /// <exception cref="BusinessException">Thrown when book name is empty</exception>
        public Book(string bookName, string authorFirstName, string authorLastName)
        {
            if (string.IsNullOrEmpty(bookName))
                throw new BusinessException("Book name cannot be empty");

            Id = Guid.NewGuid();
            BookName = bookName;
            Author = new BookAuthor(authorFirstName, authorLastName);
        }

        /// <summary>
        /// Updates the author information and raises a domain event.
        /// </summary>
        /// <param name="firstName">Author's first name</param>
        /// <param name="lastName">Author's last name</param>
        /// <remarks>
        /// This method demonstrates raising domain events that can be handled by
        /// domain event handlers or application services.
        /// </remarks>
        public void ChangeAuthor(string firstName, string lastName)
        {
            // Raise events to notify subscribers
            RaiseDomainEvent(new BookChangeEvent(BookName));

            // Update aggregate state
            Author = new BookAuthor(firstName, lastName);
        }

        /// <summary>
        /// Updates the book description.
        /// </summary>
        /// <param name="description">The new description</param>
        public void UpdateDescription(string description)
        {
            Description = description;
        }

        /// <summary>
        /// Sets the ISBN for the book.
        /// </summary>
        /// <param name="isbn">The ISBN value</param>
        public void SetISBN(string isbn)
        {
            if (string.IsNullOrWhiteSpace(isbn))
                throw new BusinessException("ISBN cannot be empty");

            ISBN = isbn;
        }

        /// <summary>
        /// Sets the publication year.
        /// </summary>
        /// <param name="year">The publication year</param>
        public void SetPublicationYear(int year)
        {
            if (year < 1000 || year > DateTime.Now.Year)
                throw new BusinessException("Invalid publication year");

            PublicationYear = year;
        }

        /// <summary>
        /// Factory method to create a new book with complete information.
        /// </summary>
        public static Book CreateNew(string bookName, string authorFirstName, string authorLastName,
            string isbn = null, int? publicationYear = null, string description = null)
        {
            var book = new Book(bookName, authorFirstName, authorLastName);
            
            if (!string.IsNullOrEmpty(isbn))
                book.SetISBN(isbn);
            
            if (publicationYear.HasValue)
                book.SetPublicationYear(publicationYear.Value);
            
            if (!string.IsNullOrEmpty(description))
                book.UpdateDescription(description);

            return book;
        }
    }
}
