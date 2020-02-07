using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.Audit
{
    public interface IHasCreationTime
    {
        DateTime CreationTime { get; set; }
    }
}
