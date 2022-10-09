namespace MiCake.Uow
{
    /// <summary>
    /// Disable current method or class auto commit unit of work.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class DisableAutoUnitOfWorkAttribute : Attribute
    {
        public DisableAutoUnitOfWorkAttribute()
        {
        }
    }

    /// <summary>
    /// Mark current method or class auto commit unit of work.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class AutoUnitOfWorkAttribute : Attribute
    {
        public AutoUnitOfWorkAttribute()
        {
        }
    }
}
