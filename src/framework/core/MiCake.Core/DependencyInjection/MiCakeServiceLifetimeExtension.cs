namespace MiCake.Core.DependencyInjection
{
    /// <summary>
    /// Extension for <see cref="MiCakeServiceLifetime"/>
    /// </summary>
    public static class MiCakeServiceLifetimeExtension
    {
        /// <summary>
        /// Convert <see cref="MiCakeServiceLifetime"/> to microsoft di <see cref="ServiceLifetime"/>
        /// </summary>
        /// <param name="miCakeServiceLifetime"></param>
        /// <returns></returns>
        public static ServiceLifetime ConvertToMSLifetime(this MiCakeServiceLifetime miCakeServiceLifetime)
        {
            return miCakeServiceLifetime switch
            {
                MiCakeServiceLifetime.Singleton => ServiceLifetime.Singleton,
                MiCakeServiceLifetime.Transient => ServiceLifetime.Transient,
                MiCakeServiceLifetime.Scoped => ServiceLifetime.Scoped,
                _ => ServiceLifetime.Transient
            };
        }
    }
}
