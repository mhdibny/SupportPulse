#region Usings

using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

#endregion

namespace SupportPulse.Core.DTOs.Message
{
    /// <summary>
    /// Lightweight DTO representing a newly created message that is broadcast to clients.
    /// </summary>
    public record NewMessageDto(
        int MessageId,
        int SupportCategoryId,
        string SenderUserName,
        string AdminFullName,
        string UniqChatId,
        DateTime SendTime,
        string? MessageData,
        List<UserAttachFileDto>? MessageFiles);

    /// <summary>
    /// Result object containing the created message and its support category ID.
    /// </summary>
    public class SendMessageResultDto
    {
        public NewMessageDto Message { get; set; }
        public int SupportCategoryId { get; set; }
    }

    /// <summary>
    /// Wraps a <see cref="SendMessageResultDto"/> with a list of target user IDs who should receive the message.
    /// </summary>
    public class SendMessageResultWithReceiversDto
    {
        public SendMessageResultDto MessageResult { get; set; } = null!;
        public List<string> ReceiverUserIds { get; set; } = new();
    }

    #region User → Support Message DTOs

    /// <summary>
    /// Request DTO for sending a message with optional text and/or files to support (API endpoint).
    /// </summary>
    public class SendMessageToSupportDto
    {
        [Display(Name = "شناسه چت")]
        [Required(ErrorMessage = "{0} را وارد کنید.")]
        [MaxLength(50, ErrorMessage = "{0} نمیتواند بیشتر از {1} کاراکتر باشد.")]
        [MinLength(30, ErrorMessage = "{0} نمیتواند کمتر از {1} کاراکتر باشد.")]
        public required string ChatUniqId { get; set; }

        [Display(Name = "متن پیام")]
        [MaxLength(800, ErrorMessage = "{0} نمیتواند بیشتر از {1} کاراکتر باشد.")]
        public string? MessageData { get; set; }

        [Display(Name = "فایل")]
        [Required(ErrorMessage = "حداقل یک {0} باید ارسال شود.")]
        [MaxLength(5, ErrorMessage = "حداکثر {1} {0} میتوانید ارسال کنید.")]
        public required List<IFormFile> AttachFiles { get; set; }
    }

    /// <summary>
    /// Request DTO for sending a plain‑text message to support (SignalR hub).
    /// </summary>
    public class SendPlainTextMessageToSupportDto
    {
        [Display(Name = "شناسه چت")]
        [Required(ErrorMessage = "{0} را وارد کنید.")]
        [MaxLength(50, ErrorMessage = "{0} نمیتواند بیشتر از {1} کاراکتر باشد.")]
        [MinLength(30, ErrorMessage = "{0} نمیتواند کمتر از {1} کاراکتر باشد.")]
        public required string ChatUniqId { get; set; }

        [Display(Name = "متن پیام")]
        [Required(ErrorMessage = "{0} را وارد کنید.")]
        [MaxLength(800, ErrorMessage = "{0} نمیتواند بیشتر از {1} کاراکتر باشد.")]
        [MinLength(1, ErrorMessage = "{0} نمیتواند کمتر از {1} کاراکتر باشد.")]
        public required string MessageData { get; set; }
    }

    #endregion

    #region Display DTOs

    /// <summary>
    /// Represents a single message displayed in the chat UI.
    /// </summary>
    public class MessageDto
    {
        public string ChatUniqId { get; set; }
        public string Time { get; set; }
        public bool IsSeen { get; set; }
        public string SenderUserName { get; set; }
        public string? SenderName { get; set; }
        public int MessageTypeId { get; set; }

        [MaxLength(800)]
        public string? Data { get; set; }

        public List<UserAttachFileDto>? AttachFiles { get; set; }
    }

    /// <summary>
    /// Metadata for a file attached to a message (server‑side storage info).
    /// </summary>
    public class AttachFileDto
    {
        public string OriginalName { get; set; }
        public string SavePath { get; set; }
    }

    /// <summary>
    /// Metadata for a file attached to a message (client‑side representation).
    /// </summary>
    public class UserAttachFileDto
    {
        public string OriginalName { get; set; }
        public string DownloadName { get; set; }
    }

    #endregion

    #region Internal / Admin DTOs

    /// <summary>
    /// Internal DTO containing chat context data used by admin message services.
    /// </summary>
    public class AdminMessageChatInfoDto
    {
        public int Id { get; set; }
        public int SupportCategoryId { get; set; }
        public string ChatUniqId { get; set; } = null!;
        public string AdminFullName { get; set; } = null!;
        public int CreatorId { get; set; }
    }

    #endregion
}