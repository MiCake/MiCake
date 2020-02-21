namespace MiCake.Uow
{
    internal interface IChildUnitOfWork : IUnitOfWork
    {
        IUnitOfWork GetParentUnitOfWork();
    }
}
