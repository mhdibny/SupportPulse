#region Usings

using System.ComponentModel.DataAnnotations;

#endregion

namespace SupportPulse.Core.DTOs.Admin.User
{
    /// <summary>
    /// Represents a user row in the admin user list table.
    /// </summary>
    public class UserListDto
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public bool IsAdmin { get; set; }
        public bool IsBanned { get; set; }
        public int RoleCount { get; set; }
    }

    /// <summary>
    /// Pagination parameters for the user list query.
    /// </summary>
    public class UserPageRequestDto
    {
        private int _pageNumber = 1;
        private int _pageSize = 20;

        public int PageNumber
        {
            get => _pageNumber;
            set => _pageNumber = value <= 0 ? 1 : value;
        }

        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value <= 0 || value > 50 ? 20 : value;
        }
    }

    /// <summary>
    /// Optional search criteria for filtering users.
    /// </summary>
    public class UserSearchTermDto
    {
        public string? UserName { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        /// <summary>null = all, true = only banned, false = only active</summary>
        public bool? IsBanned { get; set; }
    }

    /// <summary>
    /// Minimal user information used in the ban slide‑over panel.
    /// </summary>
    public class UserInformationForBanDto
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string FullName { get; set; }
    }

    /// <summary>
    /// Carries user role assignment data between the hub and the service.
    /// </summary>
    public class UserRolesDto
    {
        [Display(Name = "شناسه کاربر")]
        [Required(ErrorMessage = "{0} را وارد کنید.")]
        [Range(1, int.MaxValue, ErrorMessage = "{0} باید بزرگتر از 0 باشد.")]
        public int UserId { get; set; }
        public string UserName { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public List<int> UserRolesIdList { get; set; } = new List<int>();
    }

    /// <summary>
    /// Carries user support‑category assignment data between the hub and the service.
    /// </summary>
    public class UserSupportCategoryDto
    {
        [Display(Name = "شناسه کاربر")]
        [Required(ErrorMessage = "{0} را وارد کنید.")]
        [Range(1, int.MaxValue, ErrorMessage = "{0} باید بزرگتر از 0 باشد.")]
        public int UserId { get; set; }
        public string UserName { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public List<int> SupportCategoryIdList { get; set; } = new List<int>();
    }

    /// <summary>
    /// Lightweight DTO for user information inside support‑category editing forms.
    /// </summary>
    public class UsersSupportCategoriesDto
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string FullName { get; set; }
    }
}