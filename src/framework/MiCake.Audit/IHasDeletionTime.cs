using System;

namespace MiCake.Audit
{
    public interface IHasDeletionTime
    {
        DateTime? DeletionTime { get; set; }
    }
}
