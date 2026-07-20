#region Usings

using System.ComponentModel.DataAnnotations;

#endregion

namespace SupportPulse.Data.Entities.User.Role.Permission
{
    /// <summary>
    /// Represents a single permission that can be granted to a role.
    /// </summary>
    public class Permission
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(150)]
        public required string Name { get; set; }

        /// <summary>
        /// Category of the permission (e.g., "General" or "Notification").
        /// </summary>
        [Required]
        [MaxLength(15)]
        public required string Category { get; set; } = "General";

        #region Navigation Properties

        /// <summary>
        /// Roles that have been granted this permission.
        /// </summary>
        public List<RolePermission>? Roles { get; set; }

        #endregion
    }
}