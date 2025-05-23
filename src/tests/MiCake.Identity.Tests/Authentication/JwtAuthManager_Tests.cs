﻿using MiCake.Identity.Authentication.JwtToken;
using MiCake.Identity.Tests.FakeUser;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using Xunit;

namespace MiCake.Identity.Tests.Authentication
{
    public class JwtAuthManager_Tests
    {
        public JwtSecurityTokenHandler JwtHander { get; set; } = new JwtSecurityTokenHandler();
        public IServiceProvider ServiceProvider { get; set; }

        public JwtAuthManager_Tests()
        {
        }

        [Fact]
        public async System.Threading.Tasks.Task CreateToken_UseMiCakeUser()
        {
            var supporter = CreateJwtAuthManager(s => { });

            var micakeUser = new CommonUser()
            {
                Id = 1,
                Name = "bob"
            };
            var token = await supporter.CreateToken(micakeUser);

            Assert.False(string.IsNullOrWhiteSpace(token.AccessToken));
            Assert.False(string.IsNullOrWhiteSpace(token.RefreshToken));
        }

        [Fact]
        public async System.Threading.Tasks.Task CreateToken_NotUseRefreshToken_ShowEmptyRefreshToken()
        {
            var supporter = CreateJwtAuthManager(s => { s.UseRefreshToken = false; });

            var micakeUser = new CommonUser()
            {
                Id = 1,
                Name = "bob"
            };
            var token = await supporter.CreateToken(micakeUser);

            Assert.True(string.IsNullOrWhiteSpace(token.RefreshToken));
        }

        [Fact]
        public async System.Threading.Tasks.Task CreateToken_UseMiCakeUser_PrivateProperty()
        {
            var supporter = CreateJwtAuthManager(s => { });

            var micakeUser = new UserWithPrivateProperty() { Id = 10086 };
            var token = await supporter.CreateToken(micakeUser);
            var tokenModel = JwtHander.ReadJwtToken(token.AccessToken);

            var userIdClaim = tokenModel.Claims.FirstOrDefault(s => s.Type.Equals("userid"));
            var ageClaim = tokenModel.Claims.FirstOrDefault(s => s.Type.Equals("age"));

            Assert.NotNull(userIdClaim);
            Assert.NotNull(ageClaim);
            Assert.Equal("10086", userIdClaim.Value);
        }

        [Fact]
        public async System.Threading.Tasks.Task DecodeJwtToken_UseRightSecurityKey()
        {
            var supporter = CreateJwtAuthManager(s => { });

            var micakeUser = new UserWithJwtClaim()
            {
                Id = Guid.NewGuid(),
                Name = "bob"
            };
            var token = await supporter.CreateToken(micakeUser);
            Assert.False(string.IsNullOrWhiteSpace(token.AccessToken));

            var (claims, jwtToken) = await supporter.DecodeJwtToken(token.AccessToken);
            Assert.NotNull(claims);
            Assert.NotNull(jwtToken);
        }

        [Fact]
        public async System.Threading.Tasks.Task DecodeJwtToken_UseWrongSecurityKey()
        {
            var supporter = CreateJwtAuthManager(s => { });

            var micakeUser = new UserWithJwtClaim()
            {
                Id = Guid.NewGuid(),
                Name = "bob"
            };
            var token = await supporter.CreateToken(micakeUser);
            Assert.False(string.IsNullOrWhiteSpace(token.AccessToken));
            var result = await supporter.DecodeJwtToken(token.AccessToken);
            Assert.NotNull(result.Item1);
            Assert.NotNull(result.Item2);

            await Assert.ThrowsAsync<SecurityTokenSignatureKeyNotFoundException>(async () =>
            {
                var worngKeySupporter = CreateJwtAuthManager(s =>
                {
                    s.SecurityKey = Encoding.Default.GetBytes("wrong-key");
                    s.Audience = "Wrong Audience";
                });
                var (claims, jwtToken) = await worngKeySupporter.DecodeJwtToken(token.AccessToken);
            });
        }

        [Fact]
        public async System.Threading.Tasks.Task RefreshToken_RightRefreshToken()
        {
            var supporter = CreateJwtAuthManager(s => { });
            var micakeUser = new UserWithJwtClaim()
            {
                Id = Guid.NewGuid(),
                Name = "bob"
            };
            var token = await supporter.CreateToken(micakeUser);
            var newToken = await supporter.Refresh(token.RefreshToken, token.AccessToken);

            Assert.False(string.IsNullOrWhiteSpace(newToken.AccessToken));
            Assert.False(string.IsNullOrWhiteSpace(newToken.RefreshToken));
            Assert.NotEqual(token.AccessToken, newToken.AccessToken);
        }

