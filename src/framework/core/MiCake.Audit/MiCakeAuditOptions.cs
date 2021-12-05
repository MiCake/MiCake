namespace MiCake.Audit
{
    public class MiCakeAuditOptions
    {
        /// <summary>
        /// Whether to use soft delete function
        /// If is false,Data will not be verified for soft deletion when it is saved.
        /// Defalut value is true.
        /// </summary>
        public bool UseSoftDeletion { get; set; } = true;

        /// <summary>
        /// Whether to use audit function.
        /// If is false,Data will not be verified for audit(create time,modify time,etc) when it is saved.
        /// Defalut value is true.
        /// </summary>
        public bool UseAudit { get; set; } = true;

        public MiCakeAuditOptions()
        {
        }
    }
}
