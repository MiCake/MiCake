namespace MiCake.Uow
{
    public enum UnitOfWorkCreateType
    {
        /// <summary>
        /// Create a new unit of work that reuse a existing uow's scope. If there has no existing uow, create a new scope.
        /// </summary>
        ReUseScopeIfExists,

        /// <summary>
        /// Whether there is a uow exists or not, always create a new uow use new scope.
        /// </summary>
        AllwaysCreateNewScope,
    }
}
