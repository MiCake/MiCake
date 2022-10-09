using System.Linq.Expressions;

namespace MiCake.Core.Util.Expressions
{
    public static class ExpressionExtensions
    {
        public static Expression? Replace(this Expression expression, Expression searchEx, Expression replaceEx)
        {
            return new ReplaceVisitor(searchEx, replaceEx).Visit(expression);
        }

        internal class ReplaceVisitor : ExpressionVisitor
        {
            private readonly Expression from, to;

            public ReplaceVisitor(Expression from, Expression to)
            {
                this.from = from;
                this.to = to;
            }

            public override Expression? Visit(Expression? node)
            {
                return node == from ? to : base.Visit(node);
            }
        }
    }
}
