#region Usings

using System.ComponentModel.DataAnnotations;
using SupportPulse.Data.Entities.User;

#endregion

namespace SupportPulse.Data.Entities.User.Role
{
    /// <summary>
    /// Represents an admin role with a set of permissions.
    /// </summary>
    public class Role
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public required string Name { get; set; }

        #region Navigation Properties

        /// <summary>
        /// Users that have been assigned this role.
        /// </summary>
        public List<UserRole>? Users { get; set; }

        /// <summary>
        /// Permissions granted to this role.
        /// </summary>
        public List<RolePermission>? Permissions { get; set; }

        #endregion
    }
}