using System;
using System.Linq.Expressions;

namespace MiCake.DDD.Extensions.Store.Configure
{
    public class StoreEntityBuilder<TEntity> : StoreEntityBuilder
        where TEntity : class
    {
        public StoreEntityBuilder(IStoreEntityType storeEntity) : base(storeEntity)
        {
        }

        /// <summary>
        /// Add the property information required for the persistence object
        /// </summary>
        public virtual StorePropertyBuilder<TProperty> Property<TProperty>(Expression<Func<TEntity, TProperty>> propertyExpression)
        {
            var propertyInfo = propertyExpression.GetPropertyAccess();
            return new StorePropertyBuilder<TProperty>(_builer.AddProperty(propertyInfo.Name, propertyInfo).Metadata);
        }

        /// <summary>
        /// Mark whether the persistent object needs to be removed directly from the database
        /// If do not need to delete directly, the database provider may use soft deletion to process
        /// </summary>
        public new virtual StoreEntityBuilder<TEntity> DirectDeletion(bool directDeletion)
        {
            base.DirectDeletion(directDeletion);
            return this;
        }

        /// <summary>
        /// Add the ignored property information for the persistence object
        /// </summary>
        public new virtual StoreEntityBuilder<TEntity> Ignored(string propertyName)
        {
            base.Ignored(propertyName);
            return this;
        }
    }
}
