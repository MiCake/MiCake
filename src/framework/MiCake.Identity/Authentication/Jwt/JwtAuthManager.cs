using MiCake.Core.Util;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.Identity.Authentication.Jwt
{
    internal class JwtAuthManager : IJwtAuthManager
    {
        private readonly IJwtTokenStore _tokenStore;
        private readonly MiCakeJwtOptions _jwtOptions;
        private readonly IJwtStoreKeyGenerator _jwtStoreKeyGenerator;
        private readonly IRefreshTokenGenerator _refreshTokenGenerator;

        public JwtAuthManager(
            IJwtTokenStore tokenStore,
            IJwtStoreKeyGenerator jwtStoreKeyGenerator,
            IRefreshTokenGenerator refreshTokenGenerator,
            IOptions<MiCakeJwtOptions> jwtOptions)
        {
            CheckValue.NotNull(tokenStore, nameof(tokenStore));

            _tokenStore = tokenStore;
            _jwtStoreKeyGenerator = jwtStoreKeyGenerator;
            _refreshTokenGenerator = refreshTokenGenerator;
            _jwtOptions = jwtOptions.Value;
        }

        public Task<JwtAuthResult> CreateToken(IMiCakeUser miCakeUser, CancellationToken cancellationToken = default)
        {
            IDictionary<string, object> claimCollection = null;

            var userProperties = miCakeUser.GetType().GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            foreach (var userProperty in userProperties)
            {
                var jwtProperty = userProperty.GetCustomAttribute<JwtClaimAttribute>();
                if (jwtProperty != null)
                {
                    claimCollection ??= new Dictionary<string, object>();
                    claimCollection.Add(jwtProperty.ClaimName == null ? userProperty.Name.ToLower() : jwtProperty.ClaimName.ToLower(), userProperty.GetValue(miCakeUser).ToString());
                }
            }

            var expireTime = DateTime.Now.AddMinutes(_jwtOptions.AccessTokenExpiration);
            var signingCredentials = new SigningCredentials(new SymmetricSecurityKey(_jwtOptions.SecurityKey), _jwtOptions.Algorithm);

            return GenerateTokens(_jwtOptions.Issuer, _jwtOptions.Audience, null, null, expireTime, null, signingCredentials, _jwtOptions.EncryptingCredentials, claimCollection, cancellationToken);
        }

        public Task<JwtAuthResult> CreateToken(Claim[] claims, CancellationToken cancellationToken = default)
        {
            var expireTime = DateTime.Now.AddMinutes(_jwtOptions.AccessTokenExpiration);
            var signingCredentials = new SigningCredentials(new SymmetricSecurityKey(_jwtOptions.SecurityKey), _jwtOptions.Algorithm);

            return GenerateTokens(_jwtOptions.Issuer, _jwtOptions.Audience, new ClaimsIdentity(claims), null, expireTime, null, signingCredentials, _jwtOptions.EncryptingCredentials, null, cancellationToken);
        }

        public Task<(ClaimsPrincipal, JwtSecurityToken)> DecodeJwtToken(string token, CancellationToken cancellationToken = default)
        {
            CheckValue.NotNullOrWhiteSpace(token, "Invalid token");

            var principal = new JwtSecurityTokenHandler()
                .ValidateToken(token,
                    new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = _jwtOptions.Issuer,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(_jwtOptions.SecurityKey),
                        ValidAudience = _jwtOptions.Audience,
                        ValidAlgorithms = new List<string>() { _jwtOptions.Algorithm },
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromMinutes(1)
                    },
                    out var validatedToken);

            return Task.FromResult((principal, validatedToken as JwtSecurityToken));
        }

        public async Task<JwtAuthResult> Refresh(string refreshToken, string accessToken, CancellationToken cancellationToken = default)
        {
            var (principal, jwtToken) = await DecodeJwtToken(accessToken, cancellationToken);
            if (jwtToken == null || !jwtToken.Header.Alg.Equals(_jwtOptions.Algorithm))
            {
                throw new SecurityTokenException("Invalid token");
            }

            var storeRefreshToken = await _tokenStore.GetRefreshToken(refreshToken, cancellationToken);

            if (storeRefreshToken == null || storeRefreshToken.Value.ExpiresTime < DateTime.Now)
            {
                throw new SecurityTokenException("Invalid token");
            }

            if (_jwtOptions.AutoRemoveRefreshTokenHistory)
            {
                var currentContext = new JwtAuthContext()
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    State = JwtAuthContextState.Refresh,
                    Claims = new ClaimsIdentity(principal.Claims),
                    JwtOptions = _jwtOptions
                };
                var retrieveKey = await _jwtStoreKeyGenerator.RetrieveKey(currentContext, cancellationToken);
                await _tokenStore.RemoveRefreshToken(retrieveKey, cancellationToken);
            }

            return await CreateToken(principal.Claims.ToArray(), cancellationToken);
        }

        public async Task RemoveRefreshToken(string refreshToken, CancellationToken cancellationToken = default)
        {
            await _tokenStore.RemoveRefreshToken(refreshToken, cancellationToken);
        }

        private async Task<JwtAuthResult> GenerateTokens(string issuer,
                                                         string audience,
                                                         ClaimsIdentity subject,
                                                         DateTime? notBefore,
                                                         DateTime? expires,
                                                         DateTime? issuedAt,
                                                         SigningCredentials signingCredentials,
                                                         EncryptingCredentials encryptingCredentials,
                                                         IDictionary<string, object> claimCollection,
                                                         CancellationToken cancellationToken)
        {
            var jwtHandler = new JwtSecurityTokenHandler();
            var token = jwtHandler.CreateJwtSecurityToken(issuer, audience, subject, notBefore, expires, issuedAt, signingCredentials, encryptingCredentials, claimCollection);
            var accessToken = jwtHandler.WriteToken(token);

            var allClaim = subject?.Clone() ?? new ClaimsIdentity();
            if (claimCollection != null)
                allClaim.AddClaims(claimCollection.Select(s => new Claim(s.Key, (string)s.Value)));

            var currentContext = new JwtAuthContext()
            {
                JwtOptions = _jwtOptions,
                AccessToken = accessToken,
                Claims = allClaim,
                State = JwtAuthContextState.CreateNew
            };

            var refreshToken = await _refreshTokenGenerator.Generate(currentContext, cancellationToken);
            var result = new JwtAuthResult()
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
            };

            // give refresh-token to context.
            currentContext.RefreshToken = refreshToken;

            if (_jwtOptions.AutoRemoveRefreshTokenHistory)
            {
                var retrieveKey = await _jwtStoreKeyGenerator.RetrieveKey(currentContext, cancellationToken);
                await _tokenStore.RemoveRefreshToken(retrieveKey, cancellationToken);
            }

            var storeKey = await _jwtStoreKeyGenerator.GenerateKey(currentContext, cancellationToken);
            // add refresh to store.
            await _tokenStore.AddRefreshToken(storeKey, refreshToken, DateTime.Now.AddMinutes(_jwtOptions.RefreshTokenExpiration), cancellationToken);

            return result;
        }
    }
}
