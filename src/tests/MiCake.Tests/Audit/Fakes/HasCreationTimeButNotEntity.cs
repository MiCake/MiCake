using System;

namespace MiCake.Audit.Tests.Fakes
{
    public class HasCreationTimeButNotEntity : IHasCreatedAt
    {
        public DateTime CreatedAt { get; set; }
    }
}
