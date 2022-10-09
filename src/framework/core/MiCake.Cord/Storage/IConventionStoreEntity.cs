using System.Linq.Expressions;
using System.Reflection;

namespace MiCake.Cord.Storage
{
    /// <summary>
    /// This is an internal API  not subject to the same compatibility standards as public APIs.
    /// It may be changed or removed without notice in any release.
    /// 
    /// Expose interfaces that store entity can configure
    /// </summary>
    public interface IConventionStoreEntity
    {
        /// <summary>
        /// Indicates whether the persistent object needs to be removed directly from the database
        /// </summary>
        public bool DirectDeletion { get; }

        /// <summary>
        /// Add the property information required for the persistence object
        /// </summary>
        public IStoreProperty AddProperty(string name, MemberInfo memberInfo);

        /// <summary>
        /// Find <see cref="IStoreProperty"/> by name.
        /// </summary>
        public IStoreProperty? FindProperty(string name);

        /// <summary>
        /// Get the property configuration of the persistent object
        /// </summary>
        public IEnumerable<IStoreProperty> GetProperties();

        /// <summary>
        /// Mark whether the persistent object needs to be removed directly from the database
        /// If do not need to delete directly, the database provider may use soft deletion to process
        /// </summary>
        public void SetDirectDeletion(bool directDeletion);

        /// <summary>
        /// Add the ignored property information for the persistence object
        /// </summary>
        public void AddIgnoredMember(string propertyName);

        /// <summary>
        /// Get the ignored properties configuration of the persistent object
        /// </summary>
        public IEnumerable<string> GetIgnoredMembers();

        /// <summary>
        /// Add the filter of the persistent object at query time
        /// </summary>
        public void AddQueryFilter(LambdaExpression expression);

        /// <summary>
        /// Add the filter of the persistent object at query time
        /// </summary>
        public IEnumerable<LambdaExpression> GetQueryFilters();
    }
}
