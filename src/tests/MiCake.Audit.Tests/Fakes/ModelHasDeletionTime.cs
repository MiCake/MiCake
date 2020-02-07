using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.Audit.Tests.Fakes
{
    public class ModelHasDeletionTime : IHasDeletionTime
    {
        public DateTime? DeletionTime { get; set; }
    }

    public class ModelHasDeletionTimeAndSoftDeletion : IHasDeletionTime, ISoftDeletion
    {
        public bool IsDeleted { get; set; }
        public DateTime? DeletionTime { get; set; }
    }
}
