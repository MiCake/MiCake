using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MiCake.Uow
{
    internal interface IUnitOfWorkHook
    {
        EventHandler<IUnitOfWork> DisposeHandler { get; set; }

        void OnSaveChanged(Action action);
        void OnRollBacked(Action action);
    }
}
