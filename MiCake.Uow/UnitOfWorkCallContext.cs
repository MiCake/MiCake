using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.Uow
{
    /// <summary>
    /// This class holds the chain structure of all work units created by the UnitOfWorkManager.
    /// </summary>
    public class UnitOfWorkCallContext
    {
        private UowContextStruct _rootContext;
        private UowContextStruct _currentContext;

        public UnitOfWorkCallContext(IUnitOfWork root)
        {
            _rootContext = new UowContextStruct()
            {
                Parent = null,
                Current = root,
            };
            _currentContext = _rootContext;
        }

        public IUnitOfWork GetCurrentUow()
        {
            return _currentContext.Current;
        }

        /// <summary>
        /// Remove unit of work from tree
        /// </summary>
        public IUnitOfWork PopUnitOfWork()
        {
            _currentContext = _currentContext.Parent;

            return _currentContext.Current;
        }

        public void PushUnitOfWork(IUnitOfWork unitOfWork)
        {
            if (_currentContext == null)
                throw new ArgumentNullException("can not find current unit of work.");

            var newContext = new UowContextStruct()
            {
                Parent = _currentContext,
                Current = unitOfWork
            };

            _currentContext = newContext;
        }
    }

    internal class UowContextStruct
    {
        public UowContextStruct Parent { get; set; }

        public IUnitOfWork Current { get; set; }

        public UowContextStruct()
        {
        }
    }
}
