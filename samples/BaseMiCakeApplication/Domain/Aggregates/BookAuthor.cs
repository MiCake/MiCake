using MiCake.Core;
using MiCake.DDD.Domain;
using System.Collections.Generic;

namespace BaseMiCakeApplication.Domain.Aggregates
{
    /// <summary>
    /// BookAuthor value object - Represents author information immutably.
    /// </summary>
    /// <remarks>
    /// This is a Value Object which means:
    /// 1. It has no identity (id)
    /// 2. It is immutable
    /// 3. It is compared by value, not by reference
    /// 4. It cannot exist independently (owned by Book aggregate root)
    /// </remarks>
    public class BookAuthor : ValueObject
    {
        /// <summary>
        /// Gets the author's first name.
        /// </summary>
        public string FirstName { get; private set; }

        /// <summary>
        /// Gets the author's last name.
        /// </summary>
        public string LastName { get; private set; }

        /// <summary>
        /// Gets the full name of the author.
        /// </summary>
        public string FullName => $"{FirstName} {LastName}";

        /// <summary>
        /// Creates a new BookAuthor with the specified names.
        /// </summary>
        /// <param name="firstName">The author's first name</param>
        /// <param name="lastName">The author's last name</param>
        /// <exception cref="SlightMiCakeException">Thrown when name is empty</exception>
        public BookAuthor(string firstName, string lastName)
        {
            if (string.IsNullOrEmpty(firstName))
                throw new SlightMiCakeException("Author's first name cannot be empty");

            if (string.IsNullOrEmpty(lastName))
                throw new SlightMiCakeException("Author's last name cannot be empty");

            FirstName = firstName;
            LastName = lastName;
        }

        /// <summary>
        /// Returns the equality components for value object comparison.
        /// </summary>
        /// <remarks>
        /// Two BookAuthor instances are equal if they have the same FirstName and LastName.
        /// </remarks>
        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return FirstName;
            yield return LastName;
        }
    }
}