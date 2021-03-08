using MiCake.Core.Util;
using MiCake.Identity.Authentication.JwtToken.Abstractions;
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

namespace MiCake.Identity.Authentication.JwtToken
{
    internal class JwtAuthManager : IJwtAuthManager
    {
        private readonly MiCakeJwtOptions _jwtOptions;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly IRefreshTokenStore _refreshTokenStore;

        private const string defaultSubjectId = "default-subject";

        public JwtAuthManager(
            IRefreshTokenService refreshTokenService,
            IRefreshTokenStore refreshTokenStore,
            IOptions<MiCakeJwtOptions> jwtOptions)
        {
            _refreshTokenService = refreshTokenService;
            _refreshTokenStore = refreshTokenStore;
            _jwtOptions = jwtOptions.Value;
        }

        public Task<JwtTokenAuthResult> CreateToken(IMiCakeUser miCakeUser, CancellationToken cancellationToken = default)
        {
            return CreateToken(miCakeUser, defaultSubjectId, cancellationToken);
        }

        public Task<JwtTokenAuthResult> CreateToken(IMiCakeUser miCakeUser, string subjectId, CancellationToken cancellationToken = default)
        {
            return GenerateAuthToken(subjectId, CreateSecurityTokenDescriptorByMiCakeUser(miCakeUser, cancellationToken), cancellationToken);
        }

        public Task<JwtTokenAuthResult> CreateToken(Claim[] claims, CancellationToken cancellationToken = default)
        {
            return CreateToken(claims, defaultSubjectId, cancellationToken);
        }

        public Task<JwtTokenAuthResult> CreateToken(Claim[] claims, string subjectId, CancellationToken cancellationToken = default)
        {
            return GenerateAuthToken(subjectId, CreateSecurityTokenDescriptorByClaims(claims, cancellationToken), cancellationToken);
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

        public async Task<JwtTokenAuthResult> Refresh(string refreshTokenHandle, string accessToken, CancellationToken cancellationToken = default)
        {
            if (!_jwtOptions.UseRefreshToken)
                throw new InvalidOperationException("Not supported refresh-token.");

            var refreshToken = await ValidateRefreshTokenAsync(refreshTokenHandle, cancellationToken);
            var newHandler = await _refreshTokenService.UpdateRefreshTokenAsync(refreshTokenHandle, refreshToken, cancellationToken);

            var decodeAccessToken = await DecodeJwtToken(accessToken, cancellationToken);
            var newAccessToken = await GenerateAccessToken(CreateSecurityTokenDescriptorByClaims(decodeAccessToken.Item1.Claims.ToArray(), cancellationToken), cancellationToken);

            return new JwtTokenAuthResult { AccessToken = newAccessToken, RefreshToken = newHandler };
        }

        public async virtual Task<bool> RevokeRefreshToken(string refreshTokenHandle, CancellationToken cancellationToken = default)
        {
            bool result = true;
            try
            {
                await _refreshTokenStore.RemoveRefreshTokenAsync(refreshTokenHandle, cancellationToken);
            }
            catch
            {
                result = false;
            }

            return result;
        }

        protected async virtual Task<RefreshToken> ValidateRefreshTokenAsync(string refreshTokenHandle, CancellationToken cancellationToken = default)
        {
            var refreshToken = await _refreshTokenStore.GetRefreshTokenAsync(refreshTokenHandle, cancellationToken);

            if (refreshToken == null)
                throw new SecurityTokenException("Invalid refresh token");

            if (DateTimeOffset.UtcNow.DateTime > refreshToken.CreationTime.AddSeconds(refreshToken.Lifetime))
                throw new SecurityTokenExpiredException("Refresh token has expired.");

            if (refreshToken.ConsumedTime.HasValue)
                throw new SecurityTokenException("Rejecting refresh token because it has been consumed already.");

            return refreshToken;
        }

        protected async virtual Task<JwtTokenAuthResult> GenerateAuthToken(string subjectId, SecurityTokenDescriptor securityTokenDescriptor, CancellationToken cancellationToken)
        {
            var accessToken = await GenerateAccessToken(securityTokenDescriptor, cancellationToken);

            var result = new JwtTokenAuthResult { AccessToken = accessToken };

            if (_jwtOptions.UseRefreshToken)
            {
                var allClaim = securityTokenDescriptor.Subject?.Clone() ?? new ClaimsIdentity();
                if (securityTokenDescriptor.Claims != null)
                    allClaim.AddClaims(securityTokenDescriptor.Claims.Select(s => new Claim(s.Key, (string)s.Value)));

                var refreshTokenHandle = await _refreshTokenService.CreateRefreshTokenAsync(new ClaimsPrincipal(allClaim), subjectId, cancellationToken);

                // give refresh token.
                result.RefreshToken = refreshTokenHandle;
            }

            return result;
        }

        protected virtual Task<string> GenerateAccessToken(SecurityTokenDescriptor securityTokenDescriptor, CancellationToken cancellationToken)
        {
            var jwtHandler = new JwtSecurityTokenHandler();
            var token = jwtHandler.CreateJwtSecurityToken(securityTokenDescriptor);
            return Task.FromResult(jwtHandler.WriteToken(token));
        }

        protected virtual SecurityTokenDescriptor CreateSecurityTokenDescriptorByMiCakeUser(IMiCakeUser miCakeUser, CancellationToken cancellationToken = default)
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

            var expireTime = DateTime.Now.AddSeconds(_jwtOptions.AccessTokenLifetime);
            var signingCredentials = new SigningCredentials(new SymmetricSecurityKey(_jwtOptions.SecurityKey), _jwtOptions.Algorithm);

            return new SecurityTokenDescriptor
            {
                Issuer = _jwtOptions.Issuer,
                Audience = _jwtOptions.Audience,
                Expires = expireTime,
                SigningCredentials = signingCredentials,
                EncryptingCredentials = _jwtOptions.EncryptingCredentials,
                Claims = claimCollection
            };
        }

        protected virtual SecurityTokenDescriptor CreateSecurityTokenDescriptorByClaims(Claim[] claims, CancellationToken cancellationToken = default)
        {
            var expireTime = DateTime.Now.AddSeconds(_jwtOptions.AccessTokenLifetime);
            var signingCredentials = new SigningCredentials(new SymmetricSecurityKey(_jwtOptions.SecurityKey), _jwtOptions.Algorithm);

            return new SecurityTokenDescriptor
            {
                Issuer = _jwtOptions.Issuer,
                Audience = _jwtOptions.Audience,
                Expires = expireTime,
                SigningCredentials = signingCredentials,
                EncryptingCredentials = _jwtOptions.EncryptingCredentials,
                Subject = new ClaimsIdentity(claims)
            };
        }
    }
}
