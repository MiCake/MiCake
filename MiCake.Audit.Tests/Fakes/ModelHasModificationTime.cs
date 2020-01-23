using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.Audit.Tests.Fakes
{
    public class ModelHasModificationTime : IHasModificationTime
    {
        public DateTime? ModficationTime { get; set; }
    }
}
