using System.Linq.Expressions;
using System.Reflection;

namespace MiCake.Cord.Storage.Infrastructure
{
    /// <summary>
    /// Base on Microsoft.EntityFrameworkCore
    /// </summary>
    internal static class ExpressionExtensions
    {
        /// <summary>
        ///     <para>
        ///         Gets the <see cref="PropertyInfo" /> represented by a simple property-access expression.
        ///     </para>
        ///     <para>
        ///         This method is typically used to parse property access lambdas from fluent APIs.
        ///     </para>
        /// </summary>
        /// <param name="propertyAccessExpression"> The expression. </param>
        /// <returns> The <see cref="PropertyInfo" />. </returns>
        internal static PropertyInfo GetPropertyAccess(this LambdaExpression propertyAccessExpression)
        {
            var parameterExpression = propertyAccessExpression.Parameters.Single();
            var propertyInfo = parameterExpression.MatchSimplePropertyAccess(propertyAccessExpression.Body);

            if (propertyInfo == null)
            {
                throw new ArgumentException($"The expression '{propertyAccessExpression}' is not a valid property expression." +
                    $"The expression should represent a simple property access: 't =&gt; t.MyProperty'");
            }

            var declaringType = propertyInfo.DeclaringType;
            var parameterType = parameterExpression.Type;

            if (declaringType != null
                && declaringType != parameterType
                && declaringType.GetTypeInfo().IsInterface
                && declaringType.GetTypeInfo().IsAssignableFrom(parameterType.GetTypeInfo()))
            {
                var propertyGetter = propertyInfo.GetMethod ?? throw new ArgumentNullException($"{propertyInfo.Name} has no getter");
                var interfaceMapping = parameterType.GetTypeInfo().GetRuntimeInterfaceMap(declaringType);
                var index = Array.FindIndex(interfaceMapping.InterfaceMethods, p => propertyGetter.Equals(p));
                var targetMethod = interfaceMapping.TargetMethods[index];
                foreach (var runtimeProperty in parameterType.GetRuntimeProperties())
                {
                    if (targetMethod.Equals(runtimeProperty.GetMethod))
                    {
                        return runtimeProperty;
                    }
                }
            }

            return propertyInfo;
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        internal static PropertyInfo? MatchSimplePropertyAccess(this Expression parameterExpression, Expression propertyAccessExpression)
        {
            var propertyInfos = parameterExpression.MatchPropertyAccess(propertyAccessExpression);

            return propertyInfos?.Count == 1 ? propertyInfos[0] : null;
        }

        private static IReadOnlyList<PropertyInfo>? MatchPropertyAccess(
            this Expression parameterExpression, Expression propertyAccessExpression)
        {
            var propertyInfos = new List<PropertyInfo>();

            MemberExpression memberExpression;

            do
            {
                memberExpression = (propertyAccessExpression!.RemoveConvert().RemoveTypeAs() as MemberExpression)!;

                if (memberExpression?.Member is not PropertyInfo propertyInfo)
                {
                    return null;
                }

                propertyInfos.Insert(0, propertyInfo);

                propertyAccessExpression = memberExpression.Expression!;
            }
            while (memberExpression.Expression!.RemoveConvert().RemoveTypeAs() != parameterExpression);

            return propertyInfos;
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        internal static Expression RemoveTypeAs(this Expression expression)
        {
            while (expression?.NodeType == ExpressionType.TypeAs)
            {
                expression = ((UnaryExpression)expression.RemoveConvert()).Operand;
            }

            return expression!;
        }

        /// <summary>
        ///     <para>
        ///         Returns a new expression with any see <see cref="ExpressionType.Convert" /> or
        ///         <see cref="ExpressionType.ConvertChecked" /> nodes removed from the head of the
        ///         given expression tree/
        ///     </para>
        ///     <para>
        ///         This method is typically used by database providers (and other extensions). It is generally
        ///         not used in application code.
        ///     </para>
        /// </summary>
        /// <param name="expression"> The expression. </param>
        /// <returns> A new expression with converts at the head removed. </returns>
        internal static Expression RemoveConvert(this Expression expression)
        {
            while (expression != null
                   && (expression.NodeType == ExpressionType.Convert
                       || expression.NodeType == ExpressionType.ConvertChecked))
            {
                expression = ((UnaryExpression)expression).Operand.RemoveConvert();
            }

            return expression!;
        }
    }
}
