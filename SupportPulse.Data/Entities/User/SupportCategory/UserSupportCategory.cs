#region Usings

using System.ComponentModel.DataAnnotations.Schema;

#endregion

namespace SupportPulse.Data.Entities.User.SupportCategory
{
    /// <summary>
    /// Join entity representing a many‑to‑many relationship between users and support categories.
    /// </summary>
    public class UserSupportCategory
    {
        public int SupportCategoryId { get; set; }
        public int UserId { get; set; }

        #region Navigation Properties

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        [ForeignKey(nameof(SupportCategoryId))]
        public SupportCategory? SupportCategory { get; set; }

        #endregion
    }
}