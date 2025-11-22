namespace MiCake.Core
{
    /// <summary>
    /// Indicates a non critical error message
    /// </summary>
    public interface ISlightException
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
