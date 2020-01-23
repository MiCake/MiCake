using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.Audit.Tests.Fakes
{
    public class ModelHasModificationAudit : IModificationAudit
    {
        public DateTime? ModficationTime { get; set; }
        public long? ModifierID { get; set; }
    }

    public class ModelHasModificationAuditGeneric : IModificationAudit<Guid>
    {
        public DateTime? ModficationTime { get; set; }
        public Guid ModifierID { get; set; }
    }
}
