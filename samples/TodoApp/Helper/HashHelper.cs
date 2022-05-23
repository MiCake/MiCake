using System.Security.Cryptography;
using System.Text;

namespace TodoApp.Helper
{
    public static class HashHelper
    {
        public static string MD5Encrypt(string text)
        {
            using MD5 md5Hash = MD5.Create();
            var result = GetMd5Hash(md5Hash, text);

            return result;
        }

        public static bool MD5Verify(string input, string hash)
        {
            using MD5 md5Hash = MD5.Create();
            var result = VerifyMd5Hash(md5Hash, input, hash);

            return result;
        }

        static string GetMd5Hash(MD5 md5Hash, string input)
        {
            // Convert the input string to a byte array and compute the hash.
            byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

            // Create a new Stringbuilder to collect the bytes
            // and create a string.
            StringBuilder sBuilder = new();

            // Loop through each byte of the hashed data
            // and format each one as a hexadecimal string.
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            // Return the hexadecimal string.
            return sBuilder.ToString();
        }

        // Verify a hash against a string.
        static bool VerifyMd5Hash(MD5 md5Hash, string input, string hash)
        {
            // Hash the input.
            string hashOfInput = GetMd5Hash(md5Hash, input);

            // Create a StringComparer an compare the hashes.
            StringComparer comparer = StringComparer.OrdinalIgnoreCase;

            if (0 == comparer.Compare(hashOfInput, hash))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
