using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.Audit.Tests.Fakes
{
    public class ModelHasCreationAudit : ICreationAudit
    {
        public DateTime CreationTime { get; set; }
        public long CreatorID { get; set; }
    }

    public class ModelHasCreationAuditGeneric : ICreationAudit<Guid>
    {
        public DateTime CreationTime { get; set; }
        public Guid CreatorID { get; set; }
    }
}
