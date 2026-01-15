
using System;
using MiCake.Util.Cache;

namespace MiCake.DDD.Domain.Helper
{
    public static class DomainTypeHelper
    {
        // Cache flags for each evaluated type to avoid repeated reflection work.
        // Use a bounded LRU cache so a malicious or extreme workload does not cause unbounded memory growth.
        private static readonly BoundedLruCache<Type, DomainTypes> _typeFlagsCache = new(1024);

        public static bool IsDomainObject(Type type)
            => GetFlags(type) != DomainTypes.None;

        public static bool IsRepository(Type type)
            => GetFlags(type).HasFlag(DomainTypes.Repository);

        public static bool IsEntity(Type type)
            => GetFlags(type).HasFlag(DomainTypes.Entity);

        public static bool IsValueObject(Type type)
            => GetFlags(type).HasFlag(DomainTypes.ValueObject);

        public static bool IsAggregateRoot(Type type)
            => GetFlags(type).HasFlag(DomainTypes.AggregateRoot);

        public static bool IsDomainEvent(Type type)
            => GetFlags(type).HasFlag(DomainTypes.DomainEvent);

        public static bool IsDomainService(Type type)
            => GetFlags(type).HasFlag(DomainTypes.DomainService);

        private static DomainTypes GetFlags(Type type)
        {
            ArgumentNullException.ThrowIfNull(type);
            return _typeFlagsCache.GetOrAdd(type, ComputeFlags);
        }

        private static DomainTypes ComputeFlags(Type type)
        {
            DomainTypes flags = DomainTypes.None;

            if (typeof(IRepository).IsAssignableFrom(type))
                flags |= DomainTypes.Repository;

            if (typeof(IEntity).IsAssignableFrom(type))
                flags |= DomainTypes.Entity;

            if (typeof(IValueObject).IsAssignableFrom(type))
                flags |= DomainTypes.ValueObject;

            if (typeof(IAggregateRoot).IsAssignableFrom(type))
                flags |= DomainTypes.AggregateRoot;

            if (typeof(IDomainEvent).IsAssignableFrom(type))
                flags |= DomainTypes.DomainEvent;

            if (typeof(IDomainService).IsAssignableFrom(type))
                flags |= DomainTypes.DomainService;

            return flags;
        }

        [Flags]
        private enum DomainTypes : byte
        {
            None = 0,
            Repository = 1 << 0,
            Entity = 1 << 1,
            ValueObject = 1 << 2,
            AggregateRoot = 1 << 3,
            DomainEvent = 1 << 4,
            DomainService = 1 << 5
        }
    }
}
