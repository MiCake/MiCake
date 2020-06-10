using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.Identity.Tests.FakeUser
{
    public class CommonUser : IMiCakeUser<long>
    {
        public long Id { get; set; }

        public string Name { get; set; }

        public CommonUser()
        {

        }
    }
}
