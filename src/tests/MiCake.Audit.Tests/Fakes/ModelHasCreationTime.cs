using System;

namespace MiCake.Audit.Tests.Fakes
{
    public class ModelHasCreationTime : IHasCreationTime
    {
        public DateTime CreationTime { get; set; }
    }

}
