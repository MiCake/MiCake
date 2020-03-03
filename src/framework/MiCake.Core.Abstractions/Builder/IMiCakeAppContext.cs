using MiCake.Core.Modularity;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace MiCake.Core.Builder
{
    public interface IMiCakeAppContext
    {
        /// <summary>
        /// <see cref="IMiCakeModuleManager"/>
        /// </summary>
        public IMiCakeModuleManager Modules { get; }

        /// <summary>
        /// <see cref="MiCakeApplicationOptions"/>
        /// </summary>
        public MiCakeApplicationOptions Options { get; }

        public IServiceCollection Services { get; set; }

        /// <summary>
        /// The entry module type.
        /// </summary>
        public Type EntryType { get; }
    }
}
