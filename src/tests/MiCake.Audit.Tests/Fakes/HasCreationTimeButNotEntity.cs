using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.Audit.Tests.Fakes
{
    public class HasCreationTimeButNotEntity : IHasCreationTime
    {
        public DateTime CreationTime { get; set; }
    }
}
