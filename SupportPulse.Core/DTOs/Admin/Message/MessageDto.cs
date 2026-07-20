#region Usings

using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

#endregion

namespace SupportPulse.Core.DTOs.Admin.Message
{
    /// <summary>
    /// DTO for sending a plain‑text message from an admin to a user (via SignalR hub).
    /// </summary>
    public class SendPlainTextMessageToUserDto
    {
        [Display(Name = "شناسه چت")]
        [Required(ErrorMessage = "{0} را وارد کنید.")]
        [Range(1, int.MaxValue, ErrorMessage = "{0} باید بزرگتر از 0 باشد.")]
        public int ChatId { get; set; }

        [Display(Name = "متن پیام")]
        [Required(ErrorMessage = "{0} را وارد کنید.")]
        [MaxLength(800, ErrorMessage = "{0} نمیتواند بیشتر از {1} کاراکتر باشد.")]
        [MinLength(1, ErrorMessage = "{0} نمیتواند کمتر از {1} کاراکتر باشد.")]
        public required string MessageData { get; set; }
    }

    /// <summary>
    /// DTO for sending a message with optional text and/or files from an admin to a user (via API).
    /// </summary>
    public class SendMessageToUserDto
    {
        [Display(Name = "شناسه چت")]
        [Required(ErrorMessage = "{0} را وارد کنید.")]
        [Range(1, int.MaxValue, ErrorMessage = "{0} باید بزرگتر از 0 باشد.")]
        public int ChatId { get; set; }

        [Display(Name = "متن پیام")]
        [MaxLength(800, ErrorMessage = "{0} نمیتواند بیشتر از {1} کاراکتر باشد.")]
        public string? MessageData { get; set; }

        [Display(Name = "فایل")]
        [Required(ErrorMessage = "حداقل یک {0} باید ارسال شود.")]
        [MaxLength(5, ErrorMessage = "حداکثر {1} {0} میتوانید ارسال کنید.")]
        public required List<IFormFile> AttachFiles { get; set; }
    }
}