namespace MiCake.Audit.Core
{
    internal class AuditCoreOptions
    {
        public Func<DateTime>? DateTimeValueProvider { get; set; }

        public bool AssignCreatedTime { get; set; }

        public bool AssignUpdatedTime { get; set; }
    }
}
