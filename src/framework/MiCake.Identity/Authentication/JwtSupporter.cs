using MiCake.Core.Util;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Security.Claims;

namespace MiCake.Identity.Authentication
{
    public class JwtSupporter : IJwtSupporter
    {
        private readonly MiCakeJwtOptions miCakeJwtOptions;

        public JwtSupporter(IOptions<MiCakeJwtOptions> jwtOptions)
        {
            miCakeJwtOptions = jwtOptions?.Value;
        }

        public string CreateToken(IMiCakeUser miCakeUser)
        {
            IDictionary<string, object> claimCollection = null;

            var userProperties = miCakeUser.GetType().GetProperties();
            foreach (var userProperty in userProperties)
            {
                var jwtProperty = userProperty.GetCustomAttribute<JwtClaimAttribute>();
                if (jwtProperty != null)
                {
                    claimCollection = claimCollection ?? new Dictionary<string, object>();
                    claimCollection.Add(jwtProperty.ClaimName == null ? userProperty.Name.ToLower() : jwtProperty.ClaimName.ToLower(), userProperty.GetValue(miCakeUser).ToString());
                }
            }

            var expireTime = DateTime.Now.AddMinutes(miCakeJwtOptions.ExpirationMinutes);
            var signingCredentials = new SigningCredentials(new SymmetricSecurityKey(miCakeJwtOptions.SecurityKey), miCakeJwtOptions.Algorithm);

            return CreateTokenCore(miCakeJwtOptions.Issuer,
                                   miCakeJwtOptions.Audience,
                                   null,
                                   null,
                                   expireTime,
                                   null,
                                   signingCredentials,
                                   miCakeJwtOptions.EncryptingCredentials,
                                   claimCollection);
        }

        public string CreateToken(ClaimsIdentity claimsIdentity)
        {
            var expireTime = DateTime.Now.AddMinutes(miCakeJwtOptions.ExpirationMinutes);
            var signingCredentials = new SigningCredentials(new SymmetricSecurityKey(miCakeJwtOptions.SecurityKey), miCakeJwtOptions.Algorithm);

            return CreateTokenCore(miCakeJwtOptions.Issuer,
                                   miCakeJwtOptions.Audience,
                                   claimsIdentity,
                                   null,
                                   expireTime,
                                   null,
                                   signingCredentials,
                                   miCakeJwtOptions.EncryptingCredentials,
                                   null);
        }

        public string CreateToken(SecurityTokenDescriptor tokenDescriptor)
        {
            CheckValue.NotNull(tokenDescriptor, nameof(tokenDescriptor));

            return CreateTokenCore(
                        tokenDescriptor.Issuer,
                        tokenDescriptor.Audience,
                        tokenDescriptor.Subject,
                        tokenDescriptor.NotBefore,
                        tokenDescriptor.Expires,
                        tokenDescriptor.IssuedAt,
                        tokenDescriptor.SigningCredentials,
                        tokenDescriptor.EncryptingCredentials,
                        tokenDescriptor.Claims);
        }

        protected string CreateTokenCore(string issuer,
                                            string audience,
                                            ClaimsIdentity subject,
                                            DateTime? notBefore,
                                            DateTime? expires,
                                            DateTime? issuedAt,
                                            SigningCredentials signingCredentials,
                                            EncryptingCredentials encryptingCredentials,
                                            IDictionary<string, object> claimCollection)
        {
            var jwtHandler = new JwtSecurityTokenHandler();
            var token = jwtHandler.CreateJwtSecurityToken(issuer, audience, subject, notBefore, expires, issuedAt, signingCredentials, encryptingCredentials, claimCollection);

            return jwtHandler.WriteToken(token);
        }
    }
}
