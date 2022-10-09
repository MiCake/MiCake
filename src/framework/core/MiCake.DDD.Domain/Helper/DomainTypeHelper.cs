namespace MiCake.DDD.Domain.Helper
{
    public static class DomainTypeHelper
    {
        public readonly static List<Type> DomainTypes = new()
        {
            typeof(IRepository),
            typeof(IEntity),
            typeof(IValueObject),
            typeof(IAggregateRoot),
            typeof(IDomainEvent),
            typeof(IDomainService),
        };

        public static bool IsDomainObject(Type type)
            => DomainTypes.Any(s => s.IsAssignableFrom(type));

        public static bool IsRepository(Type type)
            => typeof(IRepository).IsAssignableFrom(type);

        public static bool IsEntity(Type type)
            => typeof(IEntity).IsAssignableFrom(type);

        public static bool IsValueObject(Type type)
            => typeof(IValueObject).IsAssignableFrom(type);

        public static bool IsAggregateRoot(Type type)
            => typeof(IAggregateRoot).IsAssignableFrom(type);

        public static bool IsDomainEvent(Type type)
            => typeof(IDomainEvent).IsAssignableFrom(type);

        public static bool IsDomainService(Type type)
            => typeof(IDomainService).IsAssignableFrom(type);
    }
}
