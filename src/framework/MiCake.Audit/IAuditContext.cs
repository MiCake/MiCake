namespace MiCake.Audit
{
    public interface IAuditContext
    {
        IAuditObjectSetter ObjectSetter { get; }
    }
}
