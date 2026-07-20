#region Usings

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#endregion

namespace SupportPulse.Data.Entities.User.Notification
{
    /// <summary>
    /// Stores a notification delivered to an admin user.
    /// </summary>
    public class AdminNotification
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// The admin user who should receive this notification.
        /// </summary>
        public int AdminUserId { get; set; }

        /// <summary>
        /// Type of the notification, matching the event name (e.g., "UserBanned").
        /// </summary>
        [Required]
        [MaxLength(50)]
        public required string NotificationType { get; set; }

        /// <summary>
        /// Short title of the notification.
        /// </summary>
        [MaxLength(200)]
        public string? Title { get; set; }

        /// <summary>
        /// Detailed message text.
        /// </summary>
        [MaxLength(500)]
        public string? Message { get; set; }

        /// <summary>
        /// Whether the admin has seen this notification.
        /// </summary>
        public bool IsSeen { get; set; }

        /// <summary>
        /// Local timestamp when the notification was created.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        #region Navigation Properties

        [ForeignKey(nameof(AdminUserId))]
        [InverseProperty(nameof(User.AdminNotifications))]
        public User? AdminUser { get; set; }

        #endregion
    }
}