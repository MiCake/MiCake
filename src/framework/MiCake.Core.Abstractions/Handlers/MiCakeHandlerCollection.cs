﻿using System;
using System.Collections.ObjectModel;

namespace MiCake.Core.Handlers
{
    /// <summary>
    /// A collection for <see cref="IMiCakeHandler"/>
    /// </summary>
    public class MiCakeHandlerCollection : Collection<IMiCakeHandler>
    {
        /// <summary>
        /// Adds a type representing an <see cref="IMiCakeHandler"/>.
        /// Current type depends on <see cref="IServiceProvider"/> for instantiation,so you can use other services that in container.
        /// </summary>
        /// <typeparam name="TMiCakeHandler">Type representing an <see cref="IMiCakeHandler"/>.</typeparam>
        /// <returns>An <see cref="IMiCakeHandler"/> representing the added type.</returns>
        public IMiCakeHandler Add<TMiCakeHandler>()
            => Add(typeof(TMiCakeHandler));

        /// <summary>
        /// Adds a type representing an <see cref="IMiCakeHandler"/>.
        /// Current type depends on <see cref="IServiceProvider"/> for instantiation,so you can use other services that in container.
        /// </summary>
        /// <param name="handler">Type representing an <see cref="IMiCakeHandler"/>.</param>
        /// <returns>An <see cref="IMiCakeHandler"/> representing the added type.</returns>
        public IMiCakeHandler Add(Type handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            if (!typeof(IMiCakeHandler).IsAssignableFrom(handler))
            {
                throw new ArgumentException($"Only can add {nameof(IMiCakeHandler)},but given type is {handler.FullName}");
            }

            var handlerResutl = new ServiceHandler(handler);
            Add(handlerResutl);
            return handlerResutl;
        }
    }
}
