using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.AspNetCore
{
    public class MiCakeAspNetOptions
    {
        public MiCakeAspNetUowOption UnitOfWork { get; set; }
    }

    /// <summary>
    /// Provides configuration for the MiCake UnitOfWork.
    /// </summary>
    public class MiCakeAspNetUowOption
    {
        /// <summary>
        /// Whether to enable unit of work automatically.
        /// Default is true.
        /// </summary>
        public bool IsAutoEnabled { get; set; } = true;
    }
}
