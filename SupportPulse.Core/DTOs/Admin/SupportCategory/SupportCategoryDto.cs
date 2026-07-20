#region Usings

using System.ComponentModel.DataAnnotations;
using SupportPulse.Core.DTOs.Admin.User;

#endregion

namespace SupportPulse.Core.DTOs.Admin.SupportCategory
{
    /// <summary>
    /// DTO for creating a new support category.
    /// </summary>
    public class AddSupportCategoryDto
    {
        [Required]
        [MaxLength(100)]
        public required string Name { get; set; }

        [Required]
        [MaxLength(300)]
        public required string Details { get; set; }

        [MaxLength(100)]
        public string? IconKey { get; set; }
    }

    /// <summary>
    /// DTO for editing an existing support category.
    /// </summary>
    public class EditSupportCategoryDto
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public required string Name { get; set; }

        [Required]
        [MaxLength(300)]
        public required string Details { get; set; }

        [MaxLength(100)]
        public string? IconKey { get; set; }

        public bool IsActive { get; set; }

        /// <summary>
        /// Users currently assigned to this support category.
        /// </summary>
        public List<UsersSupportCategoriesDto>? Users { get; set; }
    }

    /// <summary>
    /// DTO used when presenting support categories for assignment to a user.
    /// </summary>
    public class SupportCategoryForAssignToUserDto
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Details { get; set; }
        public string? IconKey { get; set; }
        public string? IconClassName { get; set; }
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// Flat list item for the admin support‑category management page.
    /// </summary>
    public class SupportCategoryListDto
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public bool IsActive { get; set; }
        public int UserCount { get; set; }
    }
}