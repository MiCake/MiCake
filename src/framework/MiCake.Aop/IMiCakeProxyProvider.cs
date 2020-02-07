namespace MiCake.Aop
{
    /// <summary>
    /// Provide get a <see cref="IMiCakeProxy"/>
    /// </summary>
    public interface IMiCakeProxyProvider
    {
        IMiCakeProxy GetMiCakeProxy(MiCakeProxyOptions options = default);
    }
}
