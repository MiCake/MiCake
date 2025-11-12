using System;

namespace MiCake.Core
{
    /// <summary>
    /// Record the environment information of the current Mike app.
    /// </summary>
    internal class MiCakeEnvironment : IMiCakeEnvironment
    {
        /// <summary>
        /// The type of entry micake module.
        /// </summary>
        public Type EntryType { get; set; }

        public MiCakeEnvironment()
        {
        }
    }
}
