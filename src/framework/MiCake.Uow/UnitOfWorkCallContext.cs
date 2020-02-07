using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.Uow
{
    /// <summary>
    /// This class holds the chain structure of all work units created by the UnitOfWorkManager.
    /// </summary>
    internal class UnitOfWorkCallContext
    {
        private UowContextStruct _currentContext;

        public UnitOfWorkCallContext()
        {
        }

        public IUnitOfWork GetCurrentUow()
        {
            if (_currentContext == null)
                return null;

            return _currentContext.Current;
        }

        public IUnitOfWork GetUowByID(Guid id)
        {
            if (_currentContext == null)
                return null;

            IUnitOfWork result = null;

            var tempContext = _currentContext;
            while (tempContext.Parent == null || tempContext.Parent.Current != null)
            {
                if (tempContext.Current.ID.Equals(id))
                    return tempContext.Current;

                tempContext = tempContext.Parent;

                if (tempContext == null) return null;
            }

            return result;
        }

        /// <summary>
        /// get a previous unit of work.
        /// </summary>
        public IUnitOfWork PopUnitOfWork()
        {
            // if is root . it's parent is null.
            if (_currentContext.Parent == null)
            {
                _currentContext = null;
                return null;
            }

            _currentContext = _currentContext.Parent;

            return _currentContext.Current;
        }

        public void PushUnitOfWork(IUnitOfWork unitOfWork)
        {
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
