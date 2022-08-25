using MiCake.Core.Time;
using MiCake.Core.Util;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Security.Claims;

namespace MiCake.Identity.Authentication.JwtToken
{
    internal class JwtAuthManager : IJwtAuthManager
    {
        private readonly IAppClock _clock;
        private readonly MiCakeJwtOptions _jwtOptions;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly IRefreshTokenStore _refreshTokenStore;

        private const string defaultSubjectId = "default-subject";

        public JwtAuthManager(
            IAppClock appClock,
            IRefreshTokenService refreshTokenService,
            IRefreshTokenStore refreshTokenStore,
            IOptions<MiCakeJwtOptions> jwtOptions)
        {
            _clock = appClock;
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

            var securityToken = (validatedToken as JwtSecurityToken)!;
            return Task.FromResult((principal, securityToken));
        }

        public async Task<JwtTokenAuthResult> Refresh(string refreshTokenHandle, string accessToken, CancellationToken cancellationToken = default)
        {
            if (!_jwtOptions.UseRefreshToken)
                throw new InvalidOperationException("Not supported refresh-token.");

            var refreshToken = await ValidateRefreshTokenAsync(refreshTokenHandle, cancellationToken);
            var newHandler = await _refreshTokenService.UpdateRefreshTokenAsync(refreshTokenHandle, refreshToken, cancellationToken);

            var decodeAccessToken = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
            var newAccessToken = await GenerateAccessToken(CreateSecurityTokenDescriptorByClaims(decodeAccessToken.Claims.ToArray(), cancellationToken), cancellationToken);

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

            if (_clock.Now > refreshToken.GetExpireDateTime())
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
            IDictionary<string, object> claimCollection = new Dictionary<string, object>();

            var userProperties = miCakeUser.GetType().GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            foreach (var userProperty in userProperties)
            {
                var jwtProperty = userProperty.GetCustomAttribute<JwtClaimAttribute>();
                if (jwtProperty != null)
                {
                    claimCollection.Add(jwtProperty.ClaimName == null ? userProperty.Name.ToLower() : jwtProperty.ClaimName.ToLower(), userProperty.GetValue(miCakeUser)?.ToString() ?? "");
                }
            }

            var expireTime = _clock.Now.AddSeconds(_jwtOptions.AccessTokenLifetime);
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
            var expireTime = _clock.Now.AddSeconds(_jwtOptions.AccessTokenLifetime);
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
