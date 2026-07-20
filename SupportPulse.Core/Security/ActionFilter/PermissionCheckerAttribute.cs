#region Usings

using Microsoft.AspNetCore.Mvc;
using SupportPulse.Data.Enums.Admin;

#endregion

namespace SupportPulse.Core.Security.ActionFilter
{
    /// <summary>
    /// An attribute that checks whether the current user has a specific admin permission.
    /// When applied to a controller or action, it executes <see cref="PermissionCheckerFilter"/>.
    /// </summary>
    public class PermissionCheckerAttribute : TypeFilterAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PermissionCheckerAttribute"/> class.
        /// </summary>
        /// <param name="permission">The admin permission required to access the decorated resource.</param>
        public PermissionCheckerAttribute(AdminPermission permission)
            : base(typeof(PermissionCheckerFilter))
        {
            Arguments = new object[] { (int)permission };
        }
    }
}