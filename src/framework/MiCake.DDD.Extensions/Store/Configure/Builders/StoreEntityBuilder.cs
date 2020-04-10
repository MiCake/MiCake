using MiCake.Core.Data;
using MiCake.Core.Util;
using System;

namespace MiCake.DDD.Extensions.Store.Configure
{
    public class StoreEntityBuilder : IHasAccessor<InternalStoreEntityBuilder>
    {
        protected InternalStoreEntityBuilder _builer;

        InternalStoreEntityBuilder IHasAccessor<InternalStoreEntityBuilder>.Instance => _builer;

        public StoreEntityBuilder(IStoreEntityType storeEntity)
        {
            CheckValue.NotNull(storeEntity, nameof(storeEntity));

            _builer = ((StoreEntityType)storeEntity).Builder;
        }

        /// <summary>
        /// Add the property information required for the persistence object
        /// </summary>
        public virtual StorePropertyBuilder Property(string propertyName, Type propertyType)
        {
            CheckValue.NotNull(propertyName, nameof(propertyName));
            CheckValue.NotNull(propertyType, nameof(propertyType));

            return new StorePropertyBuilder(_builer.AddProperty(propertyName, propertyType).Metadata);
        }

        /// <summary>
        /// Mark whether the persistent object needs to be removed directly from the database
        /// If do not need to delete directly, the database provider may use soft deletion to process
        /// </summary>
        public virtual StoreEntityBuilder DirectDeletion(bool directDeletion)
        {
            _builer.SetDirectDeletion(directDeletion);
            return this;
        }

        /// <summary>
        /// Add the ignored property information for the persistence object
        /// </summary>
        public virtual StoreEntityBuilder Ignored(string propertyName)
        {
            _builer.AddIgnoredMember(propertyName);
            return this;
        }
    }
}
