using MiCake.Core.Abstractions.ExceptionHandling;
using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.Core.ExceptionHandling
{
    public class DefaultMiCakeErrorHandler : IMiCakeErrorHandler
    {
        private Action<MiCakeErrorInfo> _dispacthActions;

        private readonly MiCakeException _exception;

        public DefaultMiCakeErrorHandler(MiCakeException exception)
        {
            _exception = exception;
        }

        public virtual IMiCakeErrorHandler ConfigureHandlerService(Action<MiCakeErrorInfo> dispacthErrorAction)
        {
            _dispacthActions += dispacthErrorAction;
            return this;
        }

        public virtual void Handle()
        {
            if (_exception == null) return;
            
            MiCakeErrorInfo errorInfo = new MiCakeErrorInfo(_exception.Code, _exception.Details, _exception);
            _dispacthActions?.Invoke(errorInfo);
        }
    }
}
