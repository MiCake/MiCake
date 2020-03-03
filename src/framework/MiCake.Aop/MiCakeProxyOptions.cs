namespace MiCake.Aop
{
    public class MiCakeProxyOptions
    {
        /// <summary>
        /// Need create a new <see cref="IMiCakeProxy"/>?
        /// </summary>
        public bool RequiredNew { get; set; }

        public MiCakeProxyOptions()
        {
        }
    }
}
