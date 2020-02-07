using MiCake.Identity.User;
using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.Audit
{
    public interface IHasModifier : IHasAuditUser
    {
        long? ModifierID { get; set; }
    }

    public interface IHasModifier<TUserKeyType> : IHasAuditUser
    {
        TUserKeyType ModifierID { get; set; }
    }
}
