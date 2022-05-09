namespace MiCake.Core
{
    /// <summary>
    /// Indicates a non critical error message
    /// </summary>
    [Serializable]
    public class PureException : Exception, IPureException
    {
        public PureException() : base()
        {
        }
    }
}
