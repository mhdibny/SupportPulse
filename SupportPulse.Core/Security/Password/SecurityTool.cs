#region Usings

using System.Security.Cryptography;

#endregion

namespace SupportPulse.Core.Security.Password
{
    /// <summary>
    /// Provides static helper methods for generating cryptographically secure random values.
    /// </summary>
    public static class SecurityTool
    {
        /// <summary>
        /// Generates a 256‑bit hex string (64 characters) suitable for use as a security stamp.
        /// </summary>
        public static string GenerateSecurityStamp()
        {
            return Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
        }

        /// <summary>
        /// Generates a one‑time nonce as a 128‑bit hex string (32 characters).
        /// </summary>
        public static string GenerateNonce()
        {
            return Guid.NewGuid().ToString("N");
        }

        /// <summary>
        /// Generates a unique chat identifier as a 128‑bit hex string (32 characters).
        /// </summary>
        public static string GenerateChatUniqId()
        {
            return Guid.NewGuid().ToString("N");
        }

        /// <summary>
        /// Generates a 256‑bit URL‑safe refresh token (44 characters, Base64 without padding).
        /// </summary>
        public static string GenerateRefreshToken()
        {
            byte[] bytes = RandomNumberGenerator.GetBytes(32);
            return Convert.ToBase64String(bytes)
                .Replace("+", "")
                .Replace("/", "")
                .Replace("=", "");
        }
    }
}