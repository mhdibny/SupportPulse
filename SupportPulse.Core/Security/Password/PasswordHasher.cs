#region Usings

using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;

#endregion

namespace SupportPulse.Core.Security.Password
{
    /// <summary>
    /// Configuration options for the <see cref="PasswordHasher"/>.
    /// </summary>
    public class PasswordHasherOptions
    {
        /// <summary>
        /// If <c>true</c>, Argon2id is used; otherwise PBKDF2‑SHA512.
        /// </summary>
        public bool UseArgon2 { get; set; } = true;

        /// <summary>
        /// A secret pepper appended to the password before hashing.
        /// </summary>
        public string Pepper { get; set; } = "";

        // Argon2 settings
        public int Argon2MemoryKb { get; set; } = 64 * 1024;
        public int Argon2Iterations { get; set; } = 3;
        public int Argon2DegreeOfParallelism { get; set; } = 4;

        /// <summary>
        /// Size of the randomly generated salt in bytes.
        /// </summary>
        public int SaltSize { get; set; } = 16;

        /// <summary>
        /// Size of the hash output in bytes.
        /// </summary>
        public int HashSize { get; set; } = 32;

        /// <summary>
        /// Number of iterations when PBKDF2 is selected.
        /// </summary>
        public int Pbkdf2Iterations { get; set; } = 150_000;
    }

    /// <summary>
    /// Provides password hashing and verification using Argon2id (default) or PBKDF2‑SHA512.
    /// </summary>
    public class PasswordHasher
    {
        private readonly PasswordHasherOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="PasswordHasher"/> class.
        /// </summary>
        /// <param name="options">The hashing options.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="options"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if the pepper is null or whitespace.</exception>
        public PasswordHasher(PasswordHasherOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            if (string.IsNullOrWhiteSpace(_options.Pepper))
                throw new ArgumentException("Pepper must not be null or empty.", nameof(options));
        }

        /// <summary>
        /// Produces a salted hash string for the given password.
        /// Format: Algorithm$Params$Base64Salt$Base64Hash
        /// </summary>
        /// <param name="password">The plain‑text password.</param>
        /// <returns>The hashed password string.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="password"/> is null or empty.</exception>
        public string HashPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentNullException(nameof(password));

            // Generate a cryptographically secure random salt
            var salt = new byte[_options.SaltSize];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            byte[] passwordBytes = Encoding.UTF8.GetBytes(password + _options.Pepper);
            byte[] hashBytes;

            if (_options.UseArgon2)
            {
                var argon2 = new Argon2id(passwordBytes)
                {
                    Salt = salt,
                    MemorySize = _options.Argon2MemoryKb,
                    Iterations = _options.Argon2Iterations,
                    DegreeOfParallelism = _options.Argon2DegreeOfParallelism
                };
                hashBytes = argon2.GetBytes(_options.HashSize);
            }
            else
            {
                using var pbkdf2 = new Rfc2898DeriveBytes(
                    passwordBytes, salt, _options.Pbkdf2Iterations, HashAlgorithmName.SHA512);
                hashBytes = pbkdf2.GetBytes(_options.HashSize);
            }

            // Build the storage string
            string algorithm = _options.UseArgon2 ? "Argon2id" : "PBKDF2-SHA512";
            string paramString = _options.UseArgon2
                ? $"mem={_options.Argon2MemoryKb};iter={_options.Argon2Iterations};par={_options.Argon2DegreeOfParallelism}"
                : $"iter={_options.Pbkdf2Iterations}";

            return $"{algorithm}${paramString}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hashBytes)}";
        }

        /// <summary>
        /// Verifies a plain‑text password against a previously hashed storage string.
        /// Uses a constant‑time comparison to prevent timing attacks.
        /// </summary>
        /// <param name="password">The plain‑text password to verify.</param>
        /// <param name="storedHash">The stored hash string produced by <see cref="HashPassword"/>.</param>
        /// <returns><c>true</c> if the password matches; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if either argument is null or empty.</exception>
        /// <exception cref="FormatException">Thrown if the stored hash format is invalid.</exception>
        public bool VerifyPassword(string password, string storedHash)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentNullException(nameof(password));
            if (string.IsNullOrWhiteSpace(storedHash))
                throw new ArgumentNullException(nameof(storedHash));

            string[] parts = storedHash.Split('$');
            if (parts.Length < 4)
                throw new FormatException("The stored hash has an invalid format.");

            string saltBase64 = parts[2];
            string hashBase64 = parts[3];

            byte[] salt;
            byte[] expectedHash;

            try
            {
                salt = Convert.FromBase64String(saltBase64);
                expectedHash = Convert.FromBase64String(hashBase64);
            }
            catch (FormatException)
            {
                throw new FormatException("The salt or hash in the stored string is not valid Base64.");
            }

            byte[] passwordBytes = Encoding.UTF8.GetBytes(password + _options.Pepper);
            byte[] computedHash;

            if (_options.UseArgon2)
            {
                var argon2 = new Argon2id(passwordBytes)
                {
                    Salt = salt,
                    MemorySize = _options.Argon2MemoryKb,
                    Iterations = _options.Argon2Iterations,
                    DegreeOfParallelism = _options.Argon2DegreeOfParallelism
                };
                computedHash = argon2.GetBytes(_options.HashSize);
            }
            else
            {
                using var pbkdf2 = new Rfc2898DeriveBytes(
                    passwordBytes, salt, _options.Pbkdf2Iterations, HashAlgorithmName.SHA512);
                computedHash = pbkdf2.GetBytes(_options.HashSize);
            }

            // Constant‑time comparison mitigates timing side‑channel attacks
            return CryptographicOperations.FixedTimeEquals(computedHash, expectedHash);
        }
    }
}