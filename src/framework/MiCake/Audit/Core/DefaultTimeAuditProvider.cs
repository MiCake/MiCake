﻿using MiCake.DDD.Extensions;
using System;

namespace MiCake.Audit.Core
{
    /// <summary>
    /// Give entity creation time or modifaction time.
    /// </summary>
    internal class DefaultTimeAuditProvider : IAuditProvider
    {
        public virtual void ApplyAudit(AuditObjectModel auditObjectModel)
        {
            if (auditObjectModel.EntityState == RepositoryEntityState.Added)
            {
                SetCreationTime(auditObjectModel.AuditEntity);
            }
            else if (auditObjectModel.EntityState == RepositoryEntityState.Modified)
            {
                SetModifactionTime(auditObjectModel.AuditEntity);
            }
        }

        private static void SetCreationTime(object entity)
        {
            if (!(entity is IHasCreationTime hasCreationTimeObj))
                return;

            if (hasCreationTimeObj.CreationTime == default)
                hasCreationTimeObj.CreationTime = DateTime.Now;
        }

        private static void SetModifactionTime(object entity)
        {
            if (!(entity is IHasModificationTime hasModificationTimeObj))
                return;

            hasModificationTimeObj.ModificationTime = DateTime.Now;
        }
    }
}
