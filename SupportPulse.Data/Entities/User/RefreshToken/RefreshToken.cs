#region Usings

using System.ComponentModel.DataAnnotations;

#endregion

namespace SupportPulse.Data.Entities.User.RefreshToken
{
    /// <summary>
    /// Represents a refresh token used for JWT authentication renewal.
    /// </summary>
    public class RefreshToken
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// The actual refresh token value.
        /// </summary>
        public required string Token { get; set; }

        public int UserId { get; set; }

        /// <summary>
        /// Expiration date and time of the token.
        /// </summary>
        public DateTime Expires { get; set; }

        /// <summary>
        /// Whether the token has been revoked.
        /// </summary>
        public bool IsRevoked { get; set; }

        /// <summary>
        /// Timestamp when the token was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        #region Navigation Properties

        public User? User { get; set; }

        #endregion
    }
}