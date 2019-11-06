using MiCake.Core.Abstractions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.Core.Tests.DependencyInjection.Fakes
{
    [InjectService()]
    public class BothAttributeAndInterfaceClass : ITransientService, ISingletonService
    {
        public string BackString() => "this is Both Attribute And Interfac  Class";
    }
}
