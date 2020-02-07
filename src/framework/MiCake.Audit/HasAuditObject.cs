using System;

namespace MiCake.Audit
{
    public class HasAuditObject : IHasAudit
    {
        public DateTime CreationTime { get; set; }
        public long CreatorID { get; set; }
        public DateTime? ModficationTime { get; set; }
        public long? ModifierID { get; set; }

        public HasAuditObject()
        {
        }
    }

    public class HasAuditObject<TUserKeyType> : IHasAudit<TUserKeyType>
    {
        public DateTime CreationTime { get ; set ; }
        public TUserKeyType CreatorID { get ; set ; }
        public DateTime? ModficationTime { get ; set ; }
        public TUserKeyType ModifierID { get ; set ; }

        public HasAuditObject()
        {
        }
    }
}
