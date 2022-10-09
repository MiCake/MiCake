namespace MiCake.Identity.Authentication.JwtToken
{
    /// <summary>
    /// The attribute is used to mark a property of <see cref="IMiCakeUser{TKey}"/> will be saved in jwt.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class JwtClaimAttribute : Attribute
    {
        /// <summary>
        /// The name of subject.
        /// 
        /// If this parameter is not specified,use property name directly.
        /// </summary>
        public string? ClaimName { get; set; }

        public JwtClaimAttribute()
        {
        }
    }
}
