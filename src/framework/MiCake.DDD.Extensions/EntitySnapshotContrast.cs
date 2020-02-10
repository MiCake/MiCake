using System.Collections.Concurrent;
using MiCake.DDD.Domain;

namespace MiCake.DDD.Extensions
{
    /// <summary>
    /// Save the contrast relationship between 
    /// store entity instance and <see cref="IEntity"/> instance
    /// </summary>
    public static class EntitySnapshotStore
    {
        /// <summary>
        /// a dictionary for : key:storeEntity value:IEntity
        /// </summary>
        private static ConcurrentDictionary<object, IEntity> dic = new ConcurrentDictionary<object, IEntity>();

        public static void AddMapping(object snapshotInstance, IEntity entity)
        {
            dic.TryAdd(snapshotInstance, entity);
        }

        public static void RemoveMapping(object snapshotInstance)
        {
            dic.TryRemove(snapshotInstance, out var entity);
        }

        public static IEntity GetEntity(object snapshotInstance)
        {
            dic.TryGetValue(snapshotInstance, out var entity);
            return entity;
        }

        public static bool GetEntity(object snapshotInstance, out IEntity entity)
        {
            return dic.TryGetValue(snapshotInstance, out entity);
        }
    }
}
