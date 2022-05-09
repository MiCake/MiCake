using System.Reflection;

namespace MiCake.Core.Util.Reflection
{
    public static class TypeHelper
    {
        public static bool IsFunc(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            var type = obj.GetType();
            if (!type.GetTypeInfo().IsGenericType)
            {
                return false;
            }

            return type.GetGenericTypeDefinition() == typeof(Func<>);
        }

        public static bool IsFunc<TReturn>(object obj)
        {
            return obj != null && obj.GetType() == typeof(Func<TReturn>);
        }

        public static bool IsPrimitiveExtended(Type type, bool includeNullables = true, bool includeEnums = false)
        {
            if (IsPrimitiveExtendedInternal(type, includeEnums))
            {
                return true;
            }

            if (includeNullables &&
                type.IsGenericType &&
                type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                return IsPrimitiveExtendedInternal(type.GenericTypeArguments[0], includeEnums);
            }

            return false;
        }

        public static Type? GetFirstGenericArgumentIfNullable(this Type t)
        {
            if (t.GetGenericArguments().Length > 0 && t.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                return t.GetGenericArguments().FirstOrDefault();
            }

            return t;
        }

        /// <summary>
        /// Get all parameters of the generic interface inherited by the type
        /// </summary>
        /// <param name="type">Inherited types</param>
        /// <param name="genericType">generic interface type</param>
        /// <returns>interface generic arguments</returns>
        public static Type[] GetGenericArguments(Type type, Type genericType)
        {
            return type.GetInterfaces()
                            .Where(i => IsGenericType(i))
                            .SelectMany(i => i.GetGenericArguments())
                            .ToArray();

            bool IsGenericType(Type type1)
                => type1.IsGenericType && type1.GetGenericTypeDefinition() == genericType;
        }

        /// <summary>
        /// Get generic interface inherited by the type
        /// </summary>
        /// <param name="type">Inherited types</param>
        /// <param name="genericType">generic interface type</param>
        /// <returns>Generic interface information for specific types</returns>
        public static Type? GetGenericInterface(Type type, Type genericType)
        {
            return type.GetInterfaces()
                            .Where(i => IsGenericType(i))
                            .FirstOrDefault();

            bool IsGenericType(Type type1)
                => type1.IsGenericType && type1.GetGenericTypeDefinition() == genericType;
        }

        public static bool IsImplementedGenericInterface(Type type, Type generic)
        {
            return type.GetInterfaces().Any(x => generic == (x.IsGenericType ? x.GetGenericTypeDefinition() : x));
        }

        public static bool IsConcrete(Type type)
        {
            return !type.GetTypeInfo().IsAbstract && !type.GetTypeInfo().IsInterface;
        }

        public static bool IsOpenGeneric(Type type)
        {
            return type.GetTypeInfo().IsGenericTypeDefinition || type.GetTypeInfo().ContainsGenericParameters;
        }

        private static bool IsPrimitiveExtendedInternal(Type type, bool includeEnums)
        {
            if (type.IsPrimitive)
            {
                return true;
            }

            if (includeEnums && type.IsEnum)
            {
                return true;
            }

            return type == typeof(string) ||
                   type == typeof(decimal) ||
                   type == typeof(DateTime) ||
                   type == typeof(DateTimeOffset) ||
                   type == typeof(TimeSpan) ||
                   type == typeof(Guid);
        }
    }
}
