using MiCake.Core;
using MiCake.Core.Handlers;
using System.Collections.Generic;

namespace MiCake.AspNetCore.Internal
{
    /// <summary>
    /// Indicates the context information that is needed in aspnet core by micake in the current HTTP request
    /// </summary>
    public interface IMiCakeCurrentRequestContext
    {
        /// <summary>
        /// MiCake handlers in current request.
        /// </summary>
        public List<MiCakeHandlerDescriptor> Handlers { get; }

        /// <summary>
        /// <see cref="IMiCakeEnvironment"/>
        /// </summary>
        public IMiCakeEnvironment MiCakeEnvironment { get; }
    }
}
