using MiCake.DDD.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.Audit.Tests.Fakes
{
    public class HasModificationTimeModel : Entity, IHasModificationTime
    {
        public DateTime? ModificationTime { get; set; }
    }
}
