using MiCake.Identity.Authentication;
using MiCake.Identity.Tests.FakeUser;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using Xunit;

namespace MiCake.Identity.Tests.Authentication
{
    public class JwtSupporter_Tests
    {
        public JwtSecurityTokenHandler JwtHander { get; set; } = new JwtSecurityTokenHandler();

        public JwtSupporter_Tests()
        {
        }

        [Fact]
        public void CreateToken_useMiCakeUser()
        {
            var options = new MiCakeJwtOptions(); //default value.
            var supporter = CreateJwtSupporter(options);

            var micakeUser = new CommonUser()
            {
                Id = 1,
                Name = "bob"
            };
            var token = supporter.CreateToken(micakeUser);

            Assert.False(string.IsNullOrWhiteSpace(token));
        }

        [Fact]
        public void CreateToken_useMiCakeUser_privateProperty()
        {
            var options = new MiCakeJwtOptions(); //default value.
            var supporter = CreateJwtSupporter(options);

            var micakeUser = new UserWithPrivateProperty() { Id = 10086 };
            var token = supporter.CreateToken(micakeUser);
            var tokenModel = JwtHander.ReadJwtToken(token);

            var userIdClaim = tokenModel.Claims.FirstOrDefault(s => s.Type.Equals("userid"));
            var ageClaim = tokenModel.Claims.FirstOrDefault(s => s.Type.Equals("age"));

            Assert.NotNull(userIdClaim);
            Assert.NotNull(ageClaim);
            Assert.Equal("10086", userIdClaim.Value);
        }

        [Fact]
        public void CreateToken_useMiCakeUser_userHasClaimeAttribute()
        {
            var options = new MiCakeJwtOptions(); //default value.
            var supporter = CreateJwtSupporter(options);

            var micakeUser = new UserWithJwtClaim()
            {
                Id = Guid.NewGuid(),
                Name = "bob"
            };
            var token = supporter.CreateToken(micakeUser);
            Assert.False(string.IsNullOrWhiteSpace(token));

            var tokenModel = JwtHander.ReadJwtToken(token);
            var nameClaim = tokenModel.Claims.FirstOrDefault(s => s.Type.Equals("name"));
            Assert.NotNull(nameClaim);
            Assert.Equal("bob", nameClaim.Value);

            var useridClaim = tokenModel.Claims.FirstOrDefault(s => s.Type.Equals("userid"));
            Assert.NotNull(useridClaim);
            Assert.Equal(micakeUser.Id.ToString(), useridClaim.Value);
        }

        [Fact]
        public void CreateToken_useClaimIdentity()
        {
            var options = new MiCakeJwtOptions(); //default value.
            var supporter = CreateJwtSupporter(options);

            List<Claim> claims = new List<Claim>()
            {
                new Claim("id","123"),
                new Claim("name","bob")
            };
            var identity = new ClaimsIdentity(claims);
            var token = supporter.CreateToken(identity);

            Assert.False(string.IsNullOrWhiteSpace(token));
        }

        [Fact]
        public void CreateToken_useClaimsIdentity_withOptions()
        {
            var options = new MiCakeJwtOptions(); //default value.
            var supporter = CreateJwtSupporter(options);

            List<Claim> claims = new List<Claim>()
            {
                new Claim("id","123"),
                new Claim("name","bob")
            };
            var identity = new ClaimsIdentity(claims);
            var token = supporter.CreateToken(identity);
            var tokenModel = JwtHander.ReadJwtToken(token);

            Assert.Equal("MiCake Application", tokenModel.Issuer);
            Assert.Equal("MiCake Client", tokenModel.Audiences.First());

            var nbf = tokenModel.Claims.First(s => s.Type.Equals("nbf"));
            var exp = tokenModel.Claims.First(s => s.Type.Equals("exp"));
            var iat = tokenModel.Claims.First(s => s.Type.Equals("iat"));

            Assert.True(nbf.Value == iat.Value);
            var expirationTimestamp = GetTimestamp(new DateTime(1970, 1, 1, 0, 0, 0).AddMinutes(options.ExpirationMinutes));
            var exceptExpirationTime = Convert.ToInt64(nbf.Value) + expirationTimestamp;
            Assert.Equal(exceptExpirationTime.ToString(), exp.Value);

            //customer options 
            var options2 = new MiCakeJwtOptions()
            {
                Audience = "test client",
                Issuer = "test application",
                ExpirationMinutes = 30 * 60

            }; //default value.
            var supporter2 = CreateJwtSupporter(options2);

            List<Claim> claims2 = new List<Claim>();
            var identity2 = new ClaimsIdentity(claims2);
            var token2 = supporter2.CreateToken(identity2);
            var tokenModel2 = JwtHander.ReadJwtToken(token2);

            Assert.Equal("test application", tokenModel2.Issuer);
            Assert.Equal("test client", tokenModel2.Audiences.First());

            var nbf2 = tokenModel.Claims.First(s => s.Type.Equals("nbf"));
            var exp2 = tokenModel.Claims.First(s => s.Type.Equals("exp"));
            var iat2 = tokenModel.Claims.First(s => s.Type.Equals("iat"));

            Assert.True(nbf2.Value == iat2.Value);
            var expirationTimestamp2 = GetTimestamp(new DateTime(1970, 1, 1, 0, 0, 0).AddMinutes(options.ExpirationMinutes));
            var exceptExpirationTime2 = Convert.ToInt64(nbf.Value) + expirationTimestamp2;
            Assert.Equal(exceptExpirationTime2.ToString(), exp2.Value);
        }

        public long GetTimestamp(DateTime dateTime)
        {
            DateTime dt1970 = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return (dateTime.Ticks - dt1970.Ticks) / (10000 * 1000);
        }

        public IJwtSupporter CreateJwtSupporter(MiCakeJwtOptions miCakeJwtOptions)
        {
            return new JwtSupporter(Options.Create(miCakeJwtOptions));
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
