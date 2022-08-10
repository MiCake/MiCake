namespace MiCake.Audit
{
    public class PresetAuditConstants
    {
        public const string SqlServer_GetDateFunc = "getdate()";

        public const string PostgreSql_GetDateFunc = "current_timestamp";

        public const string Mysql_GetDateFunc = "now()";
    }
}
