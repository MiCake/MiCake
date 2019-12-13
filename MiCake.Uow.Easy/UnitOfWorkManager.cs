using MiCake.Core.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.Uow.Easy
{
    public class UnitOfWorkManager : IUnitOfWorkManager
    {
        private IUnitOfWork currentUow;
        private bool _isDisposed = false;

        private readonly IServiceProvider _serviceProvider;

        public UnitOfWorkManager(
            IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IUnitOfWork Create(UnitOfWorkOptions options)
        {
            // 暂不实现RequireNew等操作
            IUnitOfWork unitOfWork = null;

            //考虑是否需要一个IUnitOfFactory来保持依赖注入中对工作单元的创建
            unitOfWork = (IUnitOfWork)_serviceProvider.GetService(typeof(IUnitOfWork));
            currentUow = unitOfWork;

            return unitOfWork;
        }

        public IUnitOfWork GetCurrentUnitOfWork()
        {
            return currentUow;
        }

        public void Dispose()
        {
            if (_isDisposed)
                throw new MiCakeException("this manager is already disposed");

            _isDisposed = true;

            currentUow.Dispose();
        }
    }
}
