using MiCake.Core.Util.Collections;
using MiCake.DDD.Extensions.Store;
using System;
using System.Collections.Generic;

namespace MiCake.EntityFrameworkCore.Options
{
    /// <summary>
    /// Configuration options for MiCake EF Core conventions
    /// </summary>
    public class MiCakeEFCoreConventionOptions
    {
        private readonly List<IStoreConvention> _conventions = new();

        /// <summary>
        /// Gets the list of registered conventions
        /// </summary>
        public IReadOnlyList<IStoreConvention> Conventions => _conventions.AsReadOnly();

        /// <summary>
        /// Add a convention to the engine
        /// </summary>
        /// <param name="convention">The convention to add</param>
        /// <returns>This options instance for method chaining</returns>
        public MiCakeEFCoreConventionOptions AddConvention(IStoreConvention convention)
        {
            if (convention == null)
                throw new ArgumentNullException(nameof(convention));

            _conventions.AddIfNotContains(convention, c => c.GetType() == convention.GetType());
            return this;
        }

        /// <summary>
        /// Add multiple conventions to the engine
        /// </summary>
        /// <param name="conventions">The conventions to add</param>
        /// <returns>This options instance for method chaining</returns>
        public MiCakeEFCoreConventionOptions AddConventions(params IStoreConvention[] conventions)
        {
            if (conventions == null)
                throw new ArgumentNullException(nameof(conventions));

            foreach (var convention in conventions)
            {
                AddConvention(convention);
            }
            return this;
        }

        /// <summary>
        /// Remove a convention from the engine
        /// </summary>
        /// <typeparam name="TConvention">The type of convention to remove</typeparam>
        /// <returns>This options instance for method chaining</returns>
        public MiCakeEFCoreConventionOptions RemoveConvention<TConvention>()
            where TConvention : IStoreConvention
        {
            for (int i = _conventions.Count - 1; i >= 0; i--)
            {
                if (_conventions[i] is TConvention)
                {
                    _conventions.RemoveAt(i);
                }
            }
            return this;
        }

        /// <summary>
        /// Clear all conventions
        /// </summary>
        /// <returns>This options instance for method chaining</returns>
        public MiCakeEFCoreConventionOptions ClearConventions()
        {
            _conventions.Clear();
            return this;
        }

        /// <summary>
        /// Check if a specific convention type is registered
        /// </summary>
        /// <typeparam name="TConvention">The convention type to check</typeparam>
        /// <returns>True if the convention is registered</returns>
        public bool HasConvention<TConvention>()
            where TConvention : IStoreConvention
        {
            return _conventions.Exists(c => c is TConvention);
        }
    }
}