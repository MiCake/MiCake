using MiCake.Core.Abstractions.Modularity;
using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.Serilog
{
    public class MiCakeSerilogModule : MiCakeModule
    {
        public MiCakeSerilogModule()
        {
        }

        public override void OnShuntdown(ModuleContext context)
        {
            base.OnShuntdown(context);
        }

        public override void OnStart(ModuleContext context)
        {
            base.OnStart(context);
        }

        public override void PreShuntdown(ModuleContext context)
        {
            base.PreShuntdown(context);
        }

        public override void PreStart(ModuleContext context)
        {
            base.PreStart(context);
        }

        public override void Shuntdown(ModuleContext context)
        {
            base.Shuntdown(context);
        }

        public override void Start(ModuleContext context)
        {
            base.Start(context);
        }
    }
}
