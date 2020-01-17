using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.Aop.Castle
{
    public class CastleMiCakeProxyProvider : IMiCakeProxyProvider
    {
        private IMiCakeProxy cacheMiCakeProxy;

        public CastleMiCakeProxyProvider()
        {
        }

        public IMiCakeProxy GetMiCakeProxy(MiCakeProxyOptions options = default)
        {
            IMiCakeProxy expectMiCakeProxy;
            options = options ?? new MiCakeProxyOptions();

            if (options.RequiredNew || cacheMiCakeProxy is null)
            {
                expectMiCakeProxy = new CastleMiCakeProxy();

                cacheMiCakeProxy = expectMiCakeProxy;
            }
            else
            {
                expectMiCakeProxy = cacheMiCakeProxy;
            }

            return expectMiCakeProxy;
        }
    }
}
