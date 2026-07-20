#region Usings

using System.ComponentModel.DataAnnotations;
using SupportPulse.Core.DTOs.Admin.User;

#endregion

namespace SupportPulse.Core.DTOs.Admin.Ban
{
    #region History DTOs

    /// <summary>
    /// Represents a single entry in a user's ban history.
    /// </summary>
    public class UserBanHistoryDto
    {
        public int Id { get; set; }

        /// <summary>ID of the admin who performed the action; null if performed by the system.</summary>
        public int? BannedByAdminId { get; set; }

        public string BannedByAdminUserName { get; set; } = null!;

        /// <summary>The action type (Ban, UnBan, or Change).</summary>
        public string Action { get; set; } = null!;

        public string? Reason { get; set; }
        public string ActionDate { get; set; } = null!;

        /// <summary>Expiry date of the ban (null for permanent bans or non‑ban actions).</summary>
        public string? BanExpiryDate { get; set; }
    }

    /// <summary>
    /// Contains a user's ban history together with basic user information.
    /// </summary>
    public class UserBanHistoryListDto
    {
        public List<UserBanHistoryDto> BanHistories { get; set; }
        public UserInformationForBanDto UserInformation { get; set; }
    }

    #endregion

    #region Request DTOs

    /// <summary>
    /// DTO for applying a new ban to a user.
    /// </summary>
    public class BanUserDto
    {
        [Display(Name = "شناسه کاربر")]
        [Required(ErrorMessage = "{0} را وارد کنید.")]
        [Range(1, int.MaxValue, ErrorMessage = "{0} باید بزرگتر از 0 باشد.")]
        public int UserId { get; set; }

        /// <summary>Optional expiry date in Shamsi format; null for a permanent ban.</summary>
        public string? BanExpiryDate { get; set; }

        [Display(Name = "دلیل مسدود سازی")]
        [MaxLength(300, ErrorMessage = "{0} باید بزرگتر از 0 باشد.")]
        public string? Reason { get; set; }
    }

    /// <summary>
    /// DTO for lifting an existing ban from a user.
    /// </summary>
    public class UnBanUserDto
    {
        [Display(Name = "شناسه کاربر")]
        [Required(ErrorMessage = "{0} را وارد کنید.")]
        [Range(1, int.MaxValue, ErrorMessage = "{0} باید بزرگتر از 0 باشد.")]
        public int UserId { get; set; }

        [Display(Name = "دلیل غیرفعال سازی مسدودی")]
        [MaxLength(300, ErrorMessage = "{0} باید بزرگتر از 0 باشد.")]
        public string? Reason { get; set; }
    }

    /// <summary>
    /// DTO for changing the expiry time of an existing ban.
    /// </summary>
    public class ChangeBanExpiryTimeDto
    {
        [Display(Name = "شناسه کاربر")]
        [Required(ErrorMessage = "{0} را وارد کنید.")]
        [Range(1, int.MaxValue, ErrorMessage = "{0} باید بزرگتر از 0 باشد.")]
        public int UserId { get; set; }

        [Display(Name = "دلیل غیرفعال سازی مسدودی")]
        [MaxLength(300, ErrorMessage = "{0} باید بزرگتر از 0 باشد.")]
        public string? Reason { get; set; }

        /// <summary>New expiry date in Shamsi format; null to make it permanent.</summary>
        public string? BanExpiryDate { get; set; }
    }

    #endregion
}