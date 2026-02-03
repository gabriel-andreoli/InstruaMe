using InstruaMe.Domain.Contracts.Services;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Security.Cryptography;

namespace InstruaMe.Services
{
    public sealed class PasswordHasherService : IPasswordHasherService
    {
        public (string hash, string salt) Hash(string password)
        {
            var saltBytes = RandomNumberGenerator.GetBytes(16);

            var hashBytes = KeyDerivation.Pbkdf2(
                password,
                saltBytes,
                KeyDerivationPrf.HMACSHA256,
                iterationCount: 100_000,
                numBytesRequested: 32
            );

            return (
                Convert.ToBase64String(hashBytes),
                Convert.ToBase64String(saltBytes)
            );
        }

        public bool Verify(string password, string hash, string salt)
        {
            var saltBytes = Convert.FromBase64String(salt);

            var hashBytes = KeyDerivation.Pbkdf2(
                password,
                saltBytes,
                KeyDerivationPrf.HMACSHA256,
                iterationCount: 100_000,
                numBytesRequested: 32
            );

            return Convert.ToBase64String(hashBytes) == hash;
        }
    }
}
