#region Usings

using System.ComponentModel.DataAnnotations.Schema;

#endregion

namespace SupportPulse.Data.Entities.User.Role
{
    /// <summary>
    /// Join entity representing a many‑to‑many relationship between roles and permissions.
    /// </summary>
    public class RolePermission
    {
        public int RoleId { get; set; }
        public int PermissionId { get; set; }

        #region Navigation Properties

        [ForeignKey(nameof(RoleId))]
        public Role? Role { get; set; }

        [ForeignKey(nameof(PermissionId))]
        public Permission.Permission? Permission { get; set; }

        #endregion
    }
}