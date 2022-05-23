using MiCake.Core;

namespace TodoApp.Domain
{
    public class DomainException : PureException
    {
        public DomainException(string message, string? code = null) : base(message)
        {
            Code = code;
        }
    }
}
