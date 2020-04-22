namespace MiCake.Core.ExceptionHandling
{
    /// <summary>
    ///  Intercepting errors(<see cref="MiCakeException"/>).
    /// </summary>
    public interface IMiCakeErrorHandler
    {
        /// <summary>
        /// Handle micake exception
        /// </summary>
        /// <param name="micakeException"><see cref="MiCakeException"/></param>
        void Handle(MiCakeException micakeException);
    }
}
