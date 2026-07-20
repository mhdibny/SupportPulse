#region Usings

using SupportPulse.Data.Entities.User.SupportCategory;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#endregion

namespace SupportPulse.Data.Entities.Chat
{
    /// <summary>
    /// Represents a support chat conversation between a user and an admin.
    /// </summary>
    public class Chat
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Unique public identifier for the chat (used in URLs and real‑time communication).
        /// </summary>
        [MaxLength(40)]
        public required string ChatUniqId { get; set; }

        /// <summary>
        /// Subject or title of the chat.
        /// </summary>
        [Required]
        [MaxLength(100)]
        public required string Subject { get; set; }

        /// <summary>
        /// ID of the user who created the chat.
        /// </summary>
        public int CreatorId { get; set; }

        /// <summary>
        /// ID of the support category this chat belongs to.
        /// </summary>
        public int SupportCategoryId { get; set; }

        /// <summary>
        /// Indicates whether the chat has been ended by an admin or the user.
        /// </summary>
        public bool IsEnded { get; set; }

        /// <summary>
        /// ID of the current chat status (e.g., Responding or Completed).
        /// </summary>
        public int ChatStatusId { get; set; }

        /// <summary>
        /// Timestamp when the chat was created.
        /// </summary>
        public DateTime CreatedTime { get; set; }

        /// <summary>
        /// ID of the admin who has currently locked this chat. Null if the chat is not locked.
        /// </summary>
        public int? LockedByAdminId { get; set; }

        /// <summary>
        /// Timestamp when an admin locked the chat.
        /// </summary>
        public DateTime? LockedAt { get; set; }

        #region Navigation Properties

        [ForeignKey(nameof(CreatorId))]
        [InverseProperty(nameof(User.User.Chats))]
        public User.User? Creator { get; set; }

        [ForeignKey(nameof(SupportCategoryId))]
        public SupportCategory? SupportCategory { get; set; }

        /// <summary>
        /// Messages exchanged in this chat.
        /// </summary>
        public List<Message.Message>? Messages { get; set; }

        [ForeignKey(nameof(ChatStatusId))]
        public ChatStatus? ChatStatus { get; set; }

        [ForeignKey(nameof(LockedByAdminId))]
        [InverseProperty(nameof(User.User.LockedChats))]
        public User.User? LockedByAdmin { get; set; }

        #endregion
    }
}