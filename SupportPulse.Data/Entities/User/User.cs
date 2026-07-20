#region Usings

using SupportPulse.Data.Entities.Chat.Message;
using SupportPulse.Data.Entities.User.Ban;
using SupportPulse.Data.Entities.User.Notification;
using SupportPulse.Data.Entities.User.SupportCategory;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#endregion

namespace SupportPulse.Data.Entities.User
{
    /// <summary>
    /// Represents a user (regular user or admin) of the system.
    /// </summary>
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public required string UserName { get; set; }

        [Required]
        [MaxLength(70)]
        public required string FirstName { get; set; }

        [Required]
        [MaxLength(70)]
        public required string LastName { get; set; }

        [Required]
        [MaxLength(200)]
        public required string Password { get; set; }

        [Required]
        [MaxLength(70)]
        public required string SecurityStamp { get; set; }

        public bool IsBan { get; set; }

        public DateTime? BanExpiry { get; set; }

        #region Computed Properties

        [NotMapped]
        public string FullName => $"{FirstName} {LastName}";

        [NotMapped]
        public bool IsCurrentlyBanned => IsBan && (BanExpiry == null || BanExpiry > DateTime.UtcNow);

        #endregion

        #region Navigation Properties

        public List<Chat.Chat>? Chats { get; set; }
        public List<UserSupportCategory>? SupportCategories { get; set; }
        public List<Message>? Messages { get; set; }
        public List<UserRole>? Roles { get; set; }
        public List<RefreshToken.RefreshToken>? RefreshTokens { get; set; }
        public List<UserBanHistory>? BanHistories { get; set; }
        public List<UserBanHistory>? BansIssued { get; set; }
        public List<Chat.Chat>? LockedChats { get; set; }
        public List<AdminNotification>? AdminNotifications { get; set; }
        #endregion
    }
}