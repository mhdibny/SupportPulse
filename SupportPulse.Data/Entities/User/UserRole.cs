#region Usings

using System.ComponentModel.DataAnnotations.Schema;

#endregion

namespace SupportPulse.Data.Entities.User
{
    /// <summary>
    /// Join entity representing a many‑to‑many relationship between users and roles.
    /// </summary>
    public class UserRole
    {
        public int UserId { get; set; }
        public int RoleId { get; set; }

        #region Navigation Properties

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        [ForeignKey(nameof(RoleId))]
        public Role.Role? Role { get; set; }

        #endregion
    }
}