using System;
using System.Security.Cryptography;

namespace SPJ_APP.Service
{
    public static class PasswordHasherService
    {
        private const int SaltSize = 16; // 128 bits
        private const int KeySize = 32;  // 256 bits
        private const int Iterations = 100000;

        /// <summary>
        /// Hashes a plain text password using PBKDF2 with SHA256.
        /// </summary>
        public static string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password)) return string.Empty;

            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
            byte[] key = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                Iterations,
                HashAlgorithmName.SHA256,
                KeySize);

            return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(key)}";
        }

        /// <summary>
        /// Verifies a password against a hash string or legacy plain-text password.
        /// </summary>
        public static bool VerifyPassword(string password, string? storedHashOrPlain)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(storedHashOrPlain))
                return false;

            // Handle legacy plain-text password fallback during transition phase
            if (!storedHashOrPlain.Contains('.'))
            {
                return password == storedHashOrPlain;
            }

            var parts = storedHashOrPlain.Split('.', 3);
            if (parts.Length != 3)
                return false;

            int iterations = int.Parse(parts[0]);
            byte[] salt = Convert.FromBase64String(parts[1]);
            byte[] key = Convert.FromBase64String(parts[2]);

            byte[] keyToCheck = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                iterations,
                HashAlgorithmName.SHA256,
                KeySize);

            return CryptographicOperations.FixedTimeEquals(keyToCheck, key);
        }
    }
}
