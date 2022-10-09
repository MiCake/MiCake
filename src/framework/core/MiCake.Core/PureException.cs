namespace MiCake.Core
{
    /// <summary>
    /// Indicates a non critical error message
    /// </summary>
    [Serializable]
    public class PureException : Exception
    {
        public string? Code { get; set; }

        public PureException() : base()
        {
        }

        public PureException(string? message) : base(message)
        {
        }
    }
}
