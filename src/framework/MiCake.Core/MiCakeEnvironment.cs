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
        public required Type EntryModuleType { get; set; }

        public MiCakeEnvironment()
        {
        }
    }
}
