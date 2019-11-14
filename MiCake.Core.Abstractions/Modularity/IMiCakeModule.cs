using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.Core.Abstractions.Modularity
{
    public interface IMiCakeModule
    {
        void Initialization(ModuleBearingContext context);

        void Shuntdown(ModuleBearingContext context);
    }
}
