using MiCake.Core.Data;
using MiCake.Core.Util;
using System.Collections.Concurrent;

namespace MiCake.Cord.Storage
{
    /// <summary>
    /// This is an internal API  not subject to the same compatibility standards as public APIs.
    /// It may be changed or removed without notice in any release.
    /// </summary>
    public class StoreConfig : IDisposable
    {
        private const string StoreModelCacheKey = "MiCake.StoreModel.Key";

        private readonly ConcurrentDictionary<string, Lazy<IStoreModel>> _storeModels = new();
        private readonly ConcurrentDictionary<string, IStoreModelProvider> _modelProviders = new();

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
            StoreModelBuilder storeModelBuilder = new(new StoreModel());

            foreach (var modelProvider in _modelProviders.Values)
            {
                modelProvider.Config(storeModelBuilder);
            }

            return storeModelBuilder.GetAccessor();
        }

        /// <summary>
        /// This is an internal API not subject to the same compatibility standards as public APIs.
        /// It may be changed or removed without notice in any release.
        /// 
        /// Add <see cref="IStoreModelProvider"/> to this configer.
        /// </summary>
        public virtual StoreConfig AddModelProvider(IStoreModelProvider storeModelProvider)
        {
            CheckValue.NotNull(storeModelProvider, nameof(storeModelProvider));

            var key = storeModelProvider.GetType().FullName ?? throw new InvalidOperationException($"Can not get the full name of {storeModelProvider.GetType()}");
            _modelProviders.TryAdd(key, storeModelProvider);

            return this;
        }

        public void Dispose()
        {
            _storeModels.Clear();
            _modelProviders.Clear();
        }
    }
}
