using MiCake.Core.Util.Collections;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MiCake.Core.DependencyInjection
{
    /// <summary>
    /// Marks a class for automatic dependency injection registration.
    /// This attribute allows explicit control over service registration including 
    /// service types, lifetime, and registration behavior.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class InjectServiceAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the service types (typically interfaces) to register for this implementation.
        /// If not specified and <see cref="IncludeSelf"/> is true, the class will be registered as itself.
        /// </summary>
        public Type[] ServiceTypes { get; set; }

        /// <summary>
        /// Gets or sets the service lifetime for this registration.
        /// Default is <see cref="MiCakeServiceLifetime.Transient"/>.
        /// </summary>
        public MiCakeServiceLifetime Lifetime { get; set; } = MiCakeServiceLifetime.Transient;

        /// <summary>
        /// Gets or sets whether to include the class itself as a service type in addition to <see cref="ServiceTypes"/>.
        /// Default is true.
        /// </summary>
        public bool IncludeSelf { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to only register if the service type hasn't already been registered.
        /// When true, existing registrations will not be replaced.
        /// Default is false.
        /// </summary>
        public bool TryRegister { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to replace existing service registrations.
        /// When true, the first service in the collection with the same service type will be removed 
        /// and this registration will be added.
        /// Default is false.
        /// </summary>
        public bool ReplaceServices { get; set; } = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="InjectServiceAttribute"/> class.
        /// </summary>
        /// <param name="serviceTypes">The service types to register</param>
        public InjectServiceAttribute(params Type[] serviceTypes)
        {
            ServiceTypes = serviceTypes;
        }

        /// <summary>
        /// Gets all service types to register, including the implementation type itself if <see cref="IncludeSelf"/> is true.
        /// </summary>
        /// <param name="itself">The implementation type</param>
        /// <returns>List of all service types to register</returns>
        public List<Type> GetServiceTypes(Type itself)
        {
            var serviceTypes = ServiceTypes == null ? new List<Type>() : ServiceTypes.AsEnumerable().ToList();

            if (IncludeSelf)
                serviceTypes.AddIfNotContains(itself);

            return serviceTypes;
        }
    }
}
