
using MiCake.Core.Data;

namespace MiCake.Audit
{
    public class MiCakeAuditOptions : ICanApplyData<MiCakeAuditOptions>
    {
        /// <summary>
        ///  Specified sql is used to generate CreateTime And ModifyTime value.
        /// <para>
        ///  You can get some preset vaules from <see cref="PresetAuditConstants"/>.
        /// </para> 
        /// </summary>
        public string? TimeGenerateSql { get; set; }

        /// <summary>
        /// Open soft deletion and soft deletion audit.
        /// </summary>
        public bool UseSoftDeletion { get; set; } = true;

        /// <summary>
        /// Use to change audit datetime value.
        /// The default value is : DataTime.UtcNow.
        /// </summary>
        public Func<DateTime>? AuditDateTimeProvider { get; set; }

        public void Apply(MiCakeAuditOptions options)
        {
            TimeGenerateSql = options.TimeGenerateSql;
            UseSoftDeletion = options.UseSoftDeletion;
            AuditDateTimeProvider = options.AuditDateTimeProvider;
        }
    }
}
