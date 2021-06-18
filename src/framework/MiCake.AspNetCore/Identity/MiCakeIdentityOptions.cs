namespace MiCake.AspNetCore.Identity
{
    /// <summary>
    /// The options for config micake identity.
    /// </summary>
    public class MiCakeIdentityOptions
    {
        /// <summary>
        /// Specify the user ID claim name to get value from HttpContext.User.Claims. Default value is : "userid".
        /// </summary>
        public string UserIdClaimName { get; set; } = "userid";
    }
}
