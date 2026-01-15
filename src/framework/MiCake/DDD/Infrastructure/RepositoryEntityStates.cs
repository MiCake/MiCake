using System;

namespace MiCake.DDD.Infrastructure
{
    /// <summary>
    /// The state in which the domain object is about to be changed in the repository
    /// </summary>
    [Flags]
    public enum RepositoryEntityStates
    {
        Unchanged = 1,
        Deleted = 2,
        Modified = 4,
        Added = 8
    }
}
