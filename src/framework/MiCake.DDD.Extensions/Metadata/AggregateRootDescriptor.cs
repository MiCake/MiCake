using MiCake.Core.Util.Reflection;
using MiCake.DDD.Domain;
using MiCake.DDD.Domain.Helper;
using MiCake.DDD.Domain.Store;
using MiCake.DDD.Extensions.Store;
using System;
using System.Linq;

namespace MiCake.DDD.Extensions.Metadata
{
    /// <summary>
    /// Describes an <see cref="IAggregateRoot"/>
    /// </summary>
    public class AggregateRootDescriptor : EntityDescriptor
    {
        /// <summary>
        /// Is the aggregate root declared as <see cref="IHasStorageModel"/>
        /// </summary>
        public bool HasStorageModel { get; private set; }

        /// <summary>
        /// The storage model corresponding to the aggregate root
        /// </summary>
        public Type StorageModel { get; private set; }

        public AggregateRootDescriptor(Type type) : base(type)
        {
            HasStorageModel = EntityHelper.HasStorageModel(type);
        }

        internal void SetStorageModel(Type storageType)
        {
            if (!ReflectionHelper.IsAssignableToGenericType(storageType, typeof(StorageModel<>)))
                throw new ArgumentException($"The type {storageType.Name} is not implements/inherits {nameof(StorageModel)}.");

            var entityType = TypeHelper.GetGenericArguments(storageType, typeof(StorageModel<>)).FirstOrDefault();
            if (!Type.Equals(entityType))
                throw new ArgumentException($"The type {storageType.Name} generic parameter must be {Type.Name}.But now is {entityType?.Name}");

            StorageModel = storageType;
        }
    }
}
