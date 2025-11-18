
using System;
using MiCake.Util.Cache;

namespace MiCake.DDD.Domain.Helper
{
    public static class DomainTypeHelper
    {
        // Cache flags for each evaluated type to avoid repeated reflection work.
        // Use a bounded LRU cache so a malicious or extreme workload does not cause unbounded memory growth.
        private static readonly BoundedLruCache<Type, DomainTypeFlags> _typeFlagsCache = new(1024);

        public static bool IsDomainObject(Type type)
            => GetFlags(type) != DomainTypeFlags.None;

        public static bool IsRepository(Type type)
            => GetFlags(type).HasFlag(DomainTypeFlags.Repository);

        public static bool IsEntity(Type type)
            => GetFlags(type).HasFlag(DomainTypeFlags.Entity);

        public static bool IsValueObject(Type type)
            => GetFlags(type).HasFlag(DomainTypeFlags.ValueObject);

        public static bool IsAggregateRoot(Type type)
            => GetFlags(type).HasFlag(DomainTypeFlags.AggregateRoot);

        public static bool IsDomainEvent(Type type)
            => GetFlags(type).HasFlag(DomainTypeFlags.DomainEvent);

        public static bool IsDomainService(Type type)
            => GetFlags(type).HasFlag(DomainTypeFlags.DomainService);

        private static DomainTypeFlags GetFlags(Type type)
        {
            ArgumentNullException.ThrowIfNull(type);
            return _typeFlagsCache.GetOrAdd(type, ComputeFlags);
        }

        private static DomainTypeFlags ComputeFlags(Type type)
        {
            DomainTypeFlags flags = DomainTypeFlags.None;

            if (typeof(IRepository).IsAssignableFrom(type))
                flags |= DomainTypeFlags.Repository;

            if (typeof(IEntity).IsAssignableFrom(type))
                flags |= DomainTypeFlags.Entity;

            if (typeof(IValueObject).IsAssignableFrom(type))
                flags |= DomainTypeFlags.ValueObject;

            if (typeof(IAggregateRoot).IsAssignableFrom(type))
                flags |= DomainTypeFlags.AggregateRoot;

            if (typeof(IDomainEvent).IsAssignableFrom(type))
                flags |= DomainTypeFlags.DomainEvent;

            if (typeof(IDomainService).IsAssignableFrom(type))
                flags |= DomainTypeFlags.DomainService;

            return flags;
        }

        [Flags]
        private enum DomainTypeFlags : byte
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
