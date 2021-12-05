using MiCake.Core;
using MiCake.Core.Handlers;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;

namespace MiCake.AspNetCore.Internal
{
    internal class MiCakeCurrentRequestContext : IMiCakeCurrentRequestContext
    {
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public List<MiCakeHandlerDescriptor> Handlers { get; private set; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public IMiCakeEnvironment MiCakeEnvironment { get; private set; }

        public MiCakeCurrentRequestContext(
            IServiceProvider serviceProvider,
            IMiCakeEnvironment environment,
            IOptions<MiCakeApplicationOptions> options)
        {
            MiCakeEnvironment = environment;

            var micakeHandlers = options.Value?.Handlers;
            if (micakeHandlers != null && micakeHandlers.Count != 0)
                Handlers = MiCakeHandlerFactory.CreateHandlerDescriptors(serviceProvider, micakeHandlers);
        }
    }
}
