using System;

namespace MiCake.AspNetCore.Security
{
    /// <summary>
    /// Mark current model property is use id ,this value will be verified in authenticaition system automatic.
    /// When you tag a model with this attribute, use the <see cref="CurrentUserAttribute"/> together in your action argument.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class VerifyUserIdAttribute : Attribute
    {
    }
}
