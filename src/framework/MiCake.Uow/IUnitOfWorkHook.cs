using System;

namespace MiCake.Uow
{
    internal interface IUnitOfWorkHook
    {
        event EventHandler<IUnitOfWork> DisposeHandler;

        void OnSaveChanged(Action action);
        void OnRollBacked(Action action);
    }
}
