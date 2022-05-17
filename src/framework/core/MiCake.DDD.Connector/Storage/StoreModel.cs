using MiCake.Cord.Storage.Internal;

namespace MiCake.Cord.Storage
{
    public class StoreModel : IStoreModel
    {
        private readonly Dictionary<Type, StoreEntityType> _storeEntityTypes = new();

        public StoreModel()
        {
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public IConventionStoreEntity AddStoreEntity(Type clrType)
        {
            if (_storeEntityTypes.TryGetValue(clrType, out var storeEntity))
                return storeEntity;

            var declareStoreEntity = new StoreEntityType(clrType);
            _storeEntityTypes.Add(clrType, declareStoreEntity);
            return declareStoreEntity;
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public IConventionStoreEntity? FindStoreEntity(Type clrType)
        {
            return _storeEntityTypes.TryGetValue(clrType, out var storeEntity) ? storeEntity : null;
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public IEnumerable<IConventionStoreEntity> GetStoreEntities()
        {
            return _storeEntityTypes.Values;
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public void RemoveStoreEntity(Type clrType)
        {
            _storeEntityTypes.Remove(clrType);
        }
    }
}
