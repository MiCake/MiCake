namespace MiCake.Core.Handlers
{
    /// <summary>
    ///  Intercepting errors(<see cref="MiCakeException"/>).
    /// </summary>
    public interface IMiCakeExceptionHandler
    {
        /// <summary>
        /// Handle micake exception
        /// </summary>
        /// <param name="micakeException"><see cref="MiCakeException"/></param>
        void Handle(MiCakeException micakeException);
    }
}
