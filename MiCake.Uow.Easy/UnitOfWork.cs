using MiCake.Core.Abstractions;
using MiCake.Core.Util.Collections;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.Uow.Easy
{
    public class UnitOfWork : IUnitOfWork
    {
        public Guid ID => Guid.NewGuid();

        /// <summary>
        /// 具有事务功能的数据访问容器。比如在EF里面，具有事务操纵的应该是DbContext
        /// </summary>
        private Dictionary<string, ITranscationFeature> _transcationContainer;

        private bool _isRollback = false;
        private bool _isCommit = false;
        private bool _isDisposed = false;

        public UnitOfWork()
        {
            _transcationContainer = new Dictionary<string, ITranscationFeature>();
        }

        public void Rollback()
        {
            if (_isRollback)
                throw new MiCakeException("Can not rollback in this unitofwork");

            _isRollback = true;

            foreach (var transcationFeature in _transcationContainer)
            {
                transcationFeature.Value.Rollback();
            }
        }

        public async Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            if (_isRollback)
                throw new MiCakeException("Can not rollback in this unitofwork");

            _isRollback = true;

            foreach (var transcationFeature in _transcationContainer)
            {
                await transcationFeature.Value.RollbackAsync();
            }
        }

        public void SaveChanges()
        {
            if (_isCommit)
                throw new MiCakeException("Can not commit in this unitofwork");

            _isCommit = true;

            foreach (var transcationFeature in _transcationContainer)
            {
                transcationFeature.Value.Commit();
            }
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            if (_isCommit)
                throw new MiCakeException("Can not commit in this unitofwork");

            _isCommit = true;

            foreach (var transcationFeature in _transcationContainer)
            {
                await transcationFeature.Value.CommitAsync();
            }
        }

        public void ResigtedTranscationFeature(string key, ITranscationFeature transcationFeature)
        {
            _transcationContainer.AddIfNotContains(new KeyValuePair<string, ITranscationFeature>(key, transcationFeature));
        }

        public ITranscationFeature GetOrAddTranscationFeature(string key, ITranscationFeature transcationFeature)
        {
            if (_transcationContainer.AddIfNotContains(new KeyValuePair<string, ITranscationFeature>(key, transcationFeature)))
                return transcationFeature;

            return default;
        }

        public ITranscationFeature GetTranscationFeature(string key)
        {
            ITranscationFeature result = null;
            if (_transcationContainer.TryGetValue(key, out result))
            {
                return result;
            }

            return default;
        }

        public void RemoveTranscation(string key)
        {
            _transcationContainer.Remove(key);
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;

            try
            {
                foreach (var transcationFeature in _transcationContainer)
                {
                    transcationFeature.Value.Dispose();
                }
            }
            catch
            {
                //   throw 
            }
        }
    }
}
