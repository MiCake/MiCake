using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.Identity.Authentication.Jwt
{
    internal class DefaultJwtStoreKeyGenerator : IJwtStoreKeyGenerator
    {
        public Task<string> GenerateKey(JwtAuthContext context, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(GetAccessTokenMD5(context.AccessToken));
        }

        public Task<string> RetrieveKey(JwtAuthContext context, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(GetAccessTokenMD5(context.AccessToken));
        }

        private string GetAccessTokenMD5(string accesstoken)
        {
            MD5CryptoServiceProvider md5Hasher = new MD5CryptoServiceProvider();
            var accessMD5 = md5Hasher.ComputeHash(Encoding.ASCII.GetBytes(accesstoken));
            return Encoding.ASCII.GetString(accessMD5);
        }
    }
}
