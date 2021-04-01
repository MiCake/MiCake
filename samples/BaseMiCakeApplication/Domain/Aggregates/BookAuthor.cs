using MiCake.Core;
using MiCake.DDD.Domain;
using System.Collections.Generic;

namespace BaseMiCakeApplication.Domain.Aggregates
{
    public class BookAuthor : ValueObject
    {
        public string FirstName { get; private set; }

        public string LastName { get; private set; }

        public BookAuthor(string firstName, string lastName)
        {
            if (string.IsNullOrEmpty(firstName))
                throw new SoftlyMiCakeException("作者信息的姓不能为空");

            if (string.IsNullOrEmpty(lastName))
                throw new SoftlyMiCakeException("作者信息的名不能为空");

            FirstName = firstName;
            LastName = lastName;
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return FirstName;
            yield return LastName;
        }
    }
}