using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.Core.Abstractions
{
    public interface IMiCakeApplicationProvider
    {
        void Initialize(IServiceProvider serviceProvider);

        IMiCakeApplication GetApplication();
    }
}
