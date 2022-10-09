namespace MiCake.Uow
{
    internal interface IUnitOfWorkNode
    {
        IUnitOfWorkNode? Parent { get; set; }
    }
}
