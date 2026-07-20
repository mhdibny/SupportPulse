#region Usings

using System.ComponentModel.DataAnnotations;

#endregion

namespace SupportPulse.Core.DTOs.Admin.Role
{
    /// <summary>
    /// Represents a single permission that can be assigned to a role.
    /// </summary>
    public class PermissionDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Category { get; set; } = null!;
    }

    /// <summary>
    /// DTO for creating a new role with an initial set of permissions.
    /// </summary>
    public class AddRoleDto
    {
        [Display(Name = "نام نقش")]
        [Required(ErrorMessage = "{0} را وارد کنید.")]
        [MaxLength(100, ErrorMessage = "{0} نمیتواند بیشتر از {1} کاراکتر باشد.")]
        [MinLength(3, ErrorMessage = "{0} نمیتواند کمتر از {1} کاراکتر باشد.")]
        public required string Name { get; set; }

        [Display(Name = "مجوز های نقش")]
        [Required(ErrorMessage = "{0} را وارد کنید.")]
        [MinLength(1, ErrorMessage = "{0} باید حداقل {1} عضو داشته باشد.")]
        public required List<int> PermissionIdList { get; set; }
    }

    /// <summary>
    /// Full role details including its permissions.
    /// </summary>
    public class RoleDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public List<PermissionDto> Permissions { get; set; } = null!;
    }

    /// <summary>
    /// DTO for editing an existing role's name and permissions.
    /// </summary>
    public class EditRoleDto
    {
        [Display(Name = "شناسه نقش")]
        [Range(1, int.MaxValue, ErrorMessage = "{0} باید بزرگتر از 0 باشد.")]
        public required int Id { get; set; }

        [Display(Name = "نام نقش")]
        [Required(ErrorMessage = "{0} را وارد کنید.")]
        [MaxLength(100, ErrorMessage = "{0} نمیتواند بیشتر از {1} کاراکتر باشد.")]
        [MinLength(3, ErrorMessage = "{0} نمیتواند کمتر از {1} کاراکتر باشد.")]
        public required string Name { get; set; }

        [Display(Name = "مجوز های نقش")]
        public List<int>? PermissionIdList { get; set; } = new List<int>();
    }

    /// <summary>
    /// Flat list item for the admin roles management page.
    /// </summary>
    public class RoleListDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int PermissionsCount { get; set; }
        public int UserHaveThisRoleCount { get; set; }
    }

    /// <summary>
    /// DTO used for the delete‑role confirmation dialog.
    /// </summary>
    public class DeleteRoleDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public List<PermissionDto> Permissions { get; set; } = null!;
        public int UserHaveThisRoleCount { get; set; }
    }

    /// <summary>
    /// Optional search criteria for filtering roles.
    /// </summary>
    public class SearchRoleDto
    {
        public string? RoleName { get; set; }
        public List<int>? PermissionsIdList { get; set; }
    }
}