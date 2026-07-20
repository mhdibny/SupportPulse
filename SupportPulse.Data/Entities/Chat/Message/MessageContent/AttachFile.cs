#region Usings

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#endregion

namespace SupportPulse.Data.Entities.Chat.Message.MessageContent
{
    /// <summary>
    /// Represents a file attachment linked to a message.
    /// </summary>
    public class AttachFile
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Original file name as uploaded by the user.
        /// </summary>
        [MaxLength(200)]
        public required string OriginalFileName { get; set; }

        /// <summary>
        /// Unique path (or name) where the file is stored on disk.
        /// </summary>
        [MaxLength(400)]
        public required string SavedPath { get; set; }

        /// <summary>
        /// ID of the message content this file belongs to.
        /// </summary>
        public int MessageContentId { get; set; }

        #region Navigation Properties

        [ForeignKey(nameof(MessageContentId))]
        public MessageContent? MessageContent { get; set; }

        #endregion
    }
}