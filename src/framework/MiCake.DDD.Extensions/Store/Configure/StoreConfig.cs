using MiCake.Core.Data;
using MiCake.Core.Util;
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace MiCake.DDD.Extensions.Store.Configure
{
    /// <summary>
    /// This is an internal API  not subject to the same compatibility standards as public APIs.
    /// It may be changed or removed without notice in any release.
    /// </summary>
    public class StoreConfig
    {
        /// <summary>
        /// The instance of <see cref="StoreConfig"/>.
        /// </summary>
        public static StoreConfig Instance { get; } = new StoreConfig();

        private const string StoreModelCacheKey = "StoreModelKey";

        private readonly ConcurrentDictionary<string, Lazy<IStoreModel>> _storeModels = new ConcurrentDictionary<string, Lazy<IStoreModel>>();
        private readonly ConcurrentDictionary<string, IStoreModelProvider> _modelProviders = new ConcurrentDictionary<string, IStoreModelProvider>();

        public StoreConfig()
        {
        }

        /// <summary>
        /// This is an internal API  not subject to the same compatibility standards as public APIs.
        /// It may be changed or removed without notice in any release.
        /// 
        /// Get the configured <see cref="IStoreModel"/>
        /// </summary>
        public virtual IStoreModel GetStoreModel()
        => _storeModels.GetOrAdd(StoreModelCacheKey, key => new Lazy<IStoreModel>(
               () => CreateModel(),
               LazyThreadSafetyMode.ExecutionAndPublication)).Value;

        private IStoreModel CreateModel()
        {
            StoreModelBuilder storeModelBuilder = new StoreModelBuilder(new StoreModel());

            foreach (var modelProvider in _modelProviders.Values)
            {
                modelProvider.Config(storeModelBuilder);
            }

            return storeModelBuilder.GetAccessor();
        }

        /// <summary>
        /// This is an internal API  not subject to the same compatibility standards as public APIs.
        /// It may be changed or removed without notice in any release.
        /// 
        /// Add <see cref="IStoreModelProvider"/> to this configer.
        /// </summary>
        public virtual StoreConfig AddModelProvider(IStoreModelProvider storeModelProvider)
        {
            CheckValue.NotNull(storeModelProvider, nameof(storeModelProvider));

            var key = storeModelProvider.GetType().FullName;
            _modelProviders.TryAdd(key, storeModelProvider);

            return this;
        }
    }
}
