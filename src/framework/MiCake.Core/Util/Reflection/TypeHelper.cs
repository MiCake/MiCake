using System;
using System.Linq;
using System.Reflection;

namespace MiCake.Core.Util.Reflection
{
    /// <summary>
    /// Provides utility methods for type inspection and validation.
    /// </summary>
    public static class TypeHelper
    {
        /// <summary>
        /// Determines whether an object is a Func&lt;&gt; delegate.
        /// </summary>
        /// <param name="obj">The object to check</param>
        /// <returns>True if the object is a Func&lt;&gt; delegate; otherwise, false</returns>
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

        /// <summary>
        /// Determines whether an object is a Func&lt;TReturn&gt; delegate with a specific return type.
        /// </summary>
        /// <typeparam name="TReturn">The expected return type</typeparam>
        /// <param name="obj">The object to check</param>
        /// <returns>True if the object is a Func&lt;TReturn&gt; delegate; otherwise, false</returns>
        public static bool IsFunc<TReturn>(object obj)
        {
            return obj != null && obj.GetType() == typeof(Func<TReturn>);
        }

        /// <summary>
        /// Determines whether a type is a primitive type or an extended primitive type.
        /// Extended primitives include string, decimal, DateTime, DateTimeOffset, TimeSpan, and Guid.
        /// </summary>
        /// <param name="type">The type to check</param>
        /// <param name="includeNullables">Whether to consider nullable versions of primitives as primitives</param>
        /// <param name="includeEnums">Whether to consider enums as primitives</param>
        /// <returns>True if the type is a primitive or extended primitive; otherwise, false</returns>
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

        /// <summary>
        /// Gets the underlying type of a nullable type, or returns the type itself if it's not nullable.
        /// </summary>
        /// <param name="t">The type to unwrap</param>
        /// <returns>The underlying type if nullable; otherwise, the original type</returns>
        /// <example>
        /// <code>
        /// var type = typeof(int?).GetFirstGenericArgumentIfNullable();
        /// // Returns: typeof(int)
        /// </code>
        /// </example>
        public static Type GetFirstGenericArgumentIfNullable(this Type t)
        {
            if (t.GetGenericArguments().Length > 0 && t.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                return t.GetGenericArguments().FirstOrDefault();
            }

            return t;
        }

        /// <summary>
        /// Gets all generic type arguments from interfaces implemented by a type that match a specific generic type definition.
        /// </summary>
        /// <param name="type">The type whose interfaces to examine</param>
        /// <param name="genericType">The generic interface type definition to match (e.g., typeof(IRepository&lt;&gt;))</param>
        /// <returns>Array of generic type arguments from matching interfaces</returns>
        /// <example>
        /// <code>
        /// // For a type implementing IRepository&lt;Order&gt; and IRepository&lt;Customer&gt;
        /// var args = GetGenericArguments(type, typeof(IRepository&lt;&gt;));
        /// // Returns: [typeof(Order), typeof(Customer)]
        /// </code>
        /// </example>
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
        /// Gets the first generic interface implemented by a type that matches a specific generic type definition.
        /// </summary>
        /// <param name="type">The type whose interfaces to examine</param>
        /// <param name="genericType">The generic interface type definition to match</param>
        /// <returns>The matching generic interface type, or null if not found</returns>
        public static Type GetGenericInterface(Type type, Type genericType)
        {
            return type.GetInterfaces()
                            .Where(i => IsGenericType(i))
                            .FirstOrDefault();

            bool IsGenericType(Type type1)
                => type1.IsGenericType && type1.GetGenericTypeDefinition() == genericType;
        }

        /// <summary>
        /// Determines whether a type implements a specific generic interface.
        /// </summary>
        /// <param name="type">The type to check</param>
        /// <param name="generic">The generic interface to check for</param>
        /// <returns>True if the type implements the generic interface; otherwise, false</returns>
        public static bool IsImplementedGenericInterface(Type type, Type generic)
        {
            return type.GetInterfaces().Any(x => generic == (x.IsGenericType ? x.GetGenericTypeDefinition() : x));
        }

        /// <summary>
        /// Determines whether a type is concrete (not abstract and not an interface).
        /// </summary>
        /// <param name="type">The type to check</param>
        /// <returns>True if the type is concrete; otherwise, false</returns>
        public static bool IsConcrete(Type type)
        {
            return !type.GetTypeInfo().IsAbstract && !type.GetTypeInfo().IsInterface;
        }

        /// <summary>
        /// Determines whether a type is an open generic type (has unresolved generic parameters).
        /// </summary>
        /// <param name="type">The type to check</param>
        /// <returns>True if the type is an open generic; otherwise, false</returns>
        /// <example>
        /// <code>
        /// var isOpen = IsOpenGeneric(typeof(List&lt;&gt;)); // Returns true
        /// var isClosed = IsOpenGeneric(typeof(List&lt;int&gt;)); // Returns false
        /// </code>
        /// </example>
        public static bool IsOpenGeneric(Type type)
        {
            return type.GetTypeInfo().IsGenericTypeDefinition || type.GetTypeInfo().ContainsGenericParameters;
        }

        /// <summary>
        /// Determines whether a type inherits from a specific base type.
        /// </summary>
        /// <param name="givenType">The type to check</param>
        /// <param name="baseType">The base type to check for</param>
        /// <returns>True if givenType inherits from baseType; otherwise, false</returns>
        public static bool IsInheritedFrom(Type givenType, Type baseType)
        {
            var givenTypeInfo = givenType.GetTypeInfo();
            var baseTypeInfo = baseType.GetTypeInfo();

            while (givenTypeInfo.BaseType != null)
            {
                if (givenTypeInfo.BaseType.GetTypeInfo() == baseTypeInfo)
                {
                    return true;
                }

                givenTypeInfo = givenTypeInfo.BaseType.GetTypeInfo();
            }

            return false;
        }

        /// <summary>
        /// Internal method to check if a type is a primitive or extended primitive type.
        /// </summary>
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
