using System;

namespace MiCake.Audit
{
    public interface IHasModificationTime
    {
        DateTime? ModficationTime { get; set; }
    }
}
