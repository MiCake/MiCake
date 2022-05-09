using System.Collections.Concurrent;

namespace MiCake.Core.Data
{
    /// <summary>
    /// Use to store transient data.
    /// </summary>
    public class MiCakeTransientData : IDisposable
    {
        private bool _isDispose = false;
        private readonly ConcurrentDictionary<string, object> _cachePool = new();

        public MiCakeTransientData()
        {
        }

        /// <summary>
        /// Get the stored data according to the key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public object? TakeOut(string key)
        {
            if (!_cachePool.TryGetValue(key, out var reslut))
                return default;

            return reslut;
        }

        /// <summary>
        /// Deposit required data.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="dataInfo">data infomation</param>
        public void Deposit(string key, object dataInfo)
        {
            if (_cachePool.TryGetValue(key, out var reslut))
            {
                throw new InvalidOperationException($"The key:{key} has already add in {nameof(MiCakeTransientData)},result is :{reslut}");
            }

            _cachePool.TryAdd(key, dataInfo);
        }

        public void Release()
        {
            _cachePool.Clear();
        }

        public void Dispose()
        {
            if (_isDispose)
                throw new InvalidOperationException($"{nameof(MiCakeTransientData)} has already dispose.");

            _isDispose = true;
            Release();
        }
    }
}
