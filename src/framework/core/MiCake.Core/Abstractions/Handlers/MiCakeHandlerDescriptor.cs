using MiCake.Core.Util;

namespace MiCake.Core.Handlers
{
    /// <summary>
    ///  Descriptor for an <see cref="IMiCakeHandler"/>.
    /// </summary>
    public class MiCakeHandlerDescriptor
    {
        /// <summary>
        /// The <see cref="IMiCakeHandler"/> instance.
        /// </summary>
        public IMiCakeHandler Handler { get; }

        /// <summary>
        /// Indicate this handler is from <see cref="IMiCakeHandlerFactory"/>.
        /// </summary>
        public bool IsFromFactory { get; }

        public MiCakeHandlerDescriptor(IMiCakeHandler handler, bool isFromFactory = false)
        {
            CheckValue.NotNull(handler, nameof(handler));

            Handler = handler;
            IsFromFactory = IsFromFactory;
        }
    }
}
