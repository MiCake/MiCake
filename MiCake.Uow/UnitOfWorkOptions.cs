using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.Uow
{

    public class UnitOfWorkOptions
    {
        /// <summary>
        /// Whether to enable unit of work automatically.
        /// Default is true.
        /// </summary>
        public bool IsAutoEnabled { get; set; } = true;

    }
}
