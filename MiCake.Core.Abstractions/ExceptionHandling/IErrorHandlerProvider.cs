using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.Core.Abstractions.ExceptionHandling
{
    /// <summary>
    ///Provide error handling interception action
    ///Error handler action will provide to <see cref="IMiCakeErrorHandler"/>
    /// </summary>
    public interface IErrorHandlerProvider
    {
        Action<MiCakeErrorInfo> GetErrorHandler();
    }
}
