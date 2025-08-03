using System;
using System.Linq.Expressions;
using System.Reflection;

namespace MiCake.Core.Util.LinqFilter
{
    internal static class ExpressionHelpers
    {
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

        internal static Expression ConcatExpressionsWithOperator(Expression left, Expression right, FilterOperatorType operatorType)
        {
            ArgumentNullException.ThrowIfNull(left);
            ArgumentNullException.ThrowIfNull(right);

            return operatorType switch
            {
                FilterOperatorType.Equal => Expression.Equal(left, right),
                FilterOperatorType.NotEqual => Expression.NotEqual(left, right),
                FilterOperatorType.LessThan => Expression.LessThan(left, right),
                FilterOperatorType.GreaterThan => Expression.GreaterThan(left, right),
                FilterOperatorType.LessThanOrEqual => Expression.LessThanOrEqual(left, right),
                FilterOperatorType.GreaterThanOrEqual => Expression.GreaterThanOrEqual(left, right),
                FilterOperatorType.In => CreateInExpression(left, right),
                FilterOperatorType.Contains => CreateStringMethodExpression(left, right, "Contains"),
                FilterOperatorType.StartsWith => CreateStringMethodExpression(left, right, "StartsWith"),
                FilterOperatorType.EndsWith => CreateStringMethodExpression(left, right, "EndsWith"),
                _ => Expression.Equal(left, right)
            };
        }

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
            var containsMethod = right.Type.GetMethod("Contains", [left.Type]);
            if (containsMethod == null)
                throw new InvalidOperationException($"Contains method not found for type {right.Type}");

            return Expression.Call(right, containsMethod, left);
        }

        private static MethodCallExpression CreateStringMethodExpression(Expression left, Expression right, string methodName)
        {
            var method = typeof(string).GetMethod(methodName, [typeof(string)]);
            if (method == null)
                throw new InvalidOperationException($"{methodName} method not found for string type");

            return Expression.Call(left, method, right);
        }

        private static MemberExpression FixReflectedType(MemberExpression expression)
        {
            var member = expression.Member;
            var declaringType = member.DeclaringType;

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
