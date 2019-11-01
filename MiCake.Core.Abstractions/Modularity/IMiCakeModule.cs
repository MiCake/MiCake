using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.Core.Abstractions.Modularity
{
    public interface IMiCakeModule
    {
        void Start(ModuleContext context);

        void Shuntdown(ModuleContext context);
    }
}
