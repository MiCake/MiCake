using System;

namespace MiCake.Core
{
    public interface IMiCakeApplicationProvider
    {
        void Initialize(IServiceProvider serviceProvider);

        IMiCakeApplication GetApplication();
    }
}
