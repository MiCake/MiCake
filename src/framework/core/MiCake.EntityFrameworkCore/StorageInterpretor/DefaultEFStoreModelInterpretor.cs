﻿using MiCake.Cord.Storage;
using MiCake.Cord.Storage.Interpretor;
using MiCake.Core.Util;
using MiCake.Core.Util.Reflection;
using Microsoft.EntityFrameworkCore;

namespace MiCake.EntityFrameworkCore.StorageInterpretor
{
    internal class DefaultEFStoreModelInterpretor : IStoreModelInterpretor
    {
        private readonly EFInterpretorOptions _options;

        public DefaultEFStoreModelInterpretor(EFInterpretorOptions options)
        {
            _options = options;
        }

        public virtual void Interpret(IStoreModel storeModel, ModelBuilder receiver)
        {
            CheckValue.NotNull(storeModel, nameof(storeModel));
            CheckValue.NotNull(receiver, nameof(receiver));

            var efEntities = receiver.Model.GetEntityTypes();
            var configEntities = storeModel.GetStoreEntities();

            foreach (var configEntity in configEntities)
            {
                if (configEntities is not IStoreEntityType storeEntity || configEntities is not IConventionStoreEntity conventionEntity)
                {
                    // todo: log some debug error info.
                    continue;
                }

                var clrType = storeEntity.ClrType;
                foreach (var efEntity in efEntities)
                {
                    var efClrType = efEntity.ClrType;

                    if (clrType.IsAssignableFrom(efClrType) || TypeHelper.IsImplementedGenericInterface(efClrType, clrType))
                    {
                        foreach (var configStrategy in _options.Strategies)
                        {
                            configStrategy.Config(receiver, conventionEntity, efClrType);
                        }
                    }
                }
            }
        }

        void IStoreModelInterpretor.Interpret<TReceiver>(IStoreModel storeModel, TReceiver receiver) => Interpret(storeModel, (receiver as ModelBuilder)!);

        public void Dispose()
        {
        }
    }
}
