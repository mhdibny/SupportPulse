#region Usings

using SupportPulse.Data.Enums.Admin;

#endregion

namespace SupportPulse.Core.Security.ActionFilter.Hub
{
    /// <summary>
    /// Marks a SignalR hub method with a required admin permission.
    /// Used by <see cref="PermissionCheckerHubFilter"/> to enforce fine‑grained access control.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class HubPermissionChecker : Attribute
    {
        /// <summary>
        /// The identifier of the required permission.
        /// </summary>
        public int PermissionId { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="HubPermissionChecker"/> attribute.
        /// </summary>
        /// <param name="permission">The admin permission required to invoke the decorated method.</param>
        public HubPermissionChecker(AdminPermission permission)
        {
            PermissionId = (int)permission;
        }
    }
}