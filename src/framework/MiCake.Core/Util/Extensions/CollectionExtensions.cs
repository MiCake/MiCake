using System;
using System.Collections.Generic;
using System.Linq;

namespace MiCake.Util.Extensions
{
    /// <summary>
    /// Provides extension methods for collection types.
    /// </summary>
    public static class CollectionExtensions
    {
        /// <summary>
        /// Determines whether the specified collection is null or contains no elements.
        /// </summary>
        /// <typeparam name="T">The type of elements in the collection</typeparam>
        /// <param name="source">The collection to check</param>
        /// <returns>True if the collection is null or empty; otherwise, false</returns>
        public static bool IsNullOrEmpty<T>(this ICollection<T> source)
        {
            return source == null || source.Count <= 0;
        }

        /// <summary>
        /// Adds an item to the collection if it's not already present.
        /// </summary>
        /// <typeparam name="T">The type of elements in the collection</typeparam>
        /// <param name="source">The collection to add to</param>
        /// <param name="item">The item to add</param>
        /// <returns>True if the item was added; false if it was already present</returns>
        /// <exception cref="ArgumentNullException">Thrown when source is null</exception>
        public static bool AddIfNotContains<T>(this ICollection<T> source, T item)
        {
            ArgumentNullException.ThrowIfNull(source, nameof(source));

            if (source.Contains(item))
            {
                return false;
            }

            source.Add(item);
            return true;
        }

        /// <summary>
        /// Adds an item to the collection if no item matching the predicate is found.
        /// </summary>
        /// <typeparam name="T">The type of elements in the collection</typeparam>
        /// <param name="source">The collection to add to</param>
        /// <param name="item">The item to add</param>
        /// <param name="predicate">The condition to check for existing items</param>
        /// <returns>True if the item was added; false if a matching item already exists</returns>
        /// <exception cref="ArgumentNullException">Thrown when source or predicate is null</exception>
        public static bool AddIfNotContains<T>(this ICollection<T> source, T item, Func<T, bool> predicate)
        {
            ArgumentNullException.ThrowIfNull(source, nameof(source));
            ArgumentNullException.ThrowIfNull(predicate, nameof(predicate));

            if (source.Any(predicate))
            {
                return false;
            }

            source.Add(item);
            return true;
        }

        /// <summary>
        /// Adds multiple items to the collection, excluding items that are already present.
        /// </summary>
        /// <typeparam name="T">The type of elements in the collection</typeparam>
        /// <param name="source">The collection to add to</param>
        /// <param name="items">The items to add</param>
        /// <returns>An enumerable of items that were added</returns>
        /// <exception cref="ArgumentNullException">Thrown when source is null</exception>
        public static IEnumerable<T> AddIfNotContains<T>(this ICollection<T> source, IEnumerable<T> items)
        {
            ArgumentNullException.ThrowIfNull(source, nameof(source));

            var addedItems = new List<T>();

            foreach (var item in items)
            {
                if (source.Contains(item))
                {
                    continue;
                }

                source.Add(item);
                addedItems.Add(item);
            }

            return addedItems;
        }

        /// <summary>
        /// Adds an item to the collection if no item matching the predicate is found.
        /// The item is created using a factory function only if needed.
        /// </summary>
        /// <typeparam name="T">The type of elements in the collection</typeparam>
        /// <param name="source">The collection to add to</param>
        /// <param name="predicate">The condition to check for existing items</param>
        /// <param name="itemFactory">A factory function that creates the item if it needs to be added</param>
        /// <returns>True if the item was added; false if a matching item already exists</returns>
        /// <exception cref="ArgumentNullException">Thrown when source, predicate, or itemFactory is null</exception>
        public static bool AddIfNotContains<T>(this ICollection<T> source, Func<T, bool> predicate, Func<T> itemFactory)
        {
            ArgumentNullException.ThrowIfNull(source, nameof(source));
            ArgumentNullException.ThrowIfNull(predicate, nameof(predicate));
            ArgumentNullException.ThrowIfNull(itemFactory, nameof(itemFactory));

            if (source.Any(predicate))
            {
                return false;
            }

            source.Add(itemFactory());
            return true;
        }

        /// <summary>
        /// Removes all items from the collection that satisfy the given predicate.
        /// </summary>
        /// <typeparam name="T">The type of elements in the collection</typeparam>
        /// <param name="source">The collection to remove from</param>
        /// <param name="predicate">The condition that determines which items to remove</param>
        /// <returns>A list of items that were removed</returns>
        public static IList<T> RemoveAll<T>(this ICollection<T> source, Func<T, bool> predicate)
        {
            var items = source.Where(predicate).ToList();

            foreach (var item in items)
            {
                source.Remove(item);
            }

            return items;
        }
    }
}
