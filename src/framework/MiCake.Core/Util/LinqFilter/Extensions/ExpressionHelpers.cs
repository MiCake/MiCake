#nullable disable warnings

﻿using System;
using System.Linq.Expressions;
using System.Reflection;

namespace MiCake.Util.LinqFilter
{
    internal static class ExpressionHelpers
    {
        internal static System.Linq.Expressions.Expression BuildNestedPropertyExpression(ParameterExpression parameter, string propertyName)
        {
            ArgumentNullException.ThrowIfNull(parameter);
            if (string.IsNullOrWhiteSpace(propertyName))
                throw new ArgumentException("Property name cannot be null or empty.", nameof(propertyName));

            System.Linq.Expressions.Expression body = parameter;
            foreach (var member in propertyName.Split('.'))
            {
                body = FixReflectedType(System.Linq.Expressions.Expression.PropertyOrField(body, member));
            }
            return body;
        }

        internal static System.Linq.Expressions.Expression ConcatExpressionsWithOperator(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, FilterOperatorType operatorType)
        {
            ArgumentNullException.ThrowIfNull(left);
            ArgumentNullException.ThrowIfNull(right);

            return operatorType switch
            {
                FilterOperatorType.Equal => System.Linq.Expressions.Expression.Equal(left, right),
                FilterOperatorType.NotEqual => System.Linq.Expressions.Expression.NotEqual(left, right),
                FilterOperatorType.LessThan => System.Linq.Expressions.Expression.LessThan(left, right),
                FilterOperatorType.GreaterThan => System.Linq.Expressions.Expression.GreaterThan(left, right),
                FilterOperatorType.LessThanOrEqual => System.Linq.Expressions.Expression.LessThanOrEqual(left, right),
                FilterOperatorType.GreaterThanOrEqual => System.Linq.Expressions.Expression.GreaterThanOrEqual(left, right),
                FilterOperatorType.In => CreateInExpression(left, right),
                FilterOperatorType.Contains => CreateStringMethodExpression(left, right, "Contains"),
                FilterOperatorType.StartsWith => CreateStringMethodExpression(left, right, "StartsWith"),
                FilterOperatorType.EndsWith => CreateStringMethodExpression(left, right, "EndsWith"),
                _ => System.Linq.Expressions.Expression.Equal(left, right)
            };
        }

        internal static System.Linq.Expressions.Expression ConcatExpressionsWithOperator(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, FilterJoinType joinType)
        {
            ArgumentNullException.ThrowIfNull(left);
            ArgumentNullException.ThrowIfNull(right);

            return joinType switch
            {
                FilterJoinType.And => System.Linq.Expressions.Expression.AndAlso(left, right),
                FilterJoinType.Or => System.Linq.Expressions.Expression.OrElse(left, right),
                _ => System.Linq.Expressions.Expression.AndAlso(left, right)
            };
        }

        private static System.Linq.Expressions.Expression CreateInExpression(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right)
        {
            var containsMethod = right.Type.GetMethod("Contains", [left.Type]);
            if (containsMethod == null)
                throw new InvalidOperationException($"Contains method not found for type {right.Type}");

            return System.Linq.Expressions.Expression.Call(right, containsMethod, left);
        }

        private static MethodCallExpression CreateStringMethodExpression(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, string methodName)
        {
            var method = typeof(string).GetMethod(methodName, [typeof(string)]);
            if (method == null)
                throw new InvalidOperationException($"{methodName} method not found for string type");

            return System.Linq.Expressions.Expression.Call(left, method, right);
        }

        private static MemberExpression FixReflectedType(MemberExpression expression)
        {
            var member = expression.Member;
            var declaringType = member.DeclaringType;

            if (member.ReflectedType == declaringType)
                return expression;

            return member.MemberType switch
            {
                MemberTypes.Property => System.Linq.Expressions.Expression.Property(expression.Expression, declaringType, member.Name),
                MemberTypes.Field => System.Linq.Expressions.Expression.Field(expression.Expression, declaringType, member.Name),
                _ => expression
            };
        }
    }
}
