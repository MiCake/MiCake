using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.Audit
{
    /// <summary>
    /// Mark a class with has creation time and creator
    /// </summary>
    public interface ICreationAudit : IHasCreationTime, IHasCreator
    {
    }

    /// <summary>
    /// Mark a class with has creation time and creator
    /// </summary>
    /// <typeparam name="TUserKeytType">a primary type for your user class</typeparam>
    public interface ICreationAudit<TUserKeytType> : IHasCreationTime, IHasCreator<TUserKeytType>
    {
    }
}
