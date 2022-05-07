using MiCake.Core.Data;
using MiCake.Core.Util;
using System.Linq.Expressions;
using System.Reflection;

namespace MiCake.DDD.Connector.Store.Configure
{
    public class StoreEntityBuilder : IHasAccessor<InternalStoreEntityBuilder>
    {
        protected InternalStoreEntityBuilder _builer;

        InternalStoreEntityBuilder IHasAccessor<InternalStoreEntityBuilder>.AccessibleData => _builer;

        public StoreEntityBuilder(IStoreEntityType storeEntity)
        {
            CheckValue.NotNull(storeEntity, nameof(storeEntity));

            _builer = ((StoreEntityType)storeEntity).Builder;
        }

        /// <summary>
        /// Add the property information required for the persistence object
        /// </summary>
        public virtual StorePropertyBuilder Property(string propertyName)
        {
            CheckValue.NotNull(propertyName, nameof(propertyName));

            return new StorePropertyBuilder(_builer.AddProperty(propertyName).Metadata);
        }

        /// <summary>
        /// Add the property information required for the persistence object
        /// </summary>
        public virtual StorePropertyBuilder Property(string propertyName, MemberInfo clrMemberInfo)
        {
            CheckValue.NotNull(propertyName, nameof(propertyName));
            CheckValue.NotNull(clrMemberInfo, nameof(clrMemberInfo));

            return new StorePropertyBuilder(_builer.AddProperty(propertyName, clrMemberInfo).Metadata);
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

        /// <summary>
        /// Add the filter of the persistent object at query time
        /// </summary>
        public virtual StoreEntityBuilder HasQueryFilter(LambdaExpression lambdaExpression)
        {
            _builer.AddQueryFilter(lambdaExpression);
            return this;
        }
    }
}
