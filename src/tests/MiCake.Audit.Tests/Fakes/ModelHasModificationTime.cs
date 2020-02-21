using System;

namespace MiCake.Audit.Tests.Fakes
{
    public class ModelHasModificationTime : IHasModificationTime
    {
        public DateTime? ModficationTime { get; set; }
    }
}
