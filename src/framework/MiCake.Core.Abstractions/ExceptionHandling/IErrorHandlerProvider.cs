using System;

namespace MiCake.Core.ExceptionHandling
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
