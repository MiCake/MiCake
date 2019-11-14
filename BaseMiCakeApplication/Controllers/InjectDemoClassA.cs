using MiCake.Core.Abstractions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BaseMiCakeApplication.Controllers
{
    [InjectService(Type = typeof(IClassA))]
    public class InjectDemoClassA : IClassA
    {
        public string StrWrite()
        {
            return "AAAA";
        }
    }
}
