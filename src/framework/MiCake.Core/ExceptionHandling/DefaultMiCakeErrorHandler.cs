using System;

namespace MiCake.Core.ExceptionHandling
{
    public class DefaultMiCakeErrorHandler : IMiCakeErrorHandler
    {
        private Action<MiCakeErrorInfo> _dispacthActions;

        public DefaultMiCakeErrorHandler()
        {
        }

        public virtual IMiCakeErrorHandler ConfigureHandlerService(Action<MiCakeErrorInfo> dispacthErrorAction)
        {
            _dispacthActions += dispacthErrorAction;
            return this;
        }

        public virtual void Handle(MiCakeException micakeException)
        {
            if (micakeException == null) return;

            MiCakeErrorInfo errorInfo = new MiCakeErrorInfo(micakeException.Code, micakeException.Details, micakeException);
            _dispacthActions?.Invoke(errorInfo);
        }

    }
}
