#region Usings

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#endregion

namespace SupportPulse.Data.Entities.User.Ban
{
    /// <summary>
    /// Represents a historical entry of a user ban action (ban, unban, or change).
    /// </summary>
    public class UserBanHistory
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// The user this history entry belongs to.
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// The admin who performed the action. Null if the system performed it automatically.
        /// </summary>
        public int? BannedByAdminId { get; set; }

        /// <summary>
        /// Action type: "Ban", "UnBan", or "Change".
        /// </summary>
        [Required]
        [MaxLength(10)]
        public required string Action { get; set; }

        /// <summary>
        /// Optional reason provided for the action.
        /// </summary>
        [MaxLength(300)]
        public string? Reason { get; set; }

        /// <summary>
        /// Date and time when the action took place.
        /// </summary>
        public DateTime ActionDate { get; set; } = DateTime.Now;

        /// <summary>
        /// Expiry time of the ban (null for permanent bans or non‑ban actions).
        /// </summary>
        public DateTime? BanExpiry { get; set; }

        #region Navigation Properties

        /// <summary>
        /// The user who was banned/unbanned.
        /// </summary>
        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        /// <summary>
        /// The admin who issued the action (if any).
        /// </summary>
        [ForeignKey(nameof(BannedByAdminId))]
        public User? Admin { get; set; }

        #endregion
    }
}