        [Fact]
        public async System.Threading.Tasks.Task RefreshToken_WrongRefreshToken()
        {
            var supporter = CreateJwtAuthManager(s => { });
            var micakeUser = new UserWithJwtClaim()
            {
                Id = Guid.NewGuid(),
                Name = "bob"
            };
            var token = await supporter.CreateToken(micakeUser);
            await Assert.ThrowsAnyAsync<Exception>(async () =>
            {
                var newToken = await supporter.Refresh("Wrong Token", token.AccessToken);
            });
        }

        [Fact]
        public async System.Threading.Tasks.Task RefreshToken_ReUseMode_ShouldSame()
        {
            var supporter = CreateJwtAuthManager(s => { });
            var micakeUser = new UserWithJwtClaim()
            {
                Id = Guid.NewGuid(),
                Name = "bob"
            };
            var token = await supporter.CreateToken(micakeUser);

            var newToken = await supporter.Refresh(token.RefreshToken, token.AccessToken);
            Assert.Same(token.RefreshToken, newToken.RefreshToken);
        }

        [Fact]
        public async System.Threading.Tasks.Task RefreshToken_ReCreateMode_ShouldNotSame()
        {
            var supporter = CreateJwtAuthManager(s =>
            {
                s.RefreshTokenMode = RefreshTokenUsageMode.Recreate;
            });
            var micakeUser = new UserWithJwtClaim()
            {
                Id = Guid.NewGuid(),
                Name = "bob"
            };
            var token = await supporter.CreateToken(micakeUser);

            var newToken = await supporter.Refresh(token.RefreshToken, token.AccessToken);
            Assert.NotSame(token.RefreshToken, newToken.RefreshToken);
        }

        [Fact]
        public async System.Threading.Tasks.Task RefreshToken_RecreateBeforeOverdueMode_ShouldNotSame()
        {
            var supporter = CreateJwtAuthManager(s =>
            {
                s.RefreshTokenMode = RefreshTokenUsageMode.RecreateBeforeOverdue;
            });
            var micakeUser = new UserWithJwtClaim()
            {
                Id = Guid.NewGuid(),
                Name = "bob"
            };
            var token = await supporter.CreateToken(micakeUser);

            var newToken = await supporter.Refresh(token.RefreshToken, token.AccessToken);
            Assert.Same(token.RefreshToken, newToken.RefreshToken);
        }

        [Fact]
        public async System.Threading.Tasks.Task RefreshToken_RevokeRefreshToken_StillUse_WillThrowException()
        {
            var supporter = CreateJwtAuthManager(s =>
            {
                s.RefreshTokenMode = RefreshTokenUsageMode.RecreateBeforeOverdue;
            });
            var micakeUser = new UserWithJwtClaim()
            {
                Id = Guid.NewGuid(),
                Name = "bob"
            };
            var token = await supporter.CreateToken(micakeUser);

            // remove current refresh token.
            await supporter.RevokeRefreshToken(token.RefreshToken);

            await Assert.ThrowsAnyAsync<Exception>(async () =>
            {
                await supporter.Refresh(token.RefreshToken, token.AccessToken);
            });
        }

        public IJwtAuthManager CreateJwtAuthManager(Action<MiCakeJwtOptions> miCakeJwtOptions)
        {
            var services = new ServiceCollection();
            services.AddDistributedMemoryCache();

            services.TryAddSingleton<IJwtAuthManager, JwtAuthManager>();
            services.TryAddSingleton<IRefreshTokenService, DefaultRefreshTokenService>();

            services.TryAddSingleton<IRefreshTokenStore, DefaultRefreshTokenStore>();
            services.TryAddSingleton<IRefreshTokenHandleGenerator, DefaultRefreshTokenHandleGenerator>(); ;
            services.Configure(miCakeJwtOptions);

            ServiceProvider = services.BuildServiceProvider();

            return ServiceProvider.GetRequiredService<IJwtAuthManager>();
        }
    }

    public class UserWithPrivateProperty : IMiCakeUser<long>
    {
        public long Id { get; set; }

        [JwtClaim(ClaimName = "userId")]
        private long UserId => Id;

        [JwtClaim(ClaimName = "age")]
        public long Age { get; set; }
    }
}
