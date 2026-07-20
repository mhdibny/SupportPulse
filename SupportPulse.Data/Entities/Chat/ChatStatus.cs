#region Usings

using System.ComponentModel.DataAnnotations;

#endregion

namespace SupportPulse.Data.Entities.Chat
{
    /// <summary>
    /// Lookup table for chat statuses (e.g., "Completed", "Responding").
    /// </summary>
    public class ChatStatus
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public required string Name { get; set; }

        #region Navigation Properties

        /// <summary>
        /// Chats that currently have this status.
        /// </summary>
        public List<Chat>? Chats { get; set; }

        #endregion
    }
}