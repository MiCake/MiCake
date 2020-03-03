using JetBrains.Annotations;
using System;

namespace MiCake.DDD.Domain.Helper
{
    public static class DomianTypeHelper
    {
        public static bool IsRepository([NotNull]Type type)
        {
            return typeof(IRepository).IsAssignableFrom(type);
        }

        public static bool IsEntity([NotNull] Type type)
        {
            return typeof(IEntity).IsAssignableFrom(type);
        }

        public static bool IsAggregateRoot([NotNull] Type type)
        {
            return typeof(IAggregateRoot).IsAssignableFrom(type);
        }

        public static bool IsDomainEvent([NotNull] Type type)
        {
            return typeof(IDomainEvent).IsAssignableFrom(type);
        }

        public static bool IsDomainService([NotNull] Type type)
        {
            return typeof(IDomainService).IsAssignableFrom(type);
        }

        public static bool IsValueObject([NotNull] Type type)
        {
            return typeof(IValueObject).IsAssignableFrom(type);
        }
    }
}
