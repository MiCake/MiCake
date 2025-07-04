﻿namespace MiCake.DDD.Uow
{
    public enum UnitOfWorkScope
    {
        /// <summary>
        /// Create a new unit of work
        /// Use its unit of work operation if there is an ambient unit of work
        /// </summary>
        Required,

        /// <summary>
        /// Create a new unit of work anyway
        /// </summary>
        RequiresNew,

        /// <summary>
        /// Create a new unit of work anyway
        /// All operations in the cell will not be protected by transactions
        /// </summary>
        Suppress,
    }
}
