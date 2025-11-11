using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MiCake.Util.Reflection
{
    /// <summary>
    /// Provides utility methods for common reflection operations.
    /// </summary>
    public static class ReflectionHelper
    {
        /// <summary>
        /// Checks whether a given type implements or inherits from a generic type definition.
        /// </summary>
        /// <param name="givenType">The type to check</param>
        /// <param name="genericType">The generic type definition to check for (e.g., typeof(IRepository&lt;&gt;))</param>
        /// <returns>True if givenType implements or inherits from genericType; otherwise, false</returns>
        /// <example>
        /// <code>
        /// // Check if OrderRepository implements IRepository&lt;&gt;
        /// bool implements = IsAssignableToGenericType(typeof(OrderRepository), typeof(IRepository&lt;&gt;));
        /// </code>
        /// </example>
        public static bool IsAssignableToGenericType(Type givenType, Type genericType)
        {
            var givenTypeInfo = givenType.GetTypeInfo();

            if (givenTypeInfo.IsGenericType && givenType.GetGenericTypeDefinition() == genericType)
            {
                return true;
            }

            foreach (var interfaceType in givenTypeInfo.GetInterfaces())
            {
                if (interfaceType.GetTypeInfo().IsGenericType && interfaceType.GetGenericTypeDefinition() == genericType)
                {
                    return true;
                }
            }

            if (givenTypeInfo.BaseType == null)
            {
                return false;
            }

            return IsAssignableToGenericType(givenTypeInfo.BaseType, genericType);
        }

        /// <summary>
        /// Gets all implemented generic types that match a specific generic type definition.
        /// </summary>
        /// <param name="givenType">The type to examine</param>
        /// <param name="genericType">The generic type definition to find implementations of</param>
        /// <returns>List of concrete generic types that the given type implements</returns>
        /// <example>
        /// <code>
        /// // Get all IRepository&lt;TEntity&gt; implementations
        /// var implementations = GetImplementedGenericTypes(typeof(OrderRepository), typeof(IRepository&lt;&gt;));
        /// // Returns: [IRepository&lt;Order&gt;, IRepository&lt;OrderLine&gt;, ...]
        /// </code>
        /// </example>
        public static List<Type> GetImplementedGenericTypes(Type givenType, Type genericType)
        {
            var result = new List<Type>();
            AddImplementedGenericTypes(result, givenType, genericType);
            return result;
        }

        /// <summary>
        /// Recursively adds implemented generic types to the result list.
        /// </summary>
        private static void AddImplementedGenericTypes(List<Type> result, Type givenType, Type genericType)
        {
            var givenTypeInfo = givenType.GetTypeInfo();

            if (givenTypeInfo.IsGenericType && givenType.GetGenericTypeDefinition() == genericType)
            {
                result.Add(givenType);
            }

            foreach (var interfaceType in givenTypeInfo.GetInterfaces())
            {
                if (interfaceType.GetTypeInfo().IsGenericType && interfaceType.GetGenericTypeDefinition() == genericType)
                {
                    result.Add(interfaceType);
                }
            }

            if (givenTypeInfo.BaseType == null)
            {
                return;
            }

            AddImplementedGenericTypes(result, givenTypeInfo.BaseType, genericType);
        }

        /// <summary>
        /// Gets a single attribute of the specified type from a member, including inherited attributes.
        /// Returns the default value if the attribute is not found.
        /// </summary>
        /// <typeparam name="TAttribute">The type of attribute to retrieve</typeparam>
        /// <param name="memberInfo">The member to get the attribute from</param>
        /// <param name="defaultValue">The default value to return if attribute is not found</param>
        /// <param name="inherit">Whether to search the member's inheritance chain for the attribute</param>
        /// <returns>The attribute instance, or defaultValue if not found</returns>
        public static TAttribute GetSingleAttributeOrDefault<TAttribute>(MemberInfo memberInfo, TAttribute defaultValue = default, bool inherit = true)
            where TAttribute : Attribute
        {
            // Get attribute on the member
            if (memberInfo.IsDefined(typeof(TAttribute), inherit))
            {
                return memberInfo.GetCustomAttributes(typeof(TAttribute), inherit).Cast<TAttribute>().First();
            }

            return defaultValue;
        }

        /// <summary>
        /// Gets a single attribute from either the member itself or its declaring type.
        /// Searches the member first, then the declaring type if not found on the member.
        /// </summary>
        /// <typeparam name="TAttribute">The type of attribute to retrieve</typeparam>
        /// <param name="memberInfo">The member to get the attribute from</param>
        /// <param name="defaultValue">The default value to return if attribute is not found</param>
        /// <param name="inherit">Whether to search inheritance chains</param>
        /// <returns>The attribute instance, or defaultValue if not found</returns>
        public static TAttribute GetSingleAttributeOfMemberOrDeclaringTypeOrDefault<TAttribute>(MemberInfo memberInfo, TAttribute defaultValue = default, bool inherit = true)
            where TAttribute : class
        {
            return memberInfo.GetCustomAttributes(true).OfType<TAttribute>().FirstOrDefault()
                   ?? memberInfo.DeclaringType?.GetTypeInfo().GetCustomAttributes(true).OfType<TAttribute>().FirstOrDefault()
                   ?? defaultValue;
        }

        /// <summary>
        /// Gets the value of a property by its full path from a given object.
        /// Supports nested properties using dot notation.
        /// </summary>
        /// <param name="obj">The object to get the value from</param>
        /// <param name="objectType">The type of the object</param>
        /// <param name="propertyPath">The property path (e.g., "Customer.Address.City")</param>
        /// <returns>The property value</returns>
        /// <example>
        /// <code>
        /// var city = GetValueByPath(order, typeof(Order), "Customer.Address.City");
        /// </code>
        /// </example>
        public static object GetValueByPath(object obj, Type objectType, string propertyPath)
        {
            var value = obj;
            var currentType = objectType;
            var objectPath = currentType.FullName;
            var absolutePropertyPath = propertyPath;
            
            if (absolutePropertyPath.StartsWith(objectPath))
            {
                absolutePropertyPath = absolutePropertyPath.Replace(objectPath + ".", "");
            }

            foreach (var propertyName in absolutePropertyPath.Split('.'))
            {
                var property = currentType.GetProperty(propertyName);
                value = property.GetValue(value, null);
                currentType = property.PropertyType;
            }

            return value;
        }

        /// <summary>
        /// Sets the value of a property by its full path on a given object.
        /// Supports nested properties using dot notation.
        /// </summary>
        /// <param name="obj">The object to set the value on</param>
        /// <param name="objectType">The type of the object</param>
        /// <param name="propertyPath">The property path (e.g., "Customer.Address.City")</param>
        /// <param name="value">The value to set</param>
        internal static void SetValueByPath(object obj, Type objectType, string propertyPath, object value)
        {
            var currentType = objectType;
            PropertyInfo property;
            var objectPath = currentType.FullName;
            var absolutePropertyPath = propertyPath;
            
            if (absolutePropertyPath.StartsWith(objectPath))
            {
                absolutePropertyPath = absolutePropertyPath.Replace(objectPath + ".", "");
            }

            var properties = absolutePropertyPath.Split('.');

            if (properties.Length == 1)
            {
                property = objectType.GetProperty(properties.First());
                property.SetValue(obj, value);
                return;
            }

            for (int i = 0; i < properties.Length - 1; i++)
            {
                property = currentType.GetProperty(properties[i]);
                obj = property.GetValue(obj, null);
                currentType = property.PropertyType;
            }

            property = currentType.GetProperty(properties.Last());
            property.SetValue(obj, value);
        }

        /// <summary>
        /// Gets all public constant values from a type, including constants from nested types.
        /// Recursively searches nested types up to a maximum depth.
        /// </summary>
        /// <param name="type">The type to get constants from</param>
        /// <returns>Array of constant values as strings</returns>
        public static string[] GetPublicConstantsRecursively(Type type)
        {
            const int maxRecursiveParameterValidationDepth = 8;

            var publicConstants = new List<string>();

            void Recursively(List<string> constants, Type targetType, int currentDepth)
            {
                if (currentDepth > maxRecursiveParameterValidationDepth)
                {
                    return;
                }

                constants.AddRange(targetType.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                    .Where(x => x.IsLiteral && !x.IsInitOnly)
                    .Select(x => x.GetValue(null).ToString()));

                var nestedTypes = targetType.GetNestedTypes(BindingFlags.Public);

                foreach (var nestedType in nestedTypes)
                {
                    Recursively(constants, nestedType, currentDepth + 1);
                }
            }

            Recursively(publicConstants, type, 1);

            return publicConstants.ToArray();
        }

        /// <summary>
        /// Gets all properties in a class type that are marked with a specific attribute.
        /// </summary>
        /// <param name="classType">The class type to search</param>
        /// <param name="attributeType">The attribute type to look for</param>
        /// <returns>Enumerable of property info objects that have the specified attribute</returns>
        /// <exception cref="ArgumentNullException">Thrown when classType or attributeType is null</exception>
        public static IEnumerable<PropertyInfo> GetHasAttributeProperties(Type classType, Type attributeType)
        {
            ArgumentNullException.ThrowIfNull(classType, nameof(classType));
            ArgumentNullException.ThrowIfNull(attributeType, nameof(attributeType));

            foreach (var property in classType.GetProperties())
            {
                if (property.GetCustomAttribute(attributeType) != null)
                    yield return property;
            }
        }
    }
}
