using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace MiCake.Util.Query.Dynamic
{
    /// <summary>
    /// Helper methods for building and manipulating LINQ expression trees.
    /// </summary>
    /// <remarks>
    /// This internal class provides utility methods for constructing expressions used in dynamic filtering.
    /// </remarks>
    internal static class ExpressionHelpers
    {
        /// <summary>
        /// Builds an expression for accessing a property, supporting nested properties.
        /// </summary>
        /// <param name="parameter">The parameter expression representing the target object.</param>
        /// <param name="propertyName">The property name or nested property path (e.g., "Address.City").</param>
        /// <returns>An expression that accesses the specified property.</returns>
        /// <exception cref="ArgumentNullException">Thrown when parameter is null.</exception>
        /// <exception cref="ArgumentException">Thrown when propertyName is null or empty.</exception>
        internal static Expression BuildNestedPropertyExpression(ParameterExpression parameter, string propertyName)
        {
            ArgumentNullException.ThrowIfNull(parameter);
            if (string.IsNullOrWhiteSpace(propertyName))
                throw new ArgumentException("Property name cannot be null or empty.", nameof(propertyName));

            Expression body = parameter;
            foreach (var member in propertyName.Split('.'))
            {
                body = FixReflectedType(Expression.PropertyOrField(body, member));
            }
            return body;
        }

        /// <summary>
        /// Combines two expressions using a comparison operator.
        /// </summary>
        /// <param name="left">The left operand expression.</param>
        /// <param name="right">The right operand expression.</param>
        /// <param name="operatorType">The comparison operator type.</param>
        /// <returns>An expression representing the comparison operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when left or right is null.</exception>
        internal static Expression ConcatExpressionsWithOperator(Expression left, Expression right, ValueOperatorType operatorType)
        {
            ArgumentNullException.ThrowIfNull(left);
            ArgumentNullException.ThrowIfNull(right);

            return operatorType switch
            {
                ValueOperatorType.Equal => Expression.Equal(left, right),
                ValueOperatorType.NotEqual => Expression.NotEqual(left, right),
                ValueOperatorType.LessThan => Expression.LessThan(left, right),
                ValueOperatorType.GreaterThan => Expression.GreaterThan(left, right),
                ValueOperatorType.LessThanOrEqual => Expression.LessThanOrEqual(left, right),
                ValueOperatorType.GreaterThanOrEqual => Expression.GreaterThanOrEqual(left, right),
                ValueOperatorType.In => CreateInExpression(left, right),
                ValueOperatorType.Contains => CreateStringMethodExpression(left, right, "Contains"),
                ValueOperatorType.StartsWith => CreateStringMethodExpression(left, right, "StartsWith"),
                ValueOperatorType.EndsWith => CreateStringMethodExpression(left, right, "EndsWith"),
                _ => Expression.Equal(left, right)
            };
        }

        /// <summary>
        /// Combines two expressions using a logical join operator (AND/OR).
        /// </summary>
        /// <param name="left">The left operand expression.</param>
        /// <param name="right">The right operand expression.</param>
        /// <param name="joinType">The join type (And/Or).</param>
        /// <returns>An expression representing the logical operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when left or right is null.</exception>
        internal static Expression ConcatExpressionsWithOperator(Expression left, Expression right, FilterJoinType joinType)
        {
            ArgumentNullException.ThrowIfNull(left);
            ArgumentNullException.ThrowIfNull(right);

            return joinType switch
            {
                FilterJoinType.And => Expression.AndAlso(left, right),
                FilterJoinType.Or => Expression.OrElse(left, right),
                _ => Expression.AndAlso(left, right)
            };
        }

        private static Expression CreateInExpression(Expression left, Expression right)
        {
            // Resolve element type (handle nullable left types)
            var elementType = Nullable.GetUnderlyingType(left.Type) ?? left.Type;

            // First try instance method: right.Contains(element)
            var containsMethod = right.Type.GetMethod("Contains", new[] { elementType });
            if (containsMethod != null)
                return Expression.Call(right, containsMethod, left);

            // Fallback to Enumerable.Contains<T>(IEnumerable<T>, T)
            var enumerableContains = typeof(Enumerable).GetMethods(BindingFlags.Static | BindingFlags.Public)
                .FirstOrDefault(m => m.Name == "Contains" && m.GetParameters().Length == 2);

            if (enumerableContains != null)
            {
                var generic = enumerableContains.MakeGenericMethod(elementType);
                return Expression.Call(generic, right, left);
            }

            throw new InvalidOperationException($"Contains method not found for type {right.Type}");
        }

        private static MethodCallExpression CreateStringMethodExpression(Expression left, Expression right, string methodName)
        {
            // ensure left operand is some form of string
            var leftUnderlying = Nullable.GetUnderlyingType(left.Type) ?? left.Type;
            if (leftUnderlying != typeof(string))
            {
                throw new InvalidOperationException($"Operator '{methodName}' is only supported on string properties.");
            }

            var method = typeof(string).GetMethod(methodName, new[] { typeof(string) });
            if (method == null)
                throw new InvalidOperationException($"{methodName} method not found for string type");

            return Expression.Call(left, method, right);
        }

        private static MemberExpression FixReflectedType(MemberExpression expression)
        {
            var member = expression.Member;
            var declaringType = member.DeclaringType;

            if (declaringType == null)
                return expression;

            if (member.ReflectedType == declaringType)
                return expression;

            return member.MemberType switch
            {
                MemberTypes.Property => Expression.Property(expression.Expression, declaringType, member.Name),
                MemberTypes.Field => Expression.Field(expression.Expression, declaringType, member.Name),
                _ => expression
            };
        }
    }
}
