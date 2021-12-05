using System;
using System.Collections.Generic;

namespace MiCake.Core.DependencyInjection
{
    internal static class DefaultFindServiceTypes
    {
        public static FindAutoServiceTypesDelegate Finder = (type, interfaces) =>
        {
            var result = new List<Type>();

            var typeName = type.Name.ToUpper();
            foreach (var inhertInterface in interfaces)
            {
                var interfaceName = inhertInterface.Name.ToUpper();
                if (interfaceName.StartsWith("I"))
                {
                    if (interfaceName.Contains(typeName))
                        result.Add(inhertInterface);
                }
            }

            return result;
        };
    }
}
