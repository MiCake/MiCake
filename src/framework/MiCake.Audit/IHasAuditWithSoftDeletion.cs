using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.Audit
{
    /// <summary>
    /// Mark a class with creation info,modify info and deletion info.
    /// If the implementation type is marked as soft delete, the data will not be deleted from the database
    /// </summary>
    public interface IHasAuditWithSoftDeletion : IHasAudit, ISoftDeletion, IDeleteAudit
    {
    }

    /// <summary>
    /// Mark a class with creation info, modify info and deletion info.
    /// If the implementation type is marked as soft delete, the data will not be deleted from the database
    /// </summary>
    /// <typeparam name="TUserKeyType">a primary type for your user class</typeparam>
    public interface IHasAuditWithSoftDeletion<TUserKeyType> : IHasAudit<TUserKeyType>, ISoftDeletion, IDeleteAudit<TUserKeyType>
    {
    }
}
