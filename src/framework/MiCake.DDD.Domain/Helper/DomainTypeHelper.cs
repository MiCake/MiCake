using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MiCake.DDD.Domain.Helper
{
    public static class DomainTypeHelper
    {
        public static List<Type> DomainTypes = new List<Type>()
        {
            typeof(IRepository),
            typeof(IEntity),
            typeof(IValueObject),
            typeof(IAggregateRoot),
            typeof(IDomainEvent),
            typeof(IDomainService),
        };

        public static bool IsDomainObject([NotNull]Type type)
            => DomainTypes.Any(s => s.IsAssignableFrom(type));

        public static bool IsRepository([NotNull]Type type)
            => typeof(IRepository).IsAssignableFrom(type);

        public static bool IsEntity([NotNull] Type type)
            => typeof(IEntity).IsAssignableFrom(type);

        public static bool IsValueObject([NotNull] Type type)
            => typeof(IValueObject).IsAssignableFrom(type);

        public static bool IsAggregateRoot([NotNull] Type type)
            => typeof(IAggregateRoot).IsAssignableFrom(type);

        public static bool IsDomainEvent([NotNull] Type type)
            => typeof(IDomainEvent).IsAssignableFrom(type);

        public static bool IsDomainService([NotNull] Type type)
            => typeof(IDomainService).IsAssignableFrom(type);
    }
}
