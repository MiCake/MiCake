using System;

namespace MiCake.DDD.Uow
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
