using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.DDD.Extensions
{
    /// <summary>
    /// Provide a life cycle interface of repository operation process
    /// </summary>
    public interface IRepositoryLifetime
    {
        /// <summary>
        /// Operations before domain object persistence
        /// </summary>
        void PreSaveChanges(RepositoryEntityState entityState,object entity);

        /// <summary>
        /// Operations after domain object persistence
        /// </summary>
        void PostSaveChanges(RepositoryEntityState entityState, object entity);
    }
}
