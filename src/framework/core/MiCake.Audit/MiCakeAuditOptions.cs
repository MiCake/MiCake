using MiCake.Core.Data;
using MiCake.Core.Time;

namespace MiCake.Audit
{
    public class MiCakeAuditOptions : ICanApplyData<MiCakeAuditOptions>
    {
        /// <summary>
        /// Indicate use of SQL to automatically generate time, if the value is FALSE, will assign a value by <see cref="IAppClock"/>.
        /// 
        /// <para>
        ///     Defalut value is FALSE. if you wanna open this flag,please assign a value to <see cref="SqlForGenerateTime"/>.
        /// </para>
        /// </summary>
        public bool UseSqlToGenerateTime { get; set; }

        /// <summary>
        ///  Specified sql is used to generate <see cref=" IHasCreatedTime.CreatedTime"/> And <see cref="IHasUpdatedTime.UpdatedTime"/> value.
        /// <para>
        ///  You can get some preset vaules from <see cref="PresetAuditConstants"/>.
        /// </para> 
        /// </summary>
        public string? SqlForGenerateTime { get; set; }

        /// <summary>
        /// Open soft deletion and soft deletion audit.
        /// </summary>
        public bool UseSoftDeletion { get; set; } = true;

        /// <summary>
        /// Use to change audit datetime value.
        /// The default value is : <see cref="IAppClock.Now"/>
        /// </summary>
        public Func<DateTime>? AuditDateTimeProvider { get; set; }

        public void Apply(MiCakeAuditOptions options)
        {
            UseSqlToGenerateTime = options.UseSqlToGenerateTime;
            SqlForGenerateTime = options.SqlForGenerateTime;
            UseSoftDeletion = options.UseSoftDeletion;
            AuditDateTimeProvider = options.AuditDateTimeProvider;
        }
    }
}
