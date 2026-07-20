#region Usings

using System.ComponentModel.DataAnnotations;

#endregion

namespace SupportPulse.Data.Entities.Chat.Message.MessageContent
{
    /// <summary>
    /// Lookup table for message content types (e.g., PlainText, AttachFile, etc.).
    /// </summary>
    public class MessageType
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public required string Name { get; set; }

        #region Navigation Properties

        /// <summary>
        /// Message contents that are of this type.
        /// </summary>
        public List<MessageContent>? Messages { get; set; }

        #endregion
    }
}