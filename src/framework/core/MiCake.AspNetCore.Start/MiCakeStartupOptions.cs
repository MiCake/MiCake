using MiCake.AspNetCore;
using MiCake.Audit;
using MiCake.Core;
using MiCake.EntityFrameworkCore;

namespace MiCake
{
    public class MiCakeStartupOptions
    {
        /// <summary>
        /// Some options for configure <see cref="IMiCakeApplication"/>.
        /// </summary>
        public MiCakeApplicationOptions ApplicationOptions { get; set; } = new();

        /// <summary>
        /// Some options for configure MiCake EFCore.
        /// </summary>
        public MiCakeEFCoreOptions EFCoreOptions { get; set; } = new();

        /// <summary>
        /// Some options for configure MiCake Audit.
        /// </summary>
        public MiCakeAuditOptions AuditOptions { get; set; } = new();

        /// <summary>
        /// Some options for configure MiCake AspNet Core web.
        /// </summary>
        public MiCakeAspNetCoreOptions AspNetOptions { get; set; } = new();
    }
}
