using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.Audit
{
    public interface IHasDeletionTime
    {
        DateTime? DeletionTime { get; set; }
    }
}
