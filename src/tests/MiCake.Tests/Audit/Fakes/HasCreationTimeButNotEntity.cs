using System;

namespace MiCake.Audit.Tests.Fakes
{
    public class HasCreationTimeButNotEntity : IHasCreationTime
    {
        public DateTime CreationTime { get; set; }
    }
}
