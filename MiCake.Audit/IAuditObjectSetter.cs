using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.Audit
{
    public interface IAuditObjectSetter
    {
        void SetCreationInfo(object targetObject);

        void SetModificationInfo(object targetObject);

        void SetDeletionInfo(object targetObject);
    }
}
