using System;

namespace MiCake.Core.Handlers
{
    /// <summary>
    /// A exception context for <see cref="IMiCakeExceptionHandler"/>
    /// </summary>
    public class MiCakeExceptionContext
    {
        /// <summary>
        /// The exception of current step.
        /// </summary>
        public virtual Exception Exception
        {
            get { return _exception; }
            set { _exception = value; }
        }

        private Exception _exception;

        public MiCakeExceptionContext(Exception exception)
        {
            _exception = exception;
        }
    }
}
