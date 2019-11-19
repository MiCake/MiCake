using MiCake.Core.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.Autofac
{
    public static class MiCakeApplicationAutofacExtension
    {
        public static IMiCakeApplication AddAutofac(this IMiCakeApplication miCakeApp)
        {
            return miCakeApp; ;
        }
    }
}
