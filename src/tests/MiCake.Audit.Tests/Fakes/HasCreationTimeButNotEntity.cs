using System;

namespace MiCake.Audit.Tests.Fakes
{
    public class HasCreationTimeButNotEntity : IHasCreatedTime
    {
        public DateTime CreatedTime { get; set; }
    }
}
