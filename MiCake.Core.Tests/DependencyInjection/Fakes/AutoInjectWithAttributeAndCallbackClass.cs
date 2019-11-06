using MiCake.Core.Abstractions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.Core.Tests.DependencyInjection.Fakes
{
    [InjectService(typeof(AutoInjectWithAttributeAndCallbackClass),MiCakeServiceLifeTime.Singleton)]
    public class AutoInjectWithAttributeAndCallbackClass
    {
    }


}
