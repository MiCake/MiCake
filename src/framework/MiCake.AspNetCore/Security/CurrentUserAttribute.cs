using MiCake.Identity;
using System;

namespace MiCake.AspNetCore.Security
{
    /// <summary>
    /// The ID used to mark the parameter must be the current <see cref="IMiCakeUser{TKey}"/> Id.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    public class CurrentUserAttribute : Attribute
    {
        public CurrentUserAttribute()
        {
        }
    }
}
