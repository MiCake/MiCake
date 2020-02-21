using System;

namespace MiCake.Audit
{
    public interface IHasCreationTime
    {
        DateTime CreationTime { get; set; }
    }
}
