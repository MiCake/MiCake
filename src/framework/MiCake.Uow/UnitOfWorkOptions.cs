using System;
using System.Data;

namespace MiCake.Uow
{
    public class UnitOfWorkOptions
    {
        public IsolationLevel? IsolationLevel { get; set; }

        public TimeSpan? Timeout { get; set; }

        public UnitOfWorkLimit Limit { get; set; }

        public UnitOfWorkOptions() : this(default)
        {
        }

        public UnitOfWorkOptions(IsolationLevel? isolationLevel) :
            this(isolationLevel, null)
        {
        }

        public UnitOfWorkOptions(IsolationLevel? isolationLevel, TimeSpan? timeOut)
            : this(isolationLevel, timeOut, UnitOfWorkLimit.Required)
        {
        }

        public UnitOfWorkOptions(IsolationLevel? isolationLevel, TimeSpan? timeOut, UnitOfWorkLimit unitOfWorkLimit)
        {
            IsolationLevel = isolationLevel;
            Timeout = timeOut;
            Limit = unitOfWorkLimit;
        }

        public UnitOfWorkOptions Clone()
        {
            return new UnitOfWorkOptions(IsolationLevel, Timeout, Limit);
        }
    }
}
