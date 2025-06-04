using System.Linq;
using MiCake.Core.Util;
using MiCake.Core.Util.Reflection;
using MiCake.DDD.Extensions.Store.Configure;
using MiCake.DDD.Extensions.Store.Interpret;
using MiCake.EntityFrameworkCore.Interprets.Strategies;
using Microsoft.EntityFrameworkCore;

namespace MiCake.EntityFrameworkCore.Interprets
{
    internal class DefaultEFStoreModelExpression : IStoreModelExpression
    {
        private readonly EFExpressionOptions _options;

        public DefaultEFStoreModelExpression(EFExpressionOptions options)
        {
            _options = options.AddCoreStrategies();
        }

        public virtual void Interpret(IStoreModel storeModel, ModelBuilder receiver)
        {
            CheckValue.NotNull(storeModel, nameof(storeModel));
            CheckValue.NotNull(receiver, nameof(receiver));

            var efEntities = receiver.Model.GetEntityTypes();
            var configEntities = storeModel.GetStoreEntities();

            foreach (StoreEntityType configEntity in configEntities.Cast<StoreEntityType>())
            {
                var clrType = configEntity.ClrType;

                foreach (var efEntity in efEntities)
                {
                    var efClrType = efEntity.ClrType;

                    if (clrType.IsAssignableFrom(efClrType) || TypeHelper.IsImplementedGenericInterface(efClrType, clrType))
                    {
                        foreach (var configStrategy in _options.Strategies)
                        {
                            configStrategy.Config(receiver, configEntity, efClrType);
                        }
                    }
                }
            }
        }

        void IStoreModelExpression.Interpret<TReceiver>(IStoreModel storeModel, TReceiver receiver)
            => Interpret(storeModel, receiver as ModelBuilder);
    }
}
