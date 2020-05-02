using System;
using System.Collections.Concurrent;

namespace MiCake.Core.Data
{
    /// <summary>
    /// Type used to store transient data.
    /// Can release data by <see cref="Release"/> method.
    /// </summary>
    public class DataDepositPool : IDisposable
    {
        private bool _isDispose = false;
        private ConcurrentDictionary<string, object> _cachePool = new ConcurrentDictionary<string, object>();

        public DataDepositPool()
        {
        }

        /// <summary>
        /// Get the stored data according to the key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public object TakeOut(string key)
        {
            if (!_cachePool.TryGetValue(key, out var reslut))
                return default;

            return reslut;
        }

        /// <summary>
        /// Deposit required data.
        /// </summary>
        /// <param name="dataInfo">data infomation</param>
        public void Deposit(string key, object dataInfo)
        {
            if (_cachePool.TryGetValue(key, out var reslut))
            {
                throw new InvalidOperationException($"The key:{key} has already add in {nameof(DataDepositPool)},result is :{reslut.ToString()}");
            }

            _cachePool.TryAdd(key, dataInfo);
        }

        public void Release()
        {
            _cachePool.Clear();
        }

        void IDisposable.Dispose()
        {
            if (_isDispose)
                throw new InvalidOperationException($"{nameof(DataDepositPool)} has already dispose.");

            _isDispose = true;

            Release();
        }
    }
}
