﻿using MiCake.DDD.Extensions.Store.Configure;

namespace MiCake.DDD.Tests.Store
{
    public abstract class StoreConfigTestBase
    {
        protected static IStoreModel CreateModel() => new StoreModel();

        protected StoreModelBuilder CreateStoreModelBuilder() => new(CreateModel());
    }
}
