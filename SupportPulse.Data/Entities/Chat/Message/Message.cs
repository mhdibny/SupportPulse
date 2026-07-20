#region Usings

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#endregion

namespace SupportPulse.Data.Entities.Chat.Message
{
    /// <summary>
    /// Represents a single message in a chat conversation.
    /// </summary>
    public class Message
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// ID of the chat this message belongs to.
        /// </summary>
        public int ChatId { get; set; }

        /// <summary>
        /// ID of the content (text, files, etc.) of this message.
        /// </summary>
        public int MessageContentId { get; set; }

        /// <summary>
        /// Date and time when the message was sent.
        /// </summary>
        public DateTime Time { get; set; }

        /// <summary>
        /// Indicates whether the message has been seen by the recipient.
        /// </summary>
        public bool IsSeen { get; set; }

        /// <summary>
        /// Timestamp when the message was first seen (null if not yet seen).
        /// </summary>
        public DateTime? SeenAt { get; set; }

        /// <summary>
        /// ID of the user or admin who sent the message.
        /// </summary>
        public int SenderId { get; set; }

        #region Navigation Properties

        [ForeignKey(nameof(ChatId))]
        public Chat? Chat { get; set; }

        [ForeignKey(nameof(SenderId))]
        public User.User? Sender { get; set; }

        [ForeignKey(nameof(MessageContentId))]
        public MessageContent.MessageContent? MessageContent { get; set; }

        #endregion
    }
}