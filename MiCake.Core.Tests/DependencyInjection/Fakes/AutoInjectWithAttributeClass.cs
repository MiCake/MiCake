using MiCake.Core.Abstractions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.Core.Tests.DependencyInjection.Fakes
{
    [InjectService()]
    public class AutoInjectWithAttributeClass
    {
        public string BackString()
        {
            return "this is Attribute Auto Inject";
        }
    }
}
