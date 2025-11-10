using System;
using System.Collections.Generic;

namespace MiCake.Core.DependencyInjection
{
    /// <summary>
    /// Provides the default strategy for finding service types to register.
    /// The default strategy matches interfaces whose names contain the implementation class name.
    /// </summary>
    internal static class DefaultFindServiceTypes
    {
        /// <summary>
        /// Default finder that matches interfaces by name convention.
        /// An interface is matched if its name (case-insensitive) contains the type name.
        /// For example, class "OrderService" will match interface "IOrderService".
        /// </summary>
        public static FindAutoServiceTypesDelegate Finder = (type, interfaces) =>
        {
            var result = new List<Type>();
            var typeName = type.Name.ToUpperInvariant();

            foreach (var inheritInterface in interfaces)
            {
                var interfaceName = inheritInterface.Name.ToUpperInvariant();
                
                // Skip marker interfaces (ITransientService, IScopedService, ISingletonService, IAutoInjectService)
                if (interfaceName == "ITRANSIENTSERVICE" || 
                    interfaceName == "ISCOPEDSERVICE" || 
                    interfaceName == "ISINGLETONSERVICE" || 
                    interfaceName == "IAUTOINJECTSERVICE")
                {
                    continue;
                }

                // Match interfaces that start with 'I' and contain the type name
                if (interfaceName.StartsWith('I') && interfaceName.Contains(typeName))
                {
                    result.Add(inheritInterface);
                }
            }

            return result;
        };
    }
}
