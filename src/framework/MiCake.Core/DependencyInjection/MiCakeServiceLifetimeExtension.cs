using Microsoft.Extensions.DependencyInjection;

namespace MiCake.Core.DependencyInjection
{
    /// <summary>
    /// Extension methods for <see cref="MiCakeServiceLifetime"/>.
    /// </summary>
    public static class MiCakeServiceLifetimeExtension
    {
        /// <summary>
        /// Converts MiCake service lifetime to Microsoft.Extensions.DependencyInjection service lifetime.
        /// </summary>
        /// <param name="miCakeServiceLifetime">The MiCake service lifetime to convert</param>
        /// <returns>The corresponding <see cref="ServiceLifetime"/> value</returns>
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
