using MiCake.Core.Abstractions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BaseMiCakeApplication.Controllers
{
    [InjectService(Lifetime =MiCakeServiceLifeTime.Singleton)]
    public class InjectDemoClassA
    {
        public string StrWrite()
        {
            return "AAAA";
        }
    }
}
