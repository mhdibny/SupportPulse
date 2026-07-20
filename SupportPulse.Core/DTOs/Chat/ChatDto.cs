#region Usings

using System.ComponentModel.DataAnnotations;

#endregion

namespace SupportPulse.Core.DTOs.Chat
{
    #region Request DTOs

    /// <summary>
    /// DTO used by a user to create a new support chat.
    /// </summary>
    public class CreateSupportChatDto
    {
        [Display(Name = "موضوع پشتیبانی")]
        [Required(ErrorMessage = "{0} را وارد کنید.")]
        [MaxLength(100, ErrorMessage = "{0} نمیتواند بیشتر از {1} کاراکتر باشد.")]
        [MinLength(5, ErrorMessage = "{0} نمیتواند کمتر از {1} کاراکتر باشد.")]
        public required string Subject { get; set; }

        [Display(Name = "واحد مربوطه")]
        [Required(ErrorMessage = "{0} را وارد کنید.")]
        public required int SupportUnitId { get; set; }
    }

    /// <summary>
    /// Internal DTO that carries the full chat creation data for the service layer.
    /// </summary>
    public record CreateChatSupportDto(int UserId, string Subject, int SupportCategoryId);

    #endregion

    #region Response DTOs

    /// <summary>
    /// Returned immediately after a successful chat creation, with icon classes already resolved.
    /// </summary>
    public record SuccessCreatedChatDto(
        string UniqChatId,
        string Subject,
        string ChatStatus,
        string SupportCategoryName,
        string? SupportCategoryIconKey,
        string? SupportCategoryClass)
    {
        public string SupportCategoryIconKey { get; set; }
        public string SupportCategoryClass { get; set; }
    }

    /// <summary>
    /// Contains summary information for a single chat in the user's chat list.
    /// </summary>
    public record UserChatsDto(
        string UniqChatId,
        string Subject,
        string ChatStatus,
        string SupportCategoryName,
        DateTime CreatedTime,
        string? SupportCategoryIconKey,
        string? SupportCategoryIconClass,
        string? LatestMessageText,
        DateTime? LatestMessageTime)
    {
        /// <summary>
        /// Computed icon CSS class; set after retrieval.
        /// </summary>
        public string? SupportCategoryIconClass { get; set; }
    }

    /// <summary>
    /// Contains detailed information about a single chat for a specific user.
    /// </summary>
    public record ChatDto(
        string ChatUniqId,
        string Subject,
        string CreatorUserName,
        string ChatStatus,
        string SupportCategoryName,
        string CreatedTime
    )
    {
        public string SupportCategoryIconKey { get; set; }
        public string SupportCategoryClass { get; set; }
    }

    #endregion

    #region Internal / Info DTOs

    /// <summary>
    /// Used internally when sending a user message to gather chat context.
    /// </summary>
    public class UserMessageChatInfoDto
    {
        public int Id { get; set; }
        public bool IsEnded { get; set; }
        public int SupportCategoryId { get; set; }
        public string ChatUniqId { get; set; } = null!;
        public string SenderUserName { get; set; } = null!;
        public int? LockedByAdminId { get; set; }
    }

    /// <summary>
    /// Lightweight DTO for presence tracking of locked chats.
    /// </summary>
    public class ChatPresenceInfoDto
    {
        public int ChatId { get; set; }
        public string ChatUniqId { get; set; } = null!;
        public int TargetUserId { get; set; }
    }

    #endregion
}