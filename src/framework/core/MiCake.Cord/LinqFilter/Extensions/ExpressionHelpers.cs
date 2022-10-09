using System.Linq.Expressions;
using System.Reflection;

namespace MiCake.Cord.LinqFilter.Extensions
{
    public static class ExpressionHelpers
    {
        public static Expression BuildNestedPropertyExpression(ParameterExpression pe, string propertyName)
        {
            Expression body = pe;
            foreach (var member in propertyName.Split('.'))
            {
                body = FixReflectedType(Expression.PropertyOrField(body, member));
            }
            return body;
        }

        public static Expression ConcatExpressionsWithOperator(Expression left, Expression right, FilterOperatorType expressionOperator)
        {
            switch (expressionOperator)
            {
                case FilterOperatorType.Equal:
                    return Expression.Equal(left, right);
                case FilterOperatorType.NotEqual:
                    return Expression.NotEqual(left, right);
                case FilterOperatorType.LessThan:
                    return Expression.LessThan(left, right);
                case FilterOperatorType.GreaterThan:
                    return Expression.GreaterThan(left, right);
                case FilterOperatorType.LessThanOrEqual:
                    return Expression.LessThanOrEqual(left, right);
                case FilterOperatorType.GreaterThanOrEqual:
                    return Expression.GreaterThanOrEqual(left, right);
                case FilterOperatorType.In:
                    {
                        MethodInfo method = right.Type.GetMethod("Contains", new[] { typeof(int) }) ?? throw new Exception("Not found method Contains");
                        return Expression.Call(right, method, left);
                    }
                case FilterOperatorType.Contains:
                    {
                        MethodInfo method = typeof(string).GetMethod("Contains", new[] { typeof(string) }) ?? throw new Exception("Not found method Contains");
                        return Expression.Call(left, method, right);
                    }
                case FilterOperatorType.StartsWith:
                    {
                        MethodInfo method = typeof(string).GetMethod("StartsWith", new[] { typeof(string) }) ?? throw new Exception("Not found method StartsWith");
                        return Expression.Call(left, method, right);
                    }
                case FilterOperatorType.EndsWith:
                    {
                        MethodInfo method = typeof(string).GetMethod("EndsWith", new[] { typeof(string) }) ?? throw new Exception("Not found method EndsWith");
                        return Expression.Call(left, method, right);
                    }
                default:
                    return Expression.Equal(left, right);
            }
        }

        public static Expression ConcatExpressionsWithOperator(Expression left, Expression right, FilterJoinType filterJoinType)
        {
            switch (filterJoinType)
            {
                case FilterJoinType.And:
                    return Expression.And(left, right);
                case FilterJoinType.Or:
                    return Expression.OrElse(left, right);
                default:
                    return Expression.And(left, right);
            }
        }

        private static MemberExpression FixReflectedType(MemberExpression expr)
        {
            var member = expr.Member;
            var declaringType = member.DeclaringType ?? throw new Exception("Not found declaring type");

            if (member.ReflectedType != declaringType)
            {
                switch (member.MemberType)
                {
                    case MemberTypes.Property:
                        return Expression.Property(expr.Expression, declaringType, member.Name);
                    case MemberTypes.Field:
                        return Expression.Field(expr.Expression, declaringType, member.Name);
                }
            }

            return expr;
        }
    }
}
