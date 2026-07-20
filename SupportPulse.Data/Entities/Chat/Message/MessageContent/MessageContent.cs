#region Usings

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#endregion

namespace SupportPulse.Data.Entities.Chat.Message.MessageContent
{
    /// <summary>
    /// Contains the actual content of a message (text, files, or both).
    /// </summary>
    public class MessageContent
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// ID of the message type (1 = PlainText, 2 = AttachFile, 3 = PlainTextAndAttachFile).
        /// </summary>
        public int MessageTypeId { get; set; }

        /// <summary>
        /// Text body of the message (if any).
        /// </summary>
        [MaxLength(800)]
        public string? Data { get; set; }

        #region Navigation Properties

        /// <summary>
        /// The message that owns this content.
        /// </summary>
        public Message? Message { get; set; }

        [ForeignKey(nameof(MessageTypeId))]
        public MessageType? MessageType { get; set; }

        /// <summary>
        /// Files attached to this message (if any).
        /// </summary>
        public List<AttachFile>? AttachFiles { get; set; }

        #endregion
    }
}