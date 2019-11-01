using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.Core.Abstractions.Modularity
{
    public abstract class MiCakeModule : IMiCakeModule, IModuleLifeTime
    {

        public virtual void OnShuntdown(ModuleContext context)
        {
        }

        public virtual void OnStart(ModuleContext context)
        {
        }

        public virtual void PreShuntdown(ModuleContext context)
        {
        }

        public virtual void PreStart(ModuleContext context)
        {
        }

        public virtual void Shuntdown(ModuleContext context)
        {
        }

        public virtual void Start(ModuleContext context)
        {
        }
    }
}
