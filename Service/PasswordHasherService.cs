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
        /// Handles null/empty stored passwords, whitespace trimming, and case-insensitive plain text fallback.
        /// </summary>
        public static bool VerifyPassword(string password, string? storedHashOrPlain)
        {
            if (password == null)
                return false;

            string cleanInput = password.Trim();

            // jika password tersimpan di database masih NULL atau kosong
            if (string.IsNullOrWhiteSpace(storedHashOrPlain))
            {
                // Izinkan login jika input cocok dengan password default atau input juga kosong
                return cleanInput == "" ||
                       string.Equals(cleanInput, "admin123", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(cleanInput, "ganti123", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(cleanInput, "123456", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(cleanInput, "admin", StringComparison.OrdinalIgnoreCase);
            }

            string cleanStored = storedHashOrPlain.Trim();

            // 1. Direct match (persis atau abaikan besar-kecil huruf untuk plaintext)
            if (cleanInput == cleanStored || string.Equals(cleanInput, cleanStored, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // 2. Jika tidak mengandung pemisah titik, ini adalah plain text
            if (!cleanStored.Contains('.'))
            {
                return string.Equals(cleanInput, cleanStored, StringComparison.OrdinalIgnoreCase);
            }

            // 3. Verifikasi PBKDF2 SHA256 Hash
            try
            {
                var parts = cleanStored.Split('.', 3);
                if (parts.Length != 3)
                {
                    return string.Equals(cleanInput, cleanStored, StringComparison.OrdinalIgnoreCase);
                }

                if (!int.TryParse(parts[0], out int iterations))
                {
                    return string.Equals(cleanInput, cleanStored, StringComparison.OrdinalIgnoreCase);
                }

                byte[] salt = Convert.FromBase64String(parts[1]);
                byte[] key = Convert.FromBase64String(parts[2]);

                byte[] keyToCheck = Rfc2898DeriveBytes.Pbkdf2(
                    cleanInput,
                    salt,
                    iterations,
                    HashAlgorithmName.SHA256,
                    KeySize);

                return CryptographicOperations.FixedTimeEquals(keyToCheck, key);
            }
            catch
            {
                // Fallback keselamatan ke plain text jika terjadi kesalahan parsing hash
                return string.Equals(cleanInput, cleanStored, StringComparison.OrdinalIgnoreCase);
            }
        }

    }
}
