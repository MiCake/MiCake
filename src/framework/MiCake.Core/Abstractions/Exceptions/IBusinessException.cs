namespace MiCake.Core
{
    /// <summary>
    /// Indicates an exception that can be safely shown to end users.
    /// </summary>
    public interface IBusinessException
    {
        /// <summary>
        /// Indicates a code to represent the exception.
        /// </summary>
        string? Code { get; }

        /// <summary>
        /// The error message describing what went wrong.
        /// </summary>
        string? Message { get; }

        /// <summary>
        /// Some details about the error
        /// </summary>
        object? Details { get; }
    }
}
