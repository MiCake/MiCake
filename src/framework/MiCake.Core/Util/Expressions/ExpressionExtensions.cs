using System.Linq.Expressions;

namespace MiCake.Util.Expressions
{
    public static class ExpressionExtensions
    {
        public static Expression Replace(this Expression expression, Expression searchEx, Expression replaceEx)
        {
            return new ReplaceVisitor(searchEx, replaceEx).Visit(expression);
        }

        internal class ReplaceVisitor(Expression from, Expression to) : ExpressionVisitor
        {
            private readonly Expression from = from, to = to;

            public override Expression Visit(Expression node)
            {
                return node == from ? to : base.Visit(node);
            }
        }
    }
}
