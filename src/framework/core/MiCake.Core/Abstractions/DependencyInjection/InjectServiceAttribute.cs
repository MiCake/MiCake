using MiCake.Core.Util.Collections;

namespace MiCake.Core.DependencyInjection
{
    /// <summary>
    /// Mark that the class is injected into the dependency injection framework
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class InjectServiceAttribute : Attribute
    {
        /// <summary>
        /// The service types
        /// </summary>
        public Type[] ServiceTypes { get; set; }

        /// <summary>
        /// service life time.<see cref=" MiCakeServiceLifetime"/>
        /// </summary>
        public MiCakeServiceLifetime Lifetime { get; set; } = MiCakeServiceLifetime.Transient;

        /// <summary>
        /// Add itself type to the collection.
        /// </summary>
        public bool IncludeSelf { get; set; } = true;

        /// <summary>
        /// Adds the specified descriptor to the collection if the service type hasn't already been registered.
        /// </summary>
        public bool TryRegister { get; set; } = false;

        /// <summary>
        ///  Removes the first service in ServiceCollection
        ///  with the same service type as descriptor and adds descriptor to the collection.
        /// </summary>
        public bool ReplaceServices { get; set; } = false;

        public InjectServiceAttribute(params Type[] serviceTypes)
        {
            ServiceTypes = serviceTypes;
        }

        public List<Type> GetServiceTypes(Type itself)
        {
            var serviceTypes = ServiceTypes == null ? new List<Type>() : ServiceTypes.AsEnumerable().ToList();

            if (IncludeSelf)
                serviceTypes.AddIfNotContains(itself);

            return serviceTypes;
        }
    }
}
