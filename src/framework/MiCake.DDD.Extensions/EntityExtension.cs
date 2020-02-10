using MiCake.DDD.Domain;

namespace MiCake.DDD.Extensions
{
    public static class EntityExtension
    {
        /// <summary>
        /// Get entity snapshot instance. 
        /// And will add relationship between snapshot instance and entity to <see cref="EntitySnapshotStore"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static T ToSnapshot<T>(this IEntityHasSnapshot<T> entity)
        {
            T result = default;

            if (entity is IEntity entityInstance)
            {
                result = entity.GetSnapshot();

                if (result != null)
                    EntitySnapshotStore.AddMapping(result, entityInstance);
            }
            return result;
        }

    }
}
