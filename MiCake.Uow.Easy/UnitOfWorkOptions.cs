using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace MiCake.Uow.Easy
{
    public class UnitOfWorkOptions
    {
        /// <summary>
        /// Default: false.
        /// </summary>
        public bool IsTransactional { get; set; }

        public IsolationLevel? IsolationLevel { get; set; }

        public TimeSpan? Timeout { get; set; }

        public UnitOfWorkOptions Clone()
        {
            return new UnitOfWorkOptions
            {
                IsTransactional = IsTransactional,
                IsolationLevel = IsolationLevel,
                Timeout = Timeout
            };
        }
    }
}
