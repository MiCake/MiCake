using MiCake.Core.Util;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;

namespace MiCake.Core
{
    [Serializable]
    [DebuggerDisplay("Count = {InnerExceptionCount}")]
    public class AggregateMiCakeException : MiCakeException
    {
        private int InnerExceptionCount => InnerExceptions.Count;

        private readonly ReadOnlyCollection<MiCakeException> _exceptions;

        public ReadOnlyCollection<MiCakeException> InnerExceptions => _exceptions;

        public AggregateMiCakeException(string message) : base(message)
        {
            _exceptions = new ReadOnlyCollection<MiCakeException>(Array.Empty<MiCakeException>());
        }

        public AggregateMiCakeException(string message,
            string details,
            string code = null) : base(message, details, code)
        {
            _exceptions = new ReadOnlyCollection<MiCakeException>(Array.Empty<MiCakeException>());
        }

        public AggregateMiCakeException(params MiCakeException[] innerExceptions) : this(innerExceptions, null, null, null)
        {
        }

        public AggregateMiCakeException(IEnumerable<MiCakeException> innerExceptions) : this(new List<MiCakeException>(innerExceptions), null, null, null)
        {
        }

        public AggregateMiCakeException(
            string message,
            string details,
            string code = null,
            params MiCakeException[] innerExceptions) : this(innerExceptions, message, details, code)
        {
        }

        public AggregateMiCakeException(
            IList<MiCakeException> innerExceptions,
            string message,
            string details,
            string code = null) : base(message, innerExceptions != null && innerExceptions.Count > 0 ? innerExceptions[0] : null, details, code)
        {
            CheckValue.NotNull(innerExceptions, nameof(innerExceptions));

            MiCakeException[] exceptionsCopy = new MiCakeException[innerExceptions.Count];

            for (int i = 0; i < exceptionsCopy.Length; i++)
            {
                exceptionsCopy[i] = innerExceptions[i];

                if (exceptionsCopy[i] == null)
                {
                    throw new ArgumentException("arguement:{innerExceptions} has null value.");
                }
            }

            _exceptions = new ReadOnlyCollection<MiCakeException>(exceptionsCopy);
        }

        public override string Message
        {
            get
            {
                if (_exceptions.Count == 0)
                {
                    return base.Message;
                }

                StringBuilder sb = new();
                sb.Append(base.Message);
                sb.Append(' ');
                for (int i = 0; i < _exceptions.Count; i++)
                {
                    sb.Append('(');
                    sb.Append(_exceptions[i].Message);
                    sb.Append(") ");
                }
                return sb.ToString();
            }
        }

        public override string ToString()
        {
            StringBuilder text = new();
            text.Append(base.ToString());

            for (int i = 0; i < _exceptions.Count; i++)
            {
                if (_exceptions[i] == InnerException)
                    continue; // Already logged in base.ToString()

                text.Append(_exceptions[i].ToString());
                text.Append("<--->");
                text.AppendLine();
            }

            return text.ToString();
        }
    }
}
