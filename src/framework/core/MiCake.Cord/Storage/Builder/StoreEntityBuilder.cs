using MiCake.Cord.Storage.Internal;
using MiCake.Core.Data;
using MiCake.Core.Util;
using System.Linq.Expressions;
using System.Reflection;

namespace MiCake.Cord.Storage.Builder
{
    public class StoreEntityBuilder : IHasAccessor<StoreEntityType>
    {
        protected readonly StoreEntityType _entitySource;

        StoreEntityType IHasAccessor<StoreEntityType>.AccessibleData => _entitySource;

        public StoreEntityBuilder(IStoreEntityType storeEntity)
        {
            CheckValue.NotNull(storeEntity, nameof(storeEntity));

            _entitySource = storeEntity as StoreEntityType ?? throw new ArgumentNullException(nameof(storeEntity));
        }

        /// <summary>
        /// Add the property information required for the persistence object
        /// </summary>
        public virtual StorePropertyBuilder Property(string propertyName)
        {
            CheckValue.NotNull(propertyName, nameof(propertyName));

            return new StorePropertyBuilder(_entitySource.AddProperty(propertyName));
        }

        /// <summary>
        /// Add the property information required for the persistence object
        /// </summary>
        public virtual StorePropertyBuilder Property(string propertyName, MemberInfo clrMemberInfo)
        {
            CheckValue.NotNull(propertyName, nameof(propertyName));
            CheckValue.NotNull(clrMemberInfo, nameof(clrMemberInfo));

            return new StorePropertyBuilder(_entitySource.AddProperty(propertyName, clrMemberInfo));
        }

        /// <summary>
        /// Mark whether the persistent object needs to be removed directly from the database
        /// If do not need to delete directly, the database provider may use soft deletion to process
        /// </summary>
        public virtual StoreEntityBuilder DirectDeletion(bool directDeletion)
        {
            _entitySource.SetDirectDeletion(directDeletion);
            return this;
        }

        /// <summary>
        /// Add the ignored property information for the persistence object
        /// </summary>
        public virtual StoreEntityBuilder Ignored(string propertyName)
        {
            _entitySource.AddIgnoredMember(propertyName);
            return this;
        }

        /// <summary>
        /// Add the filter of the persistent object at query time
        /// </summary>
        public virtual StoreEntityBuilder HasQueryFilter(LambdaExpression lambdaExpression)
        {
            _entitySource.AddQueryFilter(lambdaExpression);
            return this;
        }
    }
}
