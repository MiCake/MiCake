﻿using MiCake.Core.Data;
using MiCake.Core.Util;
using System;

namespace MiCake.DDD.Connector.Store.Configure
{
    public class StoreModelBuilder : IHasAccessor<IStoreModel>
    {
        private readonly IStoreModel _storeModel;

        IStoreModel IHasAccessor<IStoreModel>.AccessibleData => _storeModel;

        public StoreModelBuilder(IStoreModel storeModel)
        {
            CheckValue.NotNull(storeModel, nameof(storeModel));

            _storeModel = storeModel;
        }

        /// <summary>
        ///     Returns an object that can be used to configure a given entity type in the model.
        ///     If the entity type is not already part of the model, it will be added to the model.
        /// </summary>
        public virtual StoreEntityBuilder<TEntity> Entity<TEntity>()
            where TEntity : class
           => new(((StoreEntityType)_storeModel.AddStoreEntity(typeof(TEntity))));

        /// <summary>
        ///     Returns an object that can be used to configure a given entity type in the model.
        ///     If the entity type is not already part of the model, it will be added to the model.
        /// </summary>
        public virtual StoreEntityBuilder Entity(Type type)
           => new(((StoreEntityType)_storeModel.AddStoreEntity(CheckValue.NotNull(type, nameof(type)))));
    }
}
