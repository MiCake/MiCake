using System;

namespace MiCake.Core
{
    /// <summary>
    /// Represents micake environmental information
    /// </summary>
    public interface IMiCakeEnvironment
    {
        /// <summary>
        /// Type of entry module
        /// </summary>
        public Type EntryType { get; set; }
    }
}
