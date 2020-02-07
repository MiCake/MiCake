using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.Audit.Tests.Fakes
{
    public class ModelHasCreationTime : IHasCreationTime
    {
        public DateTime CreationTime { get; set; }
    }

}
