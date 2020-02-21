using MiCake.Identity.User;
using System;

namespace MiCake.Audit.Tests.Fakes
{
    public class TestCurrentUser : CurrentUser<Guid>
    {
        public override Guid UserID { get; set; }

        public TestCurrentUser()
        {
            UserID = new Guid("cc1b0a62-c76f-487f-8805-e6657aedbf27");
        }
    }
}
