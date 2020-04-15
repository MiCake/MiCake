using MiCake.DDD.Extensions.Store.Interpret;

namespace MiCake.EntityFrameworkCore.Interprets
{
    public class EFModelExpressionProvider
    {
        public virtual IStoreModelExpression GetExpression()
            => new DefaultEFStoreModelExpression(new EFExpressionOptions());
    }
}
