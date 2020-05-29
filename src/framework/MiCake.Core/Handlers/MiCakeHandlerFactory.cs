using MiCake.Core.Util;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MiCake.Core.Handlers
{
    public static class MiCakeHandlerFactory
    {
        /// <summary>
        /// Created handler descriptors according to <see cref="IServiceProvider"/> and <see cref="IMiCakeHandler"/> to be created
        /// </summary>
        /// <param name="serviceProvider">The <see cref="IServiceProvider"/> who is the container for <see cref="IMiCakeHandler"/></param>
        /// <param name="handlers">all <see cref="IMiCakeHandler"/></param>
        /// <returns>All handlers descriptor</returns>
        public static List<MiCakeHandlerDescriptor> CreateHandlerDescriptors(IServiceProvider serviceProvider, IEnumerable<IMiCakeHandler> handlers)
        {
            CheckValue.NotNull(serviceProvider, nameof(serviceProvider));

            var result = new List<MiCakeHandlerDescriptor>();

            if (handlers.Count() == 0)
                return result;

            foreach (var handler in handlers)
            {
                if (handler is IMiCakeHandlerFactory handlerFactory)
                {
                    var handlerInstance = handlerFactory.CreateInstance(serviceProvider);

                    if (handlerInstance != null)
                        result.Add(new MiCakeHandlerDescriptor(handlerInstance, true));
                }
                else
                {
                    result.Add(new MiCakeHandlerDescriptor(handler));
                }
            }

            return result;
        }
    }
}
