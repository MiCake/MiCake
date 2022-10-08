using MiCake.Cord.Storage.Internal;
using System.Linq.Expressions;
using System.Reflection;

namespace MiCake.Cord.Storage.Builder
{
    /// <summary>
    /// This is an internal API  not subject to the same compatibility standards as public APIs.
    /// It may be changed or removed without notice in any release.
    /// 
    ///  Provides a simple API surface for configuring an <see cref="StoreEntityType" /> from conventions.
    /// </summary>
    public interface IConventionStoreEntityBuilder
    {
        /// <summary>
        /// Add the property information required for the persistence object
        /// </summary>
        IConventionStorePropertyBuilder AddProperty(string propertyName, MemberInfo memberInfo);

        /// <summary>
        /// Mark whether the persistent object needs to be removed directly from the database
        /// If do not need to delete directly, the database provider may use soft deletion to process
        /// </summary>
        IConventionStoreEntityBuilder SetDirectDeletion(bool directDeletion);

        /// <summary>
        /// Add the ignored property information for the persistence object
        /// </summary>
        IConventionStoreEntityBuilder AddIgnoredMember(string propertyName);

        /// <summary>
        /// Add the filter of the persistent object at query time
        /// </summary>
        IConventionStoreEntityBuilder AddQueryFilter(LambdaExpression expression);
    }
}
