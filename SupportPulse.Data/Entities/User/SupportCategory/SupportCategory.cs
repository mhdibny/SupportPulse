#region Usings

using System.ComponentModel.DataAnnotations;

#endregion

namespace SupportPulse.Data.Entities.User.SupportCategory
{
    /// <summary>
    /// Represents a department or team that provides support to users.
    /// </summary>
    public class SupportCategory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public required string Name { get; set; }

        [Required]
        [MaxLength(300)]
        public required string Details { get; set; }

        /// <summary>
        /// Font‑Awesome icon key used for display purposes.
        /// Defaults to <c>fas fa-tools</c>.
        /// </summary>
        [MaxLength(100)]
        public string? IconKey { get; set; } = "fas fa-tools";

        /// <summary>
        /// Indicates whether this support category is active and can receive chats.
        /// </summary>
        public bool IsActive { get; set; }

        #region Navigation Properties

        /// <summary>
        /// Users assigned to this support category.
        /// </summary>
        public List<UserSupportCategory>? Users { get; set; }

        /// <summary>
        /// Chats belonging to this support category.
        /// </summary>
        public List<Chat.Chat>? Chats { get; set; }

        #endregion
    }
}