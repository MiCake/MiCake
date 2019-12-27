using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MiCake.Uow
{
    internal interface IUnitOfWorkHook
    {
        event EventHandler<IUnitOfWork> DisposeHandler;

        void OnSaveChanged(Action action);
        void OnRollBacked(Action action);
    }
}
