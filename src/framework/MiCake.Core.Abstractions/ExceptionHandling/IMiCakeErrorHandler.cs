using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.Core.Abstractions.ExceptionHandling
{
    /// <summary>
    /// Error handler generated in MiCake framework.
    /// Capture error and distribute errors to other recipients.
    /// </summary>
    public interface IMiCakeErrorHandler
    {
        /// <summary>
        /// Add a handler service. It will be called at handle time.
        /// </summary>
        /// <param name="errorInfo"><see cref="MiCakeErrorInfo"/></param>
        IMiCakeErrorHandler ConfigureHandlerService(Action<MiCakeErrorInfo> errorInfo);

        /// <summary>
        /// hand micake exception
        /// </summary>
        /// <param name="micakeException"></param>
        void Handle(MiCakeException micakeException);
    }
}
