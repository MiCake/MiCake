using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.Uow
{
    /// <summary>
    /// Mark root <see cref="IUnitOfWork"/> transaction disabled
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class DisableTransactionAttribute : Attribute
    {
        public DisableTransactionAttribute()
        {
        }
    }
}
