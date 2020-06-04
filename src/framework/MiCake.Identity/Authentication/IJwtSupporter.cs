using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

namespace MiCake.Identity.Authentication
{
    /// <summary>
    /// A supporter for Jwt Token.
    /// You can use it to create token.
    /// </summary>
    public interface IJwtSupporter
    {
        /// <summary>
        /// Create a jwt token with <see cref="MiCakeJwtOptions"/>.
        /// </summary>
        /// <param name="miCakeUser"><see cref="IMiCakeUser{TKey}"/>,If user property has <see cref="JwtClaimAttribute"/>,will inculde it.</param>
        /// <returns>Token string.</returns>
        string CreateToken(IMiCakeUser miCakeUser);

        /// <summary>
        /// Create a jwt token with <see cref="MiCakeJwtOptions"/>.
        /// </summary>
        /// <param name="claimsIdentity">A collection of (key,value) pairs representing System.Security.Claims.Claim for this token.</param>
        /// <returns>Token string.</returns>
        string CreateToken(ClaimsIdentity claimsIdentity);

        /// <summary>
        /// Create a jwt token with <see cref="SecurityTokenDescriptor"/>
        /// </summary>
        /// <param name="securityTokenDescriptor"><see cref="SecurityTokenDescriptor"/></param>
        /// <returns>Token string.</returns>
        string CreateToken(SecurityTokenDescriptor securityTokenDescriptor);
    }
}
