using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.Audit
{
    /// <summary>
    /// Mark a class who has deletion time and delete user
    /// </summary>
    public interface IDeleteAudit : IHasDeleteUser, IHasDeletionTime
    {
    }

    /// <summary>
    /// Mark a class who has deletion time and delete user
    /// </summary>
    /// <typeparam name="TKeyType">the primary key type of user</typeparam>
    public interface IDeleteAudit<TKeyType> : IHasDeletionTime, IHasDeleteUser<TKeyType>
    {
    }
}
