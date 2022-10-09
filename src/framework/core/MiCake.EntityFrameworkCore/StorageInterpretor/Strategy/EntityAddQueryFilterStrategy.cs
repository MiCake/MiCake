using MiCake.Cord.Storage.Internal;
using MiCake.Core.Util;
using MiCake.Core.Util.Expressions;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace MiCake.EntityFrameworkCore.StorageInterpretor.Strategy
{
    internal class EntityAddQueryFilterStrategy : IConfigModelBuilderStrategy
    {
        public ModelBuilder Config(ModelBuilder modelBuilder, StoreEntityType storeEntity, Type efModelType)
        {
            var filters = storeEntity.GetQueryFilters();

            if (!filters.Any())
                return modelBuilder;

            var entityBuilder = modelBuilder.Entity(efModelType);
            foreach (var filter in filters)
            {
                var efFilter = ConvertToEFCoreFilter(filter, efModelType);
                entityBuilder.HasQueryFilter(efFilter);
            }

            return modelBuilder;
        }

        private static LambdaExpression ConvertToEFCoreFilter(LambdaExpression originalExpression, Type efClrType)
        {
            CheckValue.NotNull(originalExpression, nameof(originalExpression));
            CheckValue.NotNull(efClrType, nameof(efClrType));

            var resultType = typeof(Func<,>).MakeGenericType(efClrType, typeof(bool));
            var parameter = Expression.Parameter(efClrType);
            var body = originalExpression.Body.Replace(originalExpression.Parameters[0], parameter);

            return Expression.Lambda(resultType, body!, parameter);
        }
    }
}
