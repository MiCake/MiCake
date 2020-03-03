using MiCake.Core.Util.Reflection;
using System;

namespace MiCake.Identity.User
{
    public class CurrentUserOptions
    {
        public Type CurrentUserType { get; set; }

        private Type userKeyType = null;
        public Type UserKeyType
        {
            get
            {
                if (userKeyType != null)
                    return userKeyType;

                userKeyType = TypeHelper.GetGenericInterface(CurrentUserType, typeof(ICurrentUser<>))?
                                        .GetGenericArguments()[0];
                return userKeyType;
            }
        }

        public CurrentUserOptions()
        {
        }
    }
}
