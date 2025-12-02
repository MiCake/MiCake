using System;
using MiCake.Audit.Core;

namespace MiCake.Audit
{
    public class MiCakeAuditOptions
    {
        /// <summary>
        /// Whether to use soft delete function
        /// If is false,Data will not be verified for soft deletion when it is saved.
        /// Default value is false.
        /// </summary>
        public bool UseSoftDeletion { get; set; } = false;

        /// <summary>
        /// Whether to use audit function.
        /// If is false,Data will not be verified for audit(create time,modify time,etc) when it is saved.
        /// Default value is true.
        /// </summary>
        public bool UseAudit { get; set; } = true;

        /// <summary>
        /// The time provider for audit.
        /// Will set the value of <see cref="DefaultTimeAuditProvider.CurrentTimeProvider"/>
        /// </summary>
        public Func<DateTime>? AuditTimeProvider { get; set; }

        public MiCakeAuditOptions()
        {
        }
    }
}